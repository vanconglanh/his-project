using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Telehealth;

// ═══════════════════════════════════════════════
// FR-804: Admin CRUD danh muc ICD-10 duoc phep tu van tu xa (configurable theo tenant,
// KHONG hardcode). Xem TelehealthIcd10Guard (TelehealthHandlers.cs) cho logic kiem tra.
// ═══════════════════════════════════════════════
public class ListAllowedIcd10QueryHandler
    : IRequestHandler<ListAllowedIcd10Query, Result<IReadOnlyList<AllowedIcd10Response>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListAllowedIcd10QueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<AllowedIcd10Response>>> Handle(ListAllowedIcd10Query q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_tel_allowed_icd10
            WHERE tenant_id=@TId AND deleted_at IS NULL
            ORDER BY icd10_code",
            new { TId = _tenant.TenantId });

        var items = rows.Select(Map).ToList();
        return Result<IReadOnlyList<AllowedIcd10Response>>.Success(items.AsReadOnly());
    }

    internal static AllowedIcd10Response Map(dynamic r) => new(
        Guid.Parse((string)r.id),
        (string)r.icd10_code,
        (string)r.icd10_name,
        Convert.ToBoolean(r.is_active),
        (string?)r.note,
        (DateTime)r.created_at,
        (DateTime)r.updated_at);
}

public class CreateAllowedIcd10CommandHandler
    : IRequestHandler<CreateAllowedIcd10Command, Result<AllowedIcd10Response>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public CreateAllowedIcd10CommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<AllowedIcd10Response>> Handle(CreateAllowedIcd10Command cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (string.IsNullOrWhiteSpace(req.Icd10Code))
            return Result<AllowedIcd10Response>.Failure("VALIDATION_ERROR", "Mã ICD-10 không được để trống");
        if (string.IsNullOrWhiteSpace(req.Icd10Name))
            return Result<AllowedIcd10Response>.Failure("VALIDATION_ERROR", "Tên chẩn đoán không được để trống");

        using var conn = _db.CreateConnection();
        var code = req.Icd10Code.Trim().ToUpperInvariant();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        try
        {
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_tel_allowed_icd10
                    (id, tenant_id, icd10_code, icd10_name, is_active, note, created_at, updated_at)
                VALUES
                    (@Id, @TId, @Code, @Name, @IsActive, @Note, @Now, @Now)",
                new { Id = id.ToString(), TId = _tenant.TenantId, Code = code, Name = req.Icd10Name, req.IsActive, req.Note, Now = now });
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return Result<AllowedIcd10Response>.Failure("TELEHEALTH_ICD10_DUPLICATE",
                $"Mã ICD-10 '{code}' đã có trong danh mục tư vấn từ xa");
        }

        await _audit.LogAsync("CREATE", "TelehealthAllowedIcd10", id.ToString(), new { code }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_tel_allowed_icd10 WHERE id=@Id", new { Id = id.ToString() });
        return Result<AllowedIcd10Response>.Success(ListAllowedIcd10QueryHandler.Map(row));
    }
}

public class UpdateAllowedIcd10CommandHandler
    : IRequestHandler<UpdateAllowedIcd10Command, Result<AllowedIcd10Response>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public UpdateAllowedIcd10CommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<AllowedIcd10Response>> Handle(UpdateAllowedIcd10Command cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_tel_allowed_icd10 WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });
        if (existing is null)
            return Result<AllowedIcd10Response>.Failure("TELEHEALTH_ICD10_NOT_FOUND", "Không tìm thấy mã ICD-10 trong danh mục");

        var req = cmd.Request;
        if (string.IsNullOrWhiteSpace(req.Icd10Name))
            return Result<AllowedIcd10Response>.Failure("VALIDATION_ERROR", "Tên chẩn đoán không được để trống");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_tel_allowed_icd10
            SET icd10_name=@Name, is_active=@IsActive, note=@Note, updated_at=@Now
            WHERE id=@Id AND tenant_id=@TId",
            new { Name = req.Icd10Name, req.IsActive, req.Note, Now = DateTime.UtcNow, Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        await _audit.LogAsync("UPDATE", "TelehealthAllowedIcd10", cmd.Id.ToString(), new { req.IsActive }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_tel_allowed_icd10 WHERE id=@Id", new { Id = cmd.Id.ToString() });
        return Result<AllowedIcd10Response>.Success(ListAllowedIcd10QueryHandler.Map(row));
    }
}

public class DeleteAllowedIcd10CommandHandler : IRequestHandler<DeleteAllowedIcd10Command, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public DeleteAllowedIcd10CommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<bool>> Handle(DeleteAllowedIcd10Command cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_tel_allowed_icd10 WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });
        if (existing is null)
            return Result<bool>.Failure("TELEHEALTH_ICD10_NOT_FOUND", "Không tìm thấy mã ICD-10 trong danh mục");

        await conn.ExecuteAsync(
            "UPDATE diab_his_tel_allowed_icd10 SET deleted_at=@Now, updated_at=@Now WHERE id=@Id AND tenant_id=@TId",
            new { Now = DateTime.UtcNow, Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        await _audit.LogAsync("DELETE", "TelehealthAllowedIcd10", cmd.Id.ToString(), null, ct);
        return Result<bool>.Success(true);
    }
}
