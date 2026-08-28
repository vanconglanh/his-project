using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Telehealth;

// ═══════════════════════════════════════════════
// Admin: CRUD mapping dich vu telehealth HIS <-> Docosan
// ═══════════════════════════════════════════════
public record ListServiceMappingsQuery() : IRequest<Result<IReadOnlyList<ServiceMappingResponse>>>;

public record CreateServiceMappingCommand(ServiceMappingRequest Request)
    : IRequest<Result<ServiceMappingResponse>>;

public record UpdateServiceMappingCommand(Guid Id, ServiceMappingRequest Request)
    : IRequest<Result<ServiceMappingResponse>>;

public class ListServiceMappingsQueryHandler
    : IRequestHandler<ListServiceMappingsQuery, Result<IReadOnlyList<ServiceMappingResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListServiceMappingsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<ServiceMappingResponse>>> Handle(ListServiceMappingsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_service_mapping
            WHERE tenant_id=@TId AND deleted_at IS NULL ORDER BY created_at DESC",
            new { TId = _tenant.TenantId });

        var items = rows.Select(Map).ToList();
        return Result<IReadOnlyList<ServiceMappingResponse>>.Success(items.AsReadOnly());
    }

    internal static ServiceMappingResponse Map(dynamic r) => new(
        Guid.Parse((string)r.id),
        r.his_service_id is not null ? Guid.Parse((string)r.his_service_id) : (Guid?)null,
        (int)r.docosan_service_id,
        (string)r.docosan_service_type,
        (string?)r.service_name,
        (int)r.default_quantity,
        (string)r.environment,
        Convert.ToBoolean(r.is_active),
        (DateTime)r.created_at);
}

public class CreateServiceMappingCommandHandler
    : IRequestHandler<CreateServiceMappingCommand, Result<ServiceMappingResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public CreateServiceMappingCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<ServiceMappingResponse>> Handle(CreateServiceMappingCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (req.DocosanServiceId <= 0)
            return Result<ServiceMappingResponse>.Failure("VALIDATION_ERROR", "docosan_service_id không hợp lệ");
        if (string.IsNullOrWhiteSpace(req.DocosanServiceType))
            return Result<ServiceMappingResponse>.Failure("VALIDATION_ERROR", "docosan_service_type không được để trống");

        using var conn = _db.CreateConnection();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        try
        {
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_int_docosan_service_mapping
                    (id, tenant_id, his_service_id, docosan_service_id, docosan_service_type,
                     service_name, default_quantity, environment, is_active, created_at, updated_at)
                VALUES
                    (@Id, @TId, @HisServiceId, @DocosanServiceId, @DocosanServiceType,
                     @ServiceName, @DefaultQuantity, @Environment, @IsActive, @Now, @Now)",
                new
                {
                    Id = id.ToString(), TId = _tenant.TenantId, HisServiceId = req.HisServiceId?.ToString(),
                    req.DocosanServiceId, req.DocosanServiceType, req.ServiceName,
                    DefaultQuantity = req.DefaultQuantity <= 0 ? 1 : req.DefaultQuantity,
                    Environment = string.IsNullOrWhiteSpace(req.Environment) ? "production" : req.Environment,
                    req.IsActive, Now = now
                });
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ServiceMappingResponse>.Failure("TELEHEALTH_SERVICE_MAPPING_DUPLICATE",
                "Dịch vụ Docosan này đã được cấu hình cho tenant/môi trường này");
        }

        await _audit.LogAsync("CREATE", "TelehealthServiceMapping", id.ToString(), new { req.DocosanServiceId }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_docosan_service_mapping WHERE id=@Id", new { Id = id.ToString() });
        return Result<ServiceMappingResponse>.Success(ListServiceMappingsQueryHandler.Map(row));
    }
}

public class UpdateServiceMappingCommandHandler
    : IRequestHandler<UpdateServiceMappingCommand, Result<ServiceMappingResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public UpdateServiceMappingCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<ServiceMappingResponse>> Handle(UpdateServiceMappingCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_int_docosan_service_mapping WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });
        if (existing is null)
            return Result<ServiceMappingResponse>.Failure("TELEHEALTH_SERVICE_MAPPING_NOT_FOUND", "Không tìm thấy mapping dịch vụ");

        var req = cmd.Request;
        await conn.ExecuteAsync(@"
            UPDATE diab_his_int_docosan_service_mapping
            SET his_service_id=@HisServiceId, docosan_service_id=@DocosanServiceId,
                docosan_service_type=@DocosanServiceType, service_name=@ServiceName,
                default_quantity=@DefaultQuantity, environment=@Environment, is_active=@IsActive, updated_at=@Now
            WHERE id=@Id",
            new
            {
                Id = cmd.Id.ToString(), HisServiceId = req.HisServiceId?.ToString(),
                req.DocosanServiceId, req.DocosanServiceType, req.ServiceName,
                DefaultQuantity = req.DefaultQuantity <= 0 ? 1 : req.DefaultQuantity,
                Environment = string.IsNullOrWhiteSpace(req.Environment) ? "production" : req.Environment,
                req.IsActive, Now = DateTime.UtcNow
            });

        await _audit.LogAsync("UPDATE", "TelehealthServiceMapping", cmd.Id.ToString(), new { req.DocosanServiceId }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_docosan_service_mapping WHERE id=@Id", new { Id = cmd.Id.ToString() });
        return Result<ServiceMappingResponse>.Success(ListServiceMappingsQueryHandler.Map(row));
    }
}
