using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.CLS;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.CLS;

/// <summary>
/// Gate thanh toan CLS (G02). Xem IClsPaymentGate de biet quy tac.
/// Luu y ve bang: repo dang ton tai song song 2 cap bang chi dinh
/// (diab_his_cli_lab_orders/diab_his_cli_rad_orders tu 0031 va
///  diab_his_lab_orders/diab_his_rad_orders tu 9004). Gate tra cuu round_id
/// o ca hai, luon kem filter tenant_id.
/// </summary>
public class ClsPaymentGateImpl : IClsPaymentGate
{
    private static readonly string[] LabTables = { "diab_his_cli_lab_orders", "diab_his_lab_orders" };
    private static readonly string[] RadTables = { "diab_his_cli_rad_orders", "diab_his_rad_orders" };

    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly ILogger<ClsPaymentGateImpl> _logger;

    public ClsPaymentGateImpl(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user,
        IAuditService audit, ILogger<ClsPaymentGateImpl> logger)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; _logger = logger; }

    public async Task<Result<bool>> EnsureRoundPayableAsync(Guid orderId, string orderKind, CancellationToken ct = default)
    {
        var tid = _tenant.TenantId;
        using var conn = _db.CreateConnection();

        var tables = orderKind == ClsOrderKind.Rad ? RadTables : LabTables;
        string? roundId = null;
        foreach (var table in tables)
        {
            try
            {
                roundId = await conn.ExecuteScalarAsync<string?>(
                    $"SELECT round_id FROM {table} WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
                    new { Id = orderId.ToString(), TId = tid });
            }
            catch (Exception ex)
            {
                // Bang/cot khong ton tai o moi truong cu -> bo qua, coi nhu don legacy
                _logger.LogDebug(ex, "ClsPaymentGate: khong doc duoc round_id tu bang {Table}", table);
                continue;
            }
            if (!string.IsNullOrEmpty(roundId)) break;
        }

        // 1. Don legacy (khong thuoc dot nao) -> bo qua gate
        if (string.IsNullOrEmpty(roundId)) return Result<bool>.Success(true);

        var round = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, payment_status, total_amount FROM diab_his_cls_order_rounds
              WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = roundId, TId = tid });
        if (round is null) return Result<bool>.Success(true); // dot da bi xoa -> khong chan

        var paymentStatus = (string)round.payment_status;

        // 2. PAID / WAIVED -> cho phep
        if (ClsRoundPaymentStatus.AllowsExecution(paymentStatus)) return Result<bool>.Success(true);

        // 3/4. UNPAID -> phu thuoc co tenant cho_phep_no_vien_phi
        var allowDebt = false;
        try
        {
            allowDebt = await conn.ExecuteScalarAsync<bool?>(
                "SELECT cho_phep_no_vien_phi FROM diab_his_sys_tenants WHERE id=@TId",
                new { TId = tid }) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ClsPaymentGate: khong doc duoc co cho_phep_no_vien_phi, mac dinh = 0");
        }

        if (!allowDebt)
            return Result<bool>.Failure("CLS_ORDER_UNPAID", "Đợt chỉ định chưa thanh toán",
                new { roundId, orderId, orderKind, totalAmount = Convert.ToDecimal(round.total_amount) });

        await _audit.LogAsync("CLS_UNPAID_BYPASS", "ClsOrderRound", roundId,
            AuditSeverity.WARN, false, null,
            new
            {
                orderId = orderId.ToString(),
                orderType = orderKind,
                totalAmount = Convert.ToDecimal(round.total_amount),
                userId = _user.UserId?.ToString()
            }, ct);

        _logger.LogWarning("CLS_UNPAID_BYPASS round={RoundId} order={OrderId} kind={Kind} tenant={TenantId}",
            roundId, orderId, orderKind, tid);

        return Result<bool>.Success(true);
    }
}
