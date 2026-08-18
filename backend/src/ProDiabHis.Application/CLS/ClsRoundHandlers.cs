using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.CLS;

/// <summary>
/// Handlers cho dot chi dinh CLS (G01/G02).
/// Bang: dot = diab_his_cls_order_rounds; chi dinh XN/CDHA dung cung bang voi
/// ClsHandlers hien tai (diab_his_cli_lab_orders / diab_his_cli_rad_orders).
/// MOI query/insert deu filter tenant_id.
/// </summary>
internal static class ClsRoundSql
{
    public const string RoundTable = "diab_his_cls_order_rounds";
    public const string LabTable   = "diab_his_cli_lab_orders";
    public const string RadTable   = "diab_his_cli_rad_orders";

    public const string SelectRound = @"
        SELECT id, tenant_id, encounter_id, round_no, status, payment_status, total_amount,
               billing_id, paid_at, paid_by, waived_reason, cancel_reason, note, created_at
        FROM diab_his_cls_order_rounds
        WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL";

    public static async Task<ClsRoundResponse?> LoadRoundAsync(IDbConnection conn, int tenantId, string roundId)
    {
        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(SelectRound, new { Id = roundId, TId = tenantId });
        if (r is null) return null;
        return await MapAsync(conn, tenantId, r);
    }

    public static async Task<ClsRoundResponse> MapAsync(IDbConnection conn, int tenantId, dynamic r)
    {
        var roundId = (string)r.id;

        var labRows = await conn.QueryAsync<dynamic>($@"
            SELECT o.id, o.test_code AS code, o.test_name AS name, o.status,
                   COALESCE(d.default_price, 0) AS unit_price
            FROM {LabTable} o
            LEFT JOIN diab_his_dict_lab_tests d ON d.code = o.test_code
            WHERE o.tenant_id=@TId AND o.round_id=@RId AND o.deleted_at IS NULL
            ORDER BY o.ordered_at", new { TId = tenantId, RId = roundId });

        var labs = labRows.Select(o => new ClsRoundOrderItemResponse(
                Guid.Parse((string)o.id), ClsOrderKind.Lab, (string)o.code, (string)o.name,
                (string)o.status, Convert.ToDecimal(o.unit_price)))
            .ToList();

        var radRows = await conn.QueryAsync<dynamic>($@"
            SELECT o.id, o.procedure_code AS code, o.procedure_name AS name, o.status,
                   COALESCE(d.default_price, 0) AS unit_price
            FROM {RadTable} o
            LEFT JOIN diab_his_dict_rad_procedures d ON d.code = o.procedure_code
            WHERE o.tenant_id=@TId AND o.round_id=@RId AND o.deleted_at IS NULL
            ORDER BY o.ordered_at", new { TId = tenantId, RId = roundId });

        var rads = radRows.Select(o => new ClsRoundOrderItemResponse(
                Guid.Parse((string)o.id), ClsOrderKind.Rad, (string)o.code, (string)o.name,
                (string)o.status, Convert.ToDecimal(o.unit_price)))
            .ToList();

        var all  = labs.Concat(rads).ToList();
        var done = all.Count(x => x.Status == "done" || x.Status == "cancelled");
        var progress = new ClsRoundProgressResponse(all.Count, done, all.Count - done);

        string? billingId = (string?)r.billing_id;

        return new ClsRoundResponse(
            Guid.Parse(roundId),
            Guid.Parse((string)r.encounter_id),
            Convert.ToInt32(r.round_no),
            (string)r.status,
            (string)r.payment_status,
            Convert.ToDecimal(r.total_amount),
            string.IsNullOrEmpty(billingId) ? (Guid?)null : Guid.Parse(billingId),
            (DateTime?)r.paid_at,
            (string?)r.waived_reason,
            (string?)r.note,
            (DateTime)r.created_at,
            labs, rads, progress);
    }

    /// <summary>Tinh lai tong tien cua dot theo bang gia dich vu</summary>
    public static async Task<decimal> RecalcTotalAsync(IDbConnection conn, int tenantId, string roundId)
    {
        var labSum = await conn.ExecuteScalarAsync<decimal?>($@"
            SELECT COALESCE(SUM(COALESCE(d.default_price,0)),0) FROM {LabTable} o
            LEFT JOIN diab_his_dict_lab_tests d ON d.code = o.test_code
            WHERE o.tenant_id=@TId AND o.round_id=@RId AND o.deleted_at IS NULL AND o.status <> 'cancelled'",
            new { TId = tenantId, RId = roundId }) ?? 0m;

        var radSum = await conn.ExecuteScalarAsync<decimal?>($@"
            SELECT COALESCE(SUM(COALESCE(d.default_price,0)),0) FROM {RadTable} o
            LEFT JOIN diab_his_dict_rad_procedures d ON d.code = o.procedure_code
            WHERE o.tenant_id=@TId AND o.round_id=@RId AND o.deleted_at IS NULL AND o.status <> 'cancelled'",
            new { TId = tenantId, RId = roundId }) ?? 0m;

        var total = labSum + radSum;
        await conn.ExecuteAsync(
            $"UPDATE {RoundTable} SET total_amount=@Total, updated_at=@Now WHERE id=@Id AND tenant_id=@TId",
            new { Total = total, Now = DateTime.UtcNow, Id = roundId, TId = tenantId });
        return total;
    }
}

// ────────────────────────────────────────────────
// CREATE round
// ────────────────────────────────────────────────
public class CreateClsRoundCommandHandler : IRequestHandler<CreateClsRoundCommand, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateClsRoundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsRoundResponse>> Handle(CreateClsRoundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var encId = cmd.EncounterId.ToString();

        var enc = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_enc_encounters WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = encId, TId = tid });
        if (enc is null)
            return Result<ClsRoundResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var maxNo = await conn.ExecuteScalarAsync<int?>(
            $"SELECT MAX(round_no) FROM {ClsRoundSql.RoundTable} WHERE tenant_id=@TId AND encounter_id=@EId",
            new { TId = tid, EId = encId }) ?? 0;
        var roundNo = maxNo + 1;

        var roundId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync($@"
            INSERT INTO {ClsRoundSql.RoundTable}
                (id, tenant_id, encounter_id, round_no, status, payment_status, total_amount,
                 note, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @TId, @EId, @No, 'OPEN', 'UNPAID', 0, @Note, @Now, @Uid, @Now, @Uid)",
            new { Id = roundId, TId = tid, EId = encId, No = roundNo, Note = cmd.Request.Note, Now = now, Uid = userId });

        var labTests = cmd.Request.LabTests ?? new List<ClsRoundLabItemRequest>();
        foreach (var t in labTests)
        {
            var catalog = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name, sample_type FROM diab_his_dict_lab_tests WHERE code=@Code", new { Code = t.TestCode });
            await conn.ExecuteAsync($@"
                INSERT INTO {ClsRoundSql.LabTable}
                    (id, tenant_id, encounter_id, round_id, test_code, test_name, sample_type,
                     priority, status, ordered_at, ordered_by, note, created_at, created_by, updated_at)
                VALUES (@Id, @TId, @EId, @RId, @Code, @Name, @Sample,
                     @Priority, 'ordered', @Now, @Uid, @Note, @Now, @Uid, @Now)",
                new
                {
                    Id = Guid.NewGuid().ToString(), TId = tid, EId = encId, RId = roundId,
                    Code = t.TestCode, Name = t.TestName ?? (string?)catalog?.name ?? t.TestCode,
                    Sample = t.SampleType ?? (string?)catalog?.sample_type,
                    Priority = t.Priority ?? ClsPriority.Normal, Now = now, Uid = userId, Note = t.Note
                });
        }

        var radOrders = cmd.Request.RadOrders ?? new List<ClsRoundRadItemRequest>();
        foreach (var o in radOrders)
        {
            var catalog = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name FROM diab_his_dict_rad_procedures WHERE code=@Code", new { Code = o.ProcedureCode });
            await conn.ExecuteAsync($@"
                INSERT INTO {ClsRoundSql.RadTable}
                    (id, tenant_id, encounter_id, round_id, modality, body_part, contrast,
                     procedure_code, procedure_name, priority, status, ordered_at, ordered_by, note,
                     created_at, created_by, updated_at)
                VALUES (@Id, @TId, @EId, @RId, @Mod, @Body, @Contrast,
                     @Code, @Name, @Priority, 'ordered', @Now, @Uid, @Note, @Now, @Uid, @Now)",
                new
                {
                    Id = Guid.NewGuid().ToString(), TId = tid, EId = encId, RId = roundId,
                    Mod = o.Modality, Body = o.BodyPart, Contrast = o.Contrast ? 1 : 0,
                    Code = o.ProcedureCode, Name = o.ProcedureName ?? (string?)catalog?.name ?? o.ProcedureCode,
                    Priority = o.Priority ?? ClsPriority.Normal, Now = now, Uid = userId, Note = o.Note
                });
        }

        await ClsRoundSql.RecalcTotalAsync(conn, tid, roundId);
        await _audit.LogAsync("CREATE", "ClsOrderRound", roundId, new { encounterId = encId, roundNo }, ct);

        var dto = await ClsRoundSql.LoadRoundAsync(conn, tid, roundId);
        return Result<ClsRoundResponse>.Success(dto!);
    }
}

// ────────────────────────────────────────────────
// LIST / GET
// ────────────────────────────────────────────────
public class ListClsRoundsQueryHandler : IRequestHandler<ListClsRoundsQuery, Result<ClsRoundListResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListClsRoundsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<ClsRoundListResponse>> Handle(ListClsRoundsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;

        var sql = $@"
            SELECT id, tenant_id, encounter_id, round_no, status, payment_status, total_amount,
                   billing_id, paid_at, paid_by, waived_reason, cancel_reason, note, created_at
            FROM {ClsRoundSql.RoundTable}
            WHERE tenant_id=@TId AND encounter_id=@EId AND deleted_at IS NULL"
            + (string.IsNullOrWhiteSpace(q.Status) ? "" : " AND status=@Status")
            + " ORDER BY round_no";

        var rows = await conn.QueryAsync<dynamic>(sql,
            new { TId = tid, EId = q.EncounterId.ToString(), Status = q.Status });

        var list = new List<ClsRoundResponse>();
        foreach (var r in rows) list.Add(await ClsRoundSql.MapAsync(conn, tid, r));

        var unpaid = list.Where(x => x.PaymentStatus == ClsRoundPaymentStatus.Unpaid
                                  && x.Status != ClsRoundStatus.Cancelled).ToList();

        return Result<ClsRoundListResponse>.Success(new ClsRoundListResponse(
            list, list.Count, unpaid.Count, unpaid.Sum(x => x.TotalAmount)));
    }
}

public class GetClsRoundQueryHandler : IRequestHandler<GetClsRoundQuery, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetClsRoundQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<ClsRoundResponse>> Handle(GetClsRoundQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var dto = await ClsRoundSql.LoadRoundAsync(conn, _tenant.TenantId, q.RoundId.ToString());
        return dto is null
            ? Result<ClsRoundResponse>.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định")
            : Result<ClsRoundResponse>.Success(dto);
    }
}

// ────────────────────────────────────────────────
// SUBMIT — chot dot
// ────────────────────────────────────────────────
public class SubmitClsRoundCommandHandler : IRequestHandler<SubmitClsRoundCommand, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public SubmitClsRoundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsRoundResponse>> Handle(SubmitClsRoundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var rid = cmd.RoundId.ToString();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(ClsRoundSql.SelectRound, new { Id = rid, TId = tid });
        if (row is null)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định");

        var current = (string)row.status;
        if (!ClsRoundStatus.CanTransition(current, ClsRoundStatus.Submitted))
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái đợt từ {current} sang {ClsRoundStatus.Submitted}");

        var itemCount = await conn.ExecuteScalarAsync<int>($@"
            SELECT (SELECT COUNT(*) FROM {ClsRoundSql.LabTable} WHERE tenant_id=@TId AND round_id=@RId AND deleted_at IS NULL)
                 + (SELECT COUNT(*) FROM {ClsRoundSql.RadTable} WHERE tenant_id=@TId AND round_id=@RId AND deleted_at IS NULL)",
            new { TId = tid, RId = rid });
        if (itemCount == 0)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_EMPTY", "Đợt chỉ định chưa có dịch vụ nào");

        await ClsRoundSql.RecalcTotalAsync(conn, tid, rid);
        await conn.ExecuteAsync(
            $"UPDATE {ClsRoundSql.RoundTable} SET status='SUBMITTED', updated_at=@Now, updated_by=@Uid WHERE id=@Id AND tenant_id=@TId",
            new { Now = DateTime.UtcNow, Uid = _user.UserId?.ToString(), Id = rid, TId = tid });

        await _audit.LogAsync("SUBMIT", "ClsOrderRound", rid, new { itemCount }, ct);

        var dto = await ClsRoundSql.LoadRoundAsync(conn, tid, rid);
        return Result<ClsRoundResponse>.Success(dto!);
    }
}

// ────────────────────────────────────────────────
// PAY — danh dau da thanh toan dot
// ────────────────────────────────────────────────
public class PayClsRoundCommandHandler : IRequestHandler<PayClsRoundCommand, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public PayClsRoundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsRoundResponse>> Handle(PayClsRoundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var rid = cmd.RoundId.ToString();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(ClsRoundSql.SelectRound, new { Id = rid, TId = tid });
        if (row is null)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định");

        var pay = (string)row.payment_status;
        if (pay == ClsRoundPaymentStatus.Paid)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_ALREADY_PAID", "Đợt chỉ định đã thanh toán");
        if (!ClsRoundPaymentStatus.CanTransition(pay, ClsRoundPaymentStatus.Paid))
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái thanh toán từ {pay} sang {ClsRoundPaymentStatus.Paid}");

        var total = Convert.ToDecimal(row.total_amount);
        var requestedAmount = cmd.Request?.Amount;
        if (requestedAmount.HasValue && requestedAmount.Value != total)
            return Result<ClsRoundResponse>.Failure("BILLING_AMOUNT_MISMATCH",
                "Số tiền thanh toán không khớp tổng tiền đợt chỉ định",
                new { expected = total, actual = requestedAmount.Value });

        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();
        await conn.ExecuteAsync($@"
            UPDATE {ClsRoundSql.RoundTable}
               SET payment_status='PAID', billing_id=COALESCE(@BillingId, billing_id),
                   paid_at=@Now, paid_by=@Uid, updated_at=@Now, updated_by=@Uid
             WHERE id=@Id AND tenant_id=@TId",
            new { BillingId = cmd.Request?.BillingId?.ToString(), Now = now, Uid = userId, Id = rid, TId = tid });

        await _audit.LogAsync("PAY", "ClsOrderRound", rid,
            new { billingId = cmd.Request?.BillingId, method = cmd.Request?.Method, amount = total }, ct);

        var dto = await ClsRoundSql.LoadRoundAsync(conn, tid, rid);
        return Result<ClsRoundResponse>.Success(dto!);
    }
}

// ────────────────────────────────────────────────
// WAIVE — mien / no vien phi
// ────────────────────────────────────────────────
public class WaiveClsRoundCommandHandler : IRequestHandler<WaiveClsRoundCommand, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public WaiveClsRoundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsRoundResponse>> Handle(WaiveClsRoundCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Request?.Reason))
            return Result<ClsRoundResponse>.Failure("CLS_WAIVE_REASON_REQUIRED", "Cần nhập lý do miễn/nợ viện phí");

        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var rid = cmd.RoundId.ToString();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(ClsRoundSql.SelectRound, new { Id = rid, TId = tid });
        if (row is null)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định");

        var pay = (string)row.payment_status;
        if (pay == ClsRoundPaymentStatus.Paid)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_ALREADY_PAID", "Đợt chỉ định đã thanh toán");
        if (!ClsRoundPaymentStatus.CanTransition(pay, ClsRoundPaymentStatus.Waived))
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái thanh toán từ {pay} sang {ClsRoundPaymentStatus.Waived}");

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync($@"
            UPDATE {ClsRoundSql.RoundTable}
               SET payment_status='WAIVED', waived_reason=@Reason, updated_at=@Now, updated_by=@Uid
             WHERE id=@Id AND tenant_id=@TId",
            new { Reason = cmd.Request.Reason, Now = now, Uid = _user.UserId?.ToString(), Id = rid, TId = tid });

        await _audit.LogAsync("CLS_ROUND_WAIVE", "ClsOrderRound", rid, AuditSeverity.WARN, false, null,
            new { reason = cmd.Request.Reason, totalAmount = Convert.ToDecimal(row.total_amount) }, ct);

        var dto = await ClsRoundSql.LoadRoundAsync(conn, tid, rid);
        return Result<ClsRoundResponse>.Success(dto!);
    }
}

// ────────────────────────────────────────────────
// CANCEL — huy dot
// ────────────────────────────────────────────────
public class CancelClsRoundCommandHandler : IRequestHandler<CancelClsRoundCommand, Result<ClsRoundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CancelClsRoundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsRoundResponse>> Handle(CancelClsRoundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var rid = cmd.RoundId.ToString();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(ClsRoundSql.SelectRound, new { Id = rid, TId = tid });
        if (row is null)
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định");

        var current = (string)row.status;
        if (!ClsRoundStatus.CanTransition(current, ClsRoundStatus.Cancelled))
            return Result<ClsRoundResponse>.Failure("CLS_ROUND_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái đợt từ {current} sang {ClsRoundStatus.Cancelled}");

        var now = DateTime.UtcNow;
        var uid = _user.UserId?.ToString();

        await conn.ExecuteAsync(
            $"UPDATE {ClsRoundSql.LabTable} SET status='cancelled', updated_at=@Now WHERE tenant_id=@TId AND round_id=@RId AND status='ordered' AND deleted_at IS NULL",
            new { Now = now, TId = tid, RId = rid });
        await conn.ExecuteAsync(
            $"UPDATE {ClsRoundSql.RadTable} SET status='cancelled', updated_at=@Now WHERE tenant_id=@TId AND round_id=@RId AND status IN ('ordered','scheduled') AND deleted_at IS NULL",
            new { Now = now, TId = tid, RId = rid });

        await conn.ExecuteAsync($@"
            UPDATE {ClsRoundSql.RoundTable}
               SET status='CANCELLED', cancel_reason=@Reason, updated_at=@Now, updated_by=@Uid
             WHERE id=@Id AND tenant_id=@TId",
            new { Reason = cmd.Reason, Now = now, Uid = uid, Id = rid, TId = tid });

        await _audit.LogAsync("CANCEL", "ClsOrderRound", rid, new { reason = cmd.Reason }, ct);

        var dto = await ClsRoundSql.LoadRoundAsync(conn, tid, rid);
        return Result<ClsRoundResponse>.Success(dto!);
    }
}
