using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.CLS;

// ────────────── DTOs ──────────────
public record LabOrderRequest(
    string TestCode,
    string? SampleType,
    string? Priority,
    DateTime? ScheduledFor,
    string? LabPartnerId,
    string? Note);

public record LabOrderResponse(
    Guid Id,
    Guid EncounterId,
    string TestCode,
    string TestName,
    string? SampleType,
    string Priority,
    string Status,
    DateTime OrderedAt,
    Guid? OrderedBy,
    DateTime? ScheduledFor,
    string? Note);

public record RadOrderRequest(
    string Modality,
    string? BodyPart,
    bool Contrast,
    string ProcedureCode,
    string? Priority,
    string? Note);

public record RadOrderResponse(
    Guid Id,
    Guid EncounterId,
    string Modality,
    string? BodyPart,
    bool Contrast,
    string ProcedureCode,
    string ProcedureName,
    string Priority,
    string Status,
    DateTime OrderedAt,
    Guid? OrderedBy,
    string? Note);

public record ClsCatalogItem(
    string Code,
    string Name,
    string Kind,
    string? SampleType,
    string? Modality,
    decimal? DefaultPrice,
    decimal? BhytPrice);

// ────────────── Commands ──────────────
public record CreateLabOrdersCommand(Guid EncounterId, IReadOnlyList<LabOrderRequest> Tests)
    : IRequest<Result<IReadOnlyList<LabOrderResponse>>>;

public record ListLabOrdersQuery(Guid EncounterId) : IRequest<Result<IReadOnlyList<LabOrderResponse>>>;

public record UpdateLabOrderStatusCommand(Guid OrderId, string Status, string? Note) : IRequest<Result<bool>>;

public record DeleteLabOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public record CreateRadOrdersCommand(Guid EncounterId, IReadOnlyList<RadOrderRequest> Orders)
    : IRequest<Result<IReadOnlyList<RadOrderResponse>>>;

public record ListRadOrdersQuery(Guid EncounterId) : IRequest<Result<IReadOnlyList<RadOrderResponse>>>;

public record UpdateRadOrderStatusCommand(Guid OrderId, string Status, string? Note) : IRequest<Result<bool>>;

public record DeleteRadOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public record SearchClsCatalogQuery(string? Q, string? Kind, int Limit)
    : IRequest<Result<IReadOnlyList<ClsCatalogItem>>>;

// ────────────────────────────────────────────────
// Create Lab Orders
// ────────────────────────────────────────────────
public class CreateLabOrdersCommandHandler
    : IRequestHandler<CreateLabOrdersCommand, Result<IReadOnlyList<LabOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateLabOrdersCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<IReadOnlyList<LabOrderResponse>>> Handle(CreateLabOrdersCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var enc = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_enc_encounters WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.EncounterId.ToString(), TId = _tenant.TenantId });
        if (enc is null) return Result<IReadOnlyList<LabOrderResponse>>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var results = new List<LabOrderResponse>();
        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        foreach (var test in cmd.Tests)
        {
            // Lookup test name
            var catalog = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name, sample_type FROM diab_his_dict_lab_tests WHERE code=@Code",
                new { Code = test.TestCode });
            var testName = (string?)catalog?.name ?? test.TestCode;
            var sampleType = test.SampleType ?? (string?)catalog?.sample_type;

            var id = Guid.NewGuid().ToString();
            var priority = test.Priority ?? ClsPriority.Normal;

            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_cli_lab_orders
                    (id, tenant_id, encounter_id, test_code, test_name, sample_type,
                     priority, status, ordered_at, ordered_by, scheduled_for, lab_partner_id, note,
                     created_at, created_by, updated_at)
                VALUES
                    (@Id, @TId, @EId, @Code, @Name, @Sample,
                     @Priority, 'ordered', @Now, @UserId, @SchedFor, @LabPartner, @Note,
                     @Now, @UserId, @Now)",
                new
                {
                    Id = id, TId = _tenant.TenantId, EId = cmd.EncounterId.ToString(),
                    Code = test.TestCode, Name = testName, Sample = sampleType,
                    Priority = priority, Now = now, UserId = userId,
                    SchedFor = test.ScheduledFor, LabPartner = test.LabPartnerId, Note = test.Note
                });

            results.Add(new LabOrderResponse(Guid.Parse(id), cmd.EncounterId, test.TestCode, testName,
                sampleType, priority, LabOrderStatus.Ordered, now,
                string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId), test.ScheduledFor, test.Note));
        }

        await _audit.LogAsync("CREATE", "LabOrders", cmd.EncounterId.ToString(), new { count = results.Count }, ct);
        return Result<IReadOnlyList<LabOrderResponse>>.Success(results.AsReadOnly());
    }
}

public class ListLabOrdersQueryHandler : IRequestHandler<ListLabOrdersQuery, Result<IReadOnlyList<LabOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListLabOrdersQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<LabOrderResponse>>> Handle(ListLabOrdersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_cli_lab_orders
            WHERE encounter_id=@EId AND tenant_id=@TId AND deleted_at IS NULL ORDER BY ordered_at",
            new { EId = q.EncounterId.ToString(), TId = _tenant.TenantId });

        var result = rows.Select(r => new LabOrderResponse(
            Guid.Parse((string)r.id), Guid.Parse((string)r.encounter_id),
            (string)r.test_code, (string)r.test_name, (string?)r.sample_type,
            (string)r.priority, (string)r.status, (DateTime)r.ordered_at,
            string.IsNullOrEmpty((string?)r.ordered_by) ? null : Guid.Parse((string)r.ordered_by),
            (DateTime?)r.scheduled_for, (string?)r.note)).ToList();

        return Result<IReadOnlyList<LabOrderResponse>>.Success(result.AsReadOnly());
    }
}

public class UpdateLabOrderStatusCommandHandler : IRequestHandler<UpdateLabOrderStatusCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public UpdateLabOrderStatusCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<bool>> Handle(UpdateLabOrderStatusCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_cli_lab_orders WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.OrderId.ToString(), TId = _tenant.TenantId });

        if (order is null) return Result<bool>.Failure("LAB_ORDER_NOT_FOUND", "Không tìm thấy chỉ định XN");

        if (!LabOrderStatus.CanTransition((string)order.status, cmd.Status))
            return Result<bool>.Failure("LAB_ORDER_INVALID_TRANSITION",
                $"Không thể chuyển từ {order.status} sang {cmd.Status}");

        await conn.ExecuteAsync("UPDATE diab_his_cli_lab_orders SET status=@Status, note=COALESCE(@Note, note), updated_at=@Now WHERE id=@Id",
            new { Id = cmd.OrderId.ToString(), Status = cmd.Status, Note = cmd.Note, Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

public class DeleteLabOrderCommandHandler : IRequestHandler<DeleteLabOrderCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public DeleteLabOrderCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<bool>> Handle(DeleteLabOrderCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_cli_lab_orders WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.OrderId.ToString(), TId = _tenant.TenantId });

        if (order is null) return Result<bool>.Failure("LAB_ORDER_NOT_FOUND", "Không tìm thấy chỉ định XN");
        if ((string)order.status != LabOrderStatus.Ordered)
            return Result<bool>.Failure("LAB_ORDER_CANNOT_DELETE", "Chỉ có thể xóa chỉ định XN ở trạng thái ordered");

        await conn.ExecuteAsync("UPDATE diab_his_cli_lab_orders SET deleted_at=@Now WHERE id=@Id",
            new { Id = cmd.OrderId.ToString(), Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Rad Orders
// ────────────────────────────────────────────────
public class CreateRadOrdersCommandHandler
    : IRequestHandler<CreateRadOrdersCommand, Result<IReadOnlyList<RadOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateRadOrdersCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<IReadOnlyList<RadOrderResponse>>> Handle(CreateRadOrdersCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var enc = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_enc_encounters WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.EncounterId.ToString(), TId = _tenant.TenantId });
        if (enc is null) return Result<IReadOnlyList<RadOrderResponse>>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var results = new List<RadOrderResponse>();
        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        foreach (var order in cmd.Orders)
        {
            var catalog = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT name FROM diab_his_dict_rad_procedures WHERE code=@Code",
                new { Code = order.ProcedureCode });
            var procName = (string?)catalog?.name ?? order.ProcedureCode;

            var id = Guid.NewGuid().ToString();
            var priority = order.Priority ?? ClsPriority.Normal;

            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_cli_rad_orders
                    (id, tenant_id, encounter_id, modality, body_part, contrast,
                     procedure_code, procedure_name, priority, status, ordered_at, ordered_by, note,
                     created_at, created_by, updated_at)
                VALUES
                    (@Id, @TId, @EId, @Mod, @Body, @Contrast,
                     @Code, @Name, @Priority, 'ordered', @Now, @UserId, @Note,
                     @Now, @UserId, @Now)",
                new
                {
                    Id = id, TId = _tenant.TenantId, EId = cmd.EncounterId.ToString(),
                    Mod = order.Modality, Body = order.BodyPart, Contrast = order.Contrast ? 1 : 0,
                    Code = order.ProcedureCode, Name = procName, Priority = priority,
                    Now = now, UserId = userId, Note = order.Note
                });

            results.Add(new RadOrderResponse(Guid.Parse(id), cmd.EncounterId, order.Modality, order.BodyPart,
                order.Contrast, order.ProcedureCode, procName, priority, RadOrderStatus.Ordered, now,
                string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId), order.Note));
        }

        await _audit.LogAsync("CREATE", "RadOrders", cmd.EncounterId.ToString(), new { count = results.Count }, ct);
        return Result<IReadOnlyList<RadOrderResponse>>.Success(results.AsReadOnly());
    }
}

public class ListRadOrdersQueryHandler : IRequestHandler<ListRadOrdersQuery, Result<IReadOnlyList<RadOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListRadOrdersQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<RadOrderResponse>>> Handle(ListRadOrdersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_cli_rad_orders
            WHERE encounter_id=@EId AND tenant_id=@TId AND deleted_at IS NULL ORDER BY ordered_at",
            new { EId = q.EncounterId.ToString(), TId = _tenant.TenantId });

        var result = rows.Select(r => new RadOrderResponse(
            Guid.Parse((string)r.id), Guid.Parse((string)r.encounter_id),
            (string)r.modality, (string?)r.body_part, (bool)((sbyte)r.contrast == 1),
            (string)r.procedure_code, (string)r.procedure_name,
            (string)r.priority, (string)r.status, (DateTime)r.ordered_at,
            string.IsNullOrEmpty((string?)r.ordered_by) ? null : Guid.Parse((string)r.ordered_by),
            (string?)r.note)).ToList();

        return Result<IReadOnlyList<RadOrderResponse>>.Success(result.AsReadOnly());
    }
}

public class UpdateRadOrderStatusCommandHandler : IRequestHandler<UpdateRadOrderStatusCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public UpdateRadOrderStatusCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<bool>> Handle(UpdateRadOrderStatusCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_cli_rad_orders WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.OrderId.ToString(), TId = _tenant.TenantId });

        if (order is null) return Result<bool>.Failure("RAD_ORDER_NOT_FOUND", "Không tìm thấy chỉ định CĐHA");

        if (!RadOrderStatus.CanTransition((string)order.status, cmd.Status))
            return Result<bool>.Failure("RAD_ORDER_INVALID_TRANSITION",
                $"Không thể chuyển từ {order.status} sang {cmd.Status}");

        await conn.ExecuteAsync("UPDATE diab_his_cli_rad_orders SET status=@Status, note=COALESCE(@Note, note), updated_at=@Now WHERE id=@Id",
            new { Id = cmd.OrderId.ToString(), Status = cmd.Status, Note = cmd.Note, Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

public class DeleteRadOrderCommandHandler : IRequestHandler<DeleteRadOrderCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public DeleteRadOrderCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<bool>> Handle(DeleteRadOrderCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_cli_rad_orders WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.OrderId.ToString(), TId = _tenant.TenantId });

        if (order is null) return Result<bool>.Failure("RAD_ORDER_NOT_FOUND", "Không tìm thấy chỉ định CĐHA");
        if ((string)order.status != RadOrderStatus.Ordered)
            return Result<bool>.Failure("RAD_ORDER_CANNOT_DELETE", "Chỉ có thể xóa chỉ định ở trạng thái ordered");

        await conn.ExecuteAsync("UPDATE diab_his_cli_rad_orders SET deleted_at=@Now WHERE id=@Id",
            new { Id = cmd.OrderId.ToString(), Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Catalog Search
// ────────────────────────────────────────────────
public class SearchClsCatalogQueryHandler : IRequestHandler<SearchClsCatalogQuery, Result<IReadOnlyList<ClsCatalogItem>>>
{
    private readonly IDapperConnectionFactory _db;

    public SearchClsCatalogQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<ClsCatalogItem>>> Handle(SearchClsCatalogQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var results = new List<ClsCatalogItem>();
        var limit = Math.Min(q.Limit, 50);
        var term = string.IsNullOrWhiteSpace(q.Q) ? "" : q.Q;

        if (string.IsNullOrEmpty(q.Kind) || q.Kind == "LAB")
        {
            var labs = await conn.QueryAsync<dynamic>(
                "SELECT code, name, sample_type, default_price, bhyt_price FROM diab_his_dict_lab_tests WHERE is_active=1 AND (name LIKE @Term OR code LIKE @Term) LIMIT @Limit",
                new { Term = $"%{term}%", Limit = limit });

            results.AddRange(labs.Select(r => new ClsCatalogItem((string)r.code, (string)r.name, "LAB",
                (string?)r.sample_type, null, (decimal?)r.default_price, (decimal?)r.bhyt_price)));
        }

        if (string.IsNullOrEmpty(q.Kind) || q.Kind == "RAD")
        {
            var rads = await conn.QueryAsync<dynamic>(
                "SELECT code, name, modality, default_price, bhyt_price FROM diab_his_dict_rad_procedures WHERE is_active=1 AND (name LIKE @Term OR code LIKE @Term) LIMIT @Limit",
                new { Term = $"%{term}%", Limit = limit });

            results.AddRange(rads.Select(r => new ClsCatalogItem((string)r.code, (string)r.name, "RAD",
                null, (string?)r.modality, (decimal?)r.default_price, (decimal?)r.bhyt_price)));
        }

        return Result<IReadOnlyList<ClsCatalogItem>>.Success(results.AsReadOnly());
    }
}
