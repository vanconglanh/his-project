using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LabPartners;

// ═══════════════════════════════════════════════
// FR-512 [P1]: Doi soat cong no/hoa hong voi doi tac XN.
// Moi LabOrder gui cho 1 LabPartner co the co 1 ban ghi chi phi (gia von
// phong kham tra doi tac - khac gia thu benh nhan). Cac ban ghi chi phi
// trong cung 1 thang duoc tong hop vao 1 ky doi soat (draft -> confirmed -> paid).
// ═══════════════════════════════════════════════

// ────────────── Commands / Queries ──────────────
public record CreateLabPartnerCostCommand(CreateLabPartnerCostRequest Req)
    : IRequest<Result<LabPartnerCostResponse>>;

public record UpdateLabPartnerCostCommand(Guid Id, UpdateLabPartnerCostRequest Req)
    : IRequest<Result<bool>>;

public record ListLabPartnerCostsQuery(Guid LabPartnerId, string? PeriodMonth, bool? Unreconciled)
    : IRequest<Result<IReadOnlyList<LabPartnerCostResponse>>>;

public record ListLabPartnerReconciliationsQuery(Guid LabPartnerId)
    : IRequest<Result<IReadOnlyList<LabPartnerReconciliationResponse>>>;

public record CreateLabPartnerReconciliationCommand(Guid LabPartnerId, CreateLabPartnerReconciliationRequest Req)
    : IRequest<Result<LabPartnerReconciliationResponse>>;

public record UpdateLabPartnerReconciliationStatusCommand(Guid Id, UpdateLabPartnerReconciliationStatusRequest Req)
    : IRequest<Result<bool>>;

// ────────────────────────────────────────────────
// Tao ban ghi chi phi cho 1 LabOrder
// ────────────────────────────────────────────────
public class CreateLabPartnerCostCommandHandler
    : IRequestHandler<CreateLabPartnerCostCommand, Result<LabPartnerCostResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateLabPartnerCostCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<LabPartnerCostResponse>> Handle(CreateLabPartnerCostCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var req = cmd.Req;

        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, lab_partner_id, ordered_at, branch_id
            FROM diab_his_cli_lab_orders
            WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = req.LabOrderId.ToString(), TId = _tenant.TenantId });

        if (order is null)
            return Result<LabPartnerCostResponse>.Failure("LAB_ORDER_NOT_FOUND", "Không tìm thấy chỉ định XN");

        string? partnerId = (string?)order.lab_partner_id;
        if (string.IsNullOrEmpty(partnerId))
            return Result<LabPartnerCostResponse>.Failure("LAB_ORDER_NO_PARTNER", "Chỉ định XN chưa gán đối tác lab");

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_int_lab_partner_costs WHERE lab_order_id=@Id AND deleted_at IS NULL",
            new { Id = req.LabOrderId.ToString() });
        if (existing is not null)
            return Result<LabPartnerCostResponse>.Failure("LAB_PARTNER_COST_EXISTS", "Chỉ định XN này đã có ghi nhận chi phí");

        decimal? costAmount = req.CostAmount;
        if (costAmount is null)
        {
            var partner = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT default_cost_amount FROM diab_his_int_lab_partners WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
                new { Id = partnerId, TId = _tenant.TenantId });
            costAmount = (decimal?)partner?.default_cost_amount;
        }
        if (costAmount is null)
            return Result<LabPartnerCostResponse>.Failure("LAB_PARTNER_COST_REQUIRED",
                "Chưa nhập chi phí và đối tác chưa có giá vốn mặc định");

        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var incurredAt = (DateTime)order.ordered_at;
        var periodMonth = incurredAt.ToString("yyyy-MM");
        var userId = _user.UserId?.ToString();
        var branchId = (int?)order.branch_id;

        var testCode = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT test_code FROM diab_his_cli_lab_orders WHERE id=@Id", new { Id = req.LabOrderId.ToString() });

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_int_lab_partner_costs
                (id, tenant_id, branch_id, lab_partner_id, lab_order_id, test_code, cost_amount, currency,
                 incurred_at, period_month, note, created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @BranchId, @PartnerId, @OrderId, @TestCode, @CostAmount, 'VND',
                 @IncurredAt, @PeriodMonth, @Note, @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId, BranchId = branchId, PartnerId = partnerId,
                OrderId = req.LabOrderId.ToString(), TestCode = testCode ?? string.Empty,
                CostAmount = costAmount.Value, IncurredAt = incurredAt, PeriodMonth = periodMonth,
                Note = req.Note, Now = now, UserId = userId
            });

        await _audit.LogAsync("CREATE", "LabPartnerCost", id, new { req.LabOrderId, costAmount }, ct);

        return Result<LabPartnerCostResponse>.Success(new LabPartnerCostResponse(
            Guid.Parse(id), Guid.Parse(partnerId), req.LabOrderId, testCode ?? string.Empty,
            costAmount.Value, "VND", incurredAt, periodMonth, null, req.Note, now));
    }
}

// ────────────────────────────────────────────────
// Cap nhat ban ghi chi phi (chi khi chua gan vao ky doi soat)
// ────────────────────────────────────────────────
public class UpdateLabPartnerCostCommandHandler : IRequestHandler<UpdateLabPartnerCostCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public UpdateLabPartnerCostCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<bool>> Handle(UpdateLabPartnerCostCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, reconciliation_id FROM diab_his_int_lab_partner_costs WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null) return Result<bool>.Failure("LAB_PARTNER_COST_NOT_FOUND", "Không tìm thấy ghi nhận chi phí");
        if (!string.IsNullOrEmpty((string?)row.reconciliation_id))
            return Result<bool>.Failure("LAB_PARTNER_COST_LOCKED", "Chi phí đã thuộc kỳ đối soát, không thể sửa");

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_lab_partner_costs SET cost_amount=@Amount, note=@Note, updated_at=@Now, updated_by=@UserId WHERE id=@Id",
            new { Id = cmd.Id.ToString(), Amount = cmd.Req.CostAmount, Note = cmd.Req.Note, Now = DateTime.UtcNow, UserId = _user.UserId?.ToString() });

        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Danh sach chi phi theo doi tac / ky
// ────────────────────────────────────────────────
public class ListLabPartnerCostsQueryHandler
    : IRequestHandler<ListLabPartnerCostsQuery, Result<IReadOnlyList<LabPartnerCostResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListLabPartnerCostsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<LabPartnerCostResponse>>> Handle(
        ListLabPartnerCostsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT * FROM diab_his_int_lab_partner_costs
                     WHERE tenant_id=@TId AND lab_partner_id=@PartnerId AND deleted_at IS NULL";
        if (!string.IsNullOrEmpty(q.PeriodMonth)) sql += " AND period_month=@PeriodMonth";
        if (q.Unreconciled == true) sql += " AND reconciliation_id IS NULL";
        sql += " ORDER BY incurred_at DESC";

        var rows = await conn.QueryAsync<dynamic>(sql,
            new { TId = _tenant.TenantId, PartnerId = q.LabPartnerId.ToString(), q.PeriodMonth });

        var result = rows.Select(Map).ToList();
        return Result<IReadOnlyList<LabPartnerCostResponse>>.Success(result.AsReadOnly());
    }

    internal static LabPartnerCostResponse Map(dynamic r) => new(
        Guid.Parse((string)r.id), Guid.Parse((string)r.lab_partner_id), Guid.Parse((string)r.lab_order_id),
        (string)r.test_code, (decimal)r.cost_amount, (string)r.currency, (DateTime)r.incurred_at,
        (string)r.period_month,
        string.IsNullOrEmpty((string?)r.reconciliation_id) ? null : Guid.Parse((string)r.reconciliation_id),
        (string?)r.note, (DateTime)r.created_at);
}

// ────────────────────────────────────────────────
// Danh sach ky doi soat theo doi tac
// ────────────────────────────────────────────────
public class ListLabPartnerReconciliationsQueryHandler
    : IRequestHandler<ListLabPartnerReconciliationsQuery, Result<IReadOnlyList<LabPartnerReconciliationResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListLabPartnerReconciliationsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<LabPartnerReconciliationResponse>>> Handle(
        ListLabPartnerReconciliationsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT r.*, lp.name AS partner_name
            FROM diab_his_int_lab_partner_reconciliations r
            JOIN diab_his_int_lab_partners lp ON lp.id = r.lab_partner_id
            WHERE r.tenant_id=@TId AND r.lab_partner_id=@PartnerId AND r.deleted_at IS NULL
            ORDER BY r.period_month DESC",
            new { TId = _tenant.TenantId, PartnerId = q.LabPartnerId.ToString() });

        var result = rows.Select(r => new LabPartnerReconciliationResponse(
            Guid.Parse((string)r.id), Guid.Parse((string)r.lab_partner_id), (string)r.partner_name,
            (string)r.period_month, (int)r.total_orders, (decimal)r.total_cost, (string)r.currency,
            (string)r.status, (DateTime?)r.confirmed_at, (DateTime?)r.paid_at, (string?)r.note,
            (DateTime)r.created_at)).ToList();

        return Result<IReadOnlyList<LabPartnerReconciliationResponse>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Tao ky doi soat (gom toan bo chi phi CHUA gan ky trong thang do)
// ────────────────────────────────────────────────
public class CreateLabPartnerReconciliationCommandHandler
    : IRequestHandler<CreateLabPartnerReconciliationCommand, Result<LabPartnerReconciliationResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateLabPartnerReconciliationCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<LabPartnerReconciliationResponse>> Handle(
        CreateLabPartnerReconciliationCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var partner = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, name FROM diab_his_int_lab_partners WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.LabPartnerId.ToString(), TId = _tenant.TenantId });
        if (partner is null)
            return Result<LabPartnerReconciliationResponse>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id FROM diab_his_int_lab_partner_reconciliations
               WHERE tenant_id=@TId AND lab_partner_id=@PartnerId AND period_month=@Period AND deleted_at IS NULL",
            new { TId = _tenant.TenantId, PartnerId = cmd.LabPartnerId.ToString(), Period = cmd.Req.PeriodMonth });
        if (existing is not null)
            return Result<LabPartnerReconciliationResponse>.Failure("LAB_PARTNER_RECONCILIATION_EXISTS",
                "Kỳ đối soát tháng này đã tồn tại");

        var costs = (await conn.QueryAsync<dynamic>(
            @"SELECT id, cost_amount FROM diab_his_int_lab_partner_costs
               WHERE tenant_id=@TId AND lab_partner_id=@PartnerId AND period_month=@Period
                 AND reconciliation_id IS NULL AND deleted_at IS NULL",
            new { TId = _tenant.TenantId, PartnerId = cmd.LabPartnerId.ToString(), Period = cmd.Req.PeriodMonth })).ToList();

        if (costs.Count == 0)
            return Result<LabPartnerReconciliationResponse>.Failure("LAB_PARTNER_COST_EMPTY",
                "Không có chi phí nào chưa đối soát trong kỳ này");

        var totalCost = costs.Sum(c => (decimal)c.cost_amount);
        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_int_lab_partner_reconciliations
                (id, tenant_id, lab_partner_id, period_month, total_orders, total_cost, currency, status,
                 note, created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @PartnerId, @Period, @Count, @Total, 'VND', 'draft',
                 @Note, @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId, PartnerId = cmd.LabPartnerId.ToString(),
                Period = cmd.Req.PeriodMonth, Count = costs.Count, Total = totalCost,
                Note = cmd.Req.Note, Now = now, UserId = userId
            });

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_lab_partner_costs SET reconciliation_id=@RecId, updated_at=@Now WHERE id IN @Ids",
            new { RecId = id, Now = now, Ids = costs.Select(c => (string)c.id).ToArray() });

        await _audit.LogAsync("CREATE", "LabPartnerReconciliation", id,
            new { cmd.LabPartnerId, cmd.Req.PeriodMonth, totalCost, count = costs.Count }, ct);

        return Result<LabPartnerReconciliationResponse>.Success(new LabPartnerReconciliationResponse(
            Guid.Parse(id), cmd.LabPartnerId, (string)partner.name, cmd.Req.PeriodMonth, costs.Count, totalCost,
            "VND", "draft", null, null, cmd.Req.Note, now));
    }
}

// ────────────────────────────────────────────────
// Chuyen trang thai ky doi soat: draft -> confirmed -> paid
// ────────────────────────────────────────────────
public class UpdateLabPartnerReconciliationStatusCommandHandler
    : IRequestHandler<UpdateLabPartnerReconciliationStatusCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public UpdateLabPartnerReconciliationStatusCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<bool>> Handle(UpdateLabPartnerReconciliationStatusCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_int_lab_partner_reconciliations WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null) return Result<bool>.Failure("LAB_PARTNER_RECONCILIATION_NOT_FOUND", "Không tìm thấy kỳ đối soát");

        var from = (string)row.status;
        var to = cmd.Req.Status;
        if (!Domain.Entities.LabPartnerReconciliationStatus.CanTransition(from, to))
            return Result<bool>.Failure("LAB_PARTNER_RECONCILIATION_INVALID_TRANSITION",
                $"Không thể chuyển từ {from} sang {to}");

        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        if (to == Domain.Entities.LabPartnerReconciliationStatus.Confirmed)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_lab_partner_reconciliations SET status=@Status, confirmed_at=@Now, confirmed_by=@UserId, note=COALESCE(@Note,note), updated_at=@Now, updated_by=@UserId WHERE id=@Id",
                new { Id = cmd.Id.ToString(), Status = to, Now = now, UserId = userId, cmd.Req.Note });
        }
        else if (to == Domain.Entities.LabPartnerReconciliationStatus.Paid)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_lab_partner_reconciliations SET status=@Status, paid_at=@Now, paid_by=@UserId, note=COALESCE(@Note,note), updated_at=@Now, updated_by=@UserId WHERE id=@Id",
                new { Id = cmd.Id.ToString(), Status = to, Now = now, UserId = userId, cmd.Req.Note });
        }

        await _audit.LogAsync("UPDATE_STATUS", "LabPartnerReconciliation", cmd.Id.ToString(), new { from, to }, ct);
        return Result<bool>.Success(true);
    }
}
