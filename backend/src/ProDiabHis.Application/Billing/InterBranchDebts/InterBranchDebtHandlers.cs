using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Billing.InterBranchDebts;

// ---- Commands + Queries ----

public record ListInterBranchDebtsQuery(
    int? DebtorBranchId, int? CreditorBranchId, string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<InterBranchDebtResponse>>>;

public record SettleInterBranchDebtCommand(Guid Id, SettleInterBranchDebtRequest Request)
    : IRequest<Result<InterBranchDebtResponse>>;

public static class InterBranchDebtErrors
{
    public const string NotFound = "INTER_BRANCH_DEBT_NOT_FOUND";
    public const string AlreadySettled = "INTER_BRANCH_DEBT_ALREADY_SETTLED";
    public const string BranchAccessDenied = "BRANCH_ACCESS_DENIED";
}

file static class InterBranchDebtSql
{
    public const string SelectBase = @"
        SELECT d.id, d.tenant_id, d.debtor_branch_id, db.name AS debtor_branch_name,
               d.creditor_branch_id, cb.name AS creditor_branch_name,
               d.amount, d.source_type, d.source_ref_id, d.source_ref_code,
               d.status, d.note, d.settled_at, d.created_at
        FROM diab_his_bil_inter_branch_debts d
        LEFT JOIN diab_his_sys_branches db ON db.id = d.debtor_branch_id
        LEFT JOIN diab_his_sys_branches cb ON cb.id = d.creditor_branch_id";
}

file static class InterBranchDebtMapper
{
    public static InterBranchDebtResponse ToDto(dynamic r) => new(
        (Guid)Guid.Parse((string)r.id.ToString()),
        (int)r.tenant_id,
        (int)r.debtor_branch_id, (string?)r.debtor_branch_name,
        (int)r.creditor_branch_id, (string?)r.creditor_branch_name,
        (decimal)r.amount, (string)r.source_type,
        r.source_ref_id == null ? null : (Guid?)Guid.Parse((string)r.source_ref_id.ToString()),
        (string?)r.source_ref_code,
        (string)r.status, (string?)r.note,
        r.settled_at == null ? null : (DateTime?)r.settled_at,
        (DateTime)r.created_at);
}

/// <summary>
/// Danh sach cong no noi bo. BR-60 (ap dung tuong tu dieu chuyen kho): user xem duoc dong ma
/// debtor_branch_id HOAC creditor_branch_id thuoc scope cua minh - KHONG duoc bo qua filter neu
/// khong co IgnoreBranchFilter (branch.cross_view/group_view).
/// </summary>
public class ListInterBranchDebtsHandler
    : IRequestHandler<ListInterBranchDebtsQuery, Result<PagedResult<InterBranchDebtResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public ListInterBranchDebtsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<PagedResult<InterBranchDebtResponse>>> Handle(ListInterBranchDebtsQuery q, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        var where = new List<string> { "d.tenant_id = @tenantId", "d.deleted_at IS NULL" };
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);

        // BR-60 style: khong bo filter neu khong co IgnoreBranchFilter.
        if (!_branchProvider.IgnoreBranchFilter)
        {
            var allowed = _branchProvider.AllowedBranchIds.Count > 0
                ? _branchProvider.AllowedBranchIds.ToList()
                : new List<int> { _branchProvider.BranchId };
            where.Add("(d.debtor_branch_id IN @allowed OR d.creditor_branch_id IN @allowed)");
            p.Add("allowed", allowed);
        }

        if (q.DebtorBranchId.HasValue) { where.Add("d.debtor_branch_id = @debtorId"); p.Add("debtorId", q.DebtorBranchId.Value); }
        if (q.CreditorBranchId.HasValue) { where.Add("d.creditor_branch_id = @creditorId"); p.Add("creditorId", q.CreditorBranchId.Value); }
        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("d.status = @status"); p.Add("status", q.Status); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_bil_inter_branch_debts d WHERE {wc}", p);

        var offset = (q.Page - 1) * q.PageSize;
        p.Add("limit", q.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $"{InterBranchDebtSql.SelectBase} WHERE {wc} ORDER BY d.created_at DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(InterBranchDebtMapper.ToDto).ToList();
        return Result<PagedResult<InterBranchDebtResponse>>.Success(
            new PagedResult<InterBranchDebtResponse>(items, q.Page, q.PageSize, total));
    }
}

public class SettleInterBranchDebtHandler : IRequestHandler<SettleInterBranchDebtCommand, Result<InterBranchDebtResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IAuditService _audit;

    public SettleInterBranchDebtHandler(IDapperConnectionFactory db, ICurrentUser currentUser,
        IBranchProvider branchProvider, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider; _audit = audit;
    }

    public async Task<Result<InterBranchDebtResponse>> Handle(SettleInterBranchDebtCommand cmd, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        var idStr = cmd.Id.ToString();
        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, debtor_branch_id, creditor_branch_id, status FROM diab_his_bil_inter_branch_debts " +
            "WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = idStr, tenantId });
        if (header == null)
            return Result<InterBranchDebtResponse>.Failure(InterBranchDebtErrors.NotFound, "Không tìm thấy bút toán công nợ nội bộ");

        if (!_branchProvider.IgnoreBranchFilter)
        {
            var allowed = _branchProvider.AllowedBranchIds.Count > 0
                ? _branchProvider.AllowedBranchIds.ToList() : new List<int> { _branchProvider.BranchId };
            int debtorId = (int)header.debtor_branch_id, creditorId = (int)header.creditor_branch_id;
            if (!allowed.Contains(debtorId) && !allowed.Contains(creditorId))
                return Result<InterBranchDebtResponse>.Failure(InterBranchDebtErrors.BranchAccessDenied, "Không có quyền truy cập bút toán công nợ chi nhánh này");
        }

        if ((string)header.status == InterBranchDebtStatus.Settled)
            return Result<InterBranchDebtResponse>.Failure(InterBranchDebtErrors.AlreadySettled, "Bút toán đã được tất toán trước đó");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_bil_inter_branch_debts
            SET status = @status, settled_at = UTC_TIMESTAMP(), settled_by = @userId,
                note = COALESCE(@note, note), updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new
            {
                status = InterBranchDebtStatus.Settled, userId = _currentUser.UserId?.ToString(),
                note = cmd.Request.Note, id = idStr, tenantId
            });

        await _audit.LogAsync(Domain.Entities.AuditAction.Update, "InterBranchDebt", idStr,
            new { action = "settle" }, ct);

        var updated = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $"{InterBranchDebtSql.SelectBase} WHERE d.id = @id AND d.tenant_id = @tenantId",
            new { id = idStr, tenantId });
        return Result<InterBranchDebtResponse>.Success(InterBranchDebtMapper.ToDto(updated!));
    }
}
