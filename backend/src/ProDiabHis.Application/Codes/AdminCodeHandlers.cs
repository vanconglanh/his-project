using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Codes;

// ────────────────────────────────────────────────
// DTOs (Admin)
// ────────────────────────────────────────────────
public record AdminCodeGroupDto(string Id, string Name, bool IsSystem, bool IsActive);

public record AdminCodeDetailDto(
    string Id, string Code, string Name, string? NameEn, int SortOrder, bool IsActive,
    bool IsHidden, bool IsSystem, int? TenantId, string? Extra, bool IsOverride, bool IsDefault);

// ────────────────────────────────────────────────
// Queries
// ────────────────────────────────────────────────
public record GetAdminCodeGroupsQuery() : IRequest<Result<IReadOnlyList<AdminCodeGroupDto>>>;

public record GetAdminCodeDetailsQuery(string GroupId) : IRequest<Result<IReadOnlyList<AdminCodeDetailDto>>>;

// ────────────────────────────────────────────────
// Commands
// ────────────────────────────────────────────────
public record CreateCodeDetailCommand(string GroupId, string Code, string Name, string? NameEn, int? SortOrder, string? Extra)
    : IRequest<Result<AdminCodeDetailDto>>;

public record UpdateCodeDetailCommand(string GroupId, string Id, string Name, string? NameEn, int? SortOrder, bool? IsActive, string? Extra)
    : IRequest<Result<AdminCodeDetailDto>>;

public record SetCodeVisibilityCommand(string GroupId, string Code, bool IsHidden) : IRequest<Result>;

public record DeleteCodeDetailCommand(string GroupId, string Id) : IRequest<Result>;

// ────────────────────────────────────────────────
// Helper: invalidate cache CodeResolver dung (tach rieng de khong phu thuoc Infrastructure)
// ────────────────────────────────────────────────
internal static class CodeCache
{
    public static void Invalidate(IMemoryCache cache, int tenantId, string groupId)
    {
        cache.Remove($"code_resolver:{tenantId}:{groupId}");
        cache.Remove($"code_resolver:0:{groupId}"); // phong khi tenantId=0 (chua co tenant) van duoc query
    }
}

// ────────────────────────────────────────────────
// GET /admin/codes — danh sach nhom
// ────────────────────────────────────────────────
public class GetAdminCodeGroupsQueryHandler : IRequestHandler<GetAdminCodeGroupsQuery, Result<IReadOnlyList<AdminCodeGroupDto>>>
{
    private readonly IDapperConnectionFactory _db;

    public GetAdminCodeGroupsQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<IReadOnlyList<AdminCodeGroupDto>>> Handle(GetAdminCodeGroupsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT id, name, is_system, is_active
            FROM diab_his_sys_code_master
            ORDER BY sort_order, id");

        var result = rows.Select(r => new AdminCodeGroupDto(
            (string)r.id, (string)r.name, Convert.ToBoolean(r.is_system), Convert.ToBoolean(r.is_active))).ToList();
        return Result<IReadOnlyList<AdminCodeGroupDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// GET /admin/codes/{groupId}/details — full rows da resolve theo tenant
// ────────────────────────────────────────────────
public class GetAdminCodeDetailsQueryHandler : IRequestHandler<GetAdminCodeDetailsQuery, Result<IReadOnlyList<AdminCodeDetailDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetAdminCodeDetailsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<IReadOnlyList<AdminCodeDetailDto>>> Handle(GetAdminCodeDetailsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var groupExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_code_master WHERE id = @GroupId", new { q.GroupId });
        if (groupExists == 0)
            return Result<IReadOnlyList<AdminCodeDetailDto>>.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        var rows = (await conn.QueryAsync<dynamic>(@"
            SELECT id, code, name, name_en, sort_order, is_active, is_hidden, is_system, tenant_id, extra
            FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId AND (tenant_id IS NULL OR tenant_id = @TenantId)
            ORDER BY sort_order, code, tenant_id",
            new { q.GroupId, TenantId = _tenant.TenantId })).ToList();

        var codesWithGlobal = rows.Where(r => r.tenant_id is null).Select(r => (string)r.code).ToHashSet(StringComparer.Ordinal);

        var result = rows.Select(r => new AdminCodeDetailDto(
            (string)r.id.ToString(),
            (string)r.code,
            (string)r.name,
            r.name_en is null ? null : (string)r.name_en,
            (int)r.sort_order,
            Convert.ToBoolean(r.is_active),
            Convert.ToBoolean(r.is_hidden),
            Convert.ToBoolean(r.is_system),
            r.tenant_id is null ? (int?)null : (int)r.tenant_id,
            r.extra is null ? null : (string)r.extra,
            r.tenant_id is not null,
            codesWithGlobal.Contains((string)r.code)
        )).ToList();

        return Result<IReadOnlyList<AdminCodeDetailDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// POST /admin/codes/{groupId}/details — tao ma rieng tenant
// ────────────────────────────────────────────────
public class CreateCodeDetailCommandHandler : IRequestHandler<CreateCodeDetailCommand, Result<AdminCodeDetailDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IMemoryCache _cache;

    public CreateCodeDetailCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IMemoryCache cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result<AdminCodeDetailDto>> Handle(CreateCodeDetailCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var groupExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_code_master WHERE id = @GroupId", new { cmd.GroupId });
        if (groupExists == 0)
            return Result<AdminCodeDetailDto>.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        var dup = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId AND code = @Code AND tenant_id = @TenantId",
            new { cmd.GroupId, cmd.Code, TenantId = _tenant.TenantId });
        if (dup > 0)
            return Result<AdminCodeDetailDto>.Failure("CODE_DUPLICATED", "Mã đã tồn tại");

        var id = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_sys_code_detail
                (id, code_master_id, code, name, name_en, sort_order, is_active, tenant_id, is_system, is_hidden, extra)
            VALUES
                (@Id, @GroupId, @Code, @Name, @NameEn, @SortOrder, 1, @TenantId, 0, 0, @Extra)",
            new
            {
                Id = id,
                cmd.GroupId,
                cmd.Code,
                cmd.Name,
                NameEn = cmd.NameEn,
                SortOrder = cmd.SortOrder ?? 0,
                TenantId = _tenant.TenantId,
                Extra = cmd.Extra
            });

        CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);

        return Result<AdminCodeDetailDto>.Success(new AdminCodeDetailDto(
            id, cmd.Code, cmd.Name, cmd.NameEn, cmd.SortOrder ?? 0, true, false, false, _tenant.TenantId, cmd.Extra, true, false));
    }
}

// ────────────────────────────────────────────────
// PUT /admin/codes/{groupId}/details/{id}
// ────────────────────────────────────────────────
public class UpdateCodeDetailCommandHandler : IRequestHandler<UpdateCodeDetailCommand, Result<AdminCodeDetailDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IMemoryCache _cache;

    public UpdateCodeDetailCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IMemoryCache cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result<AdminCodeDetailDto>> Handle(UpdateCodeDetailCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, code, name, name_en, sort_order, is_active, is_hidden, is_system, tenant_id, extra
            FROM diab_his_sys_code_detail
            WHERE id = @Id AND code_master_id = @GroupId",
            new { cmd.Id, cmd.GroupId });

        if (row is null)
            return Result<AdminCodeDetailDto>.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        int? rowTenantId = row.tenant_id is null ? null : (int)row.tenant_id;
        bool isSystem = Convert.ToBoolean(row.is_system);

        // Ma he thong global (tenant_id NULL, is_system=1) -> khong cho sua truc tiep,
        // thay vao do tao/ghi de ban override rieng cua tenant hien tai cho cung code.
        if (rowTenantId is null && isSystem)
        {
            string code = (string)row.code;
            var overrideRow = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT id FROM diab_his_sys_code_detail
                WHERE code_master_id = @GroupId AND code = @Code AND tenant_id = @TenantId",
                new { cmd.GroupId, Code = code, TenantId = _tenant.TenantId });

            var name = cmd.Name;
            var nameEn = cmd.NameEn;
            var sortOrder = cmd.SortOrder ?? (int)row.sort_order;
            var isActive = cmd.IsActive ?? Convert.ToBoolean(row.is_active);
            var extra = cmd.Extra;

            if (overrideRow is null)
            {
                var newId = Guid.NewGuid().ToString();
                await conn.ExecuteAsync(@"
                    INSERT INTO diab_his_sys_code_detail
                        (id, code_master_id, code, name, name_en, sort_order, is_active, tenant_id, is_system, is_hidden, extra)
                    VALUES
                        (@Id, @GroupId, @Code, @Name, @NameEn, @SortOrder, @IsActive, @TenantId, 0, 0, @Extra)",
                    new { Id = newId, cmd.GroupId, Code = code, Name = name, NameEn = nameEn, SortOrder = sortOrder, IsActive = isActive, TenantId = _tenant.TenantId, Extra = extra });

                CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);
                return Result<AdminCodeDetailDto>.Success(new AdminCodeDetailDto(
                    newId, code, name, nameEn, sortOrder, isActive, false, false, _tenant.TenantId, extra, true, true));
            }
            else
            {
                string overrideId = overrideRow.id.ToString();
                await conn.ExecuteAsync(@"
                    UPDATE diab_his_sys_code_detail
                    SET name = @Name, name_en = @NameEn, sort_order = @SortOrder, is_active = @IsActive, extra = @Extra
                    WHERE id = @Id",
                    new { Id = overrideId, Name = name, NameEn = nameEn, SortOrder = sortOrder, IsActive = isActive, Extra = extra });

                CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);
                return Result<AdminCodeDetailDto>.Success(new AdminCodeDetailDto(
                    overrideId, code, name, nameEn, sortOrder, isActive, false, false, _tenant.TenantId, extra, true, true));
            }
        }

        // Row thuoc tenant khac -> 404
        if (rowTenantId is not null && rowTenantId != _tenant.TenantId)
            return Result<AdminCodeDetailDto>.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        // Row cua chinh tenant hien tai -> sua truc tiep
        var name2 = cmd.Name;
        var nameEn2 = cmd.NameEn;
        var sortOrder2 = cmd.SortOrder ?? (int)row.sort_order;
        var isActive2 = cmd.IsActive ?? Convert.ToBoolean(row.is_active);
        var extra2 = cmd.Extra;

        await conn.ExecuteAsync(@"
            UPDATE diab_his_sys_code_detail
            SET name = @Name, name_en = @NameEn, sort_order = @SortOrder, is_active = @IsActive, extra = @Extra
            WHERE id = @Id",
            new { Id = cmd.Id, Name = name2, NameEn = nameEn2, SortOrder = sortOrder2, IsActive = isActive2, Extra = extra2 });

        CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);

        return Result<AdminCodeDetailDto>.Success(new AdminCodeDetailDto(
            cmd.Id, (string)row.code, name2, nameEn2, sortOrder2, isActive2, Convert.ToBoolean(row.is_hidden), isSystem, rowTenantId, extra2, rowTenantId is not null, false));
    }
}

// ────────────────────────────────────────────────
// PATCH /admin/codes/{groupId}/details/{code}/visibility
// ────────────────────────────────────────────────
public class SetCodeVisibilityCommandHandler : IRequestHandler<SetCodeVisibilityCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IMemoryCache _cache;

    public SetCodeVisibilityCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IMemoryCache cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result> Handle(SetCodeVisibilityCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var globalRow = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT name, name_en, sort_order, extra FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId AND code = @Code AND tenant_id IS NULL",
            new { cmd.GroupId, cmd.Code });
        if (globalRow is null)
            return Result.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        var existingTenantRow = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId AND code = @Code AND tenant_id = @TenantId",
            new { cmd.GroupId, cmd.Code, TenantId = _tenant.TenantId });

        if (existingTenantRow is not null)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_sys_code_detail SET is_hidden = @IsHidden WHERE id = @Id",
                new { cmd.IsHidden, Id = (string)existingTenantRow.id.ToString() });
        }
        else
        {
            var newId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_sys_code_detail
                    (id, code_master_id, code, name, name_en, sort_order, is_active, tenant_id, is_system, is_hidden, extra)
                VALUES
                    (@Id, @GroupId, @Code, @Name, @NameEn, @SortOrder, 1, @TenantId, 0, @IsHidden, @Extra)",
                new
                {
                    Id = newId,
                    cmd.GroupId,
                    cmd.Code,
                    Name = (string)globalRow.name,
                    NameEn = globalRow.name_en is null ? null : (string)globalRow.name_en,
                    SortOrder = (int)globalRow.sort_order,
                    TenantId = _tenant.TenantId,
                    cmd.IsHidden,
                    Extra = globalRow.extra is null ? null : (string)globalRow.extra
                });
        }

        CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);
        return Result.Success();
    }
}

// ────────────────────────────────────────────────
// DELETE /admin/codes/{groupId}/details/{id}
// ────────────────────────────────────────────────
public class DeleteCodeDetailCommandHandler : IRequestHandler<DeleteCodeDetailCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IMemoryCache _cache;

    public DeleteCodeDetailCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IMemoryCache cache)
    {
        _db = db;
        _tenant = tenant;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteCodeDetailCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT tenant_id, is_system FROM diab_his_sys_code_detail
            WHERE id = @Id AND code_master_id = @GroupId",
            new { cmd.Id, cmd.GroupId });
        if (row is null)
            return Result.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        int? rowTenantId = row.tenant_id is null ? null : (int)row.tenant_id;
        bool isSystem = Convert.ToBoolean(row.is_system);

        if (isSystem || rowTenantId is null)
            return Result.Failure("CODE_IS_SYSTEM_READONLY", "Mã hệ thống không được xoá, chỉ có thể ẩn");

        if (rowTenantId != _tenant.TenantId)
            return Result.Failure("CODE_GROUP_NOT_FOUND", "Không tìm thấy nhóm mã");

        await conn.ExecuteAsync("DELETE FROM diab_his_sys_code_detail WHERE id = @Id", new { cmd.Id });

        CodeCache.Invalidate(_cache, _tenant.TenantId, cmd.GroupId);
        return Result.Success();
    }
}
