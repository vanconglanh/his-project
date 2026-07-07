using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Rooms;

// ─── Commands & Queries ───────────────────────────────────────────────────────

public record ListRoomsAdminQuery(bool? IsActive, int Page, int PageSize)
    : IRequest<PagedResult<RoomAdminResponse>>;

public record GetRoomQuery(string Id)
    : IRequest<Result<RoomAdminResponse>>;

public record CreateRoomCommand(CreateRoomRequest Request)
    : IRequest<Result<RoomAdminResponse>>;

public record UpdateRoomCommand(string Id, UpdateRoomRequest Request)
    : IRequest<Result<RoomAdminResponse>>;

public record DeleteRoomCommand(string Id)
    : IRequest<Result<bool>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────

public class ListRoomsAdminHandler : IRequestHandler<ListRoomsAdminQuery, PagedResult<RoomAdminResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListRoomsAdminHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RoomAdminResponse>> Handle(ListRoomsAdminQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = "WHERE (tenant_id = @tenantId OR tenant_id IS NULL) AND deleted_at IS NULL";
        if (q.IsActive.HasValue)
            where += " AND is_active = @isActive";

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_sys_rooms {where}",
            new { tenantId, isActive = q.IsActive.HasValue ? (q.IsActive.Value ? 1 : 0) : (int?)null });

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT id, tenant_id, code AS room_code, name, capacity AS max_per_day, is_active, created_at, updated_at FROM diab_his_sys_rooms {where} ORDER BY code LIMIT @limit OFFSET @offset",
            new { tenantId, isActive = q.IsActive.HasValue ? (q.IsActive.Value ? 1 : 0) : (int?)null, limit = q.PageSize, offset });

        var items = rows.Select(r => new RoomAdminResponse(
            r.id.ToString(),
            (int?)r.tenant_id,
            (string)r.room_code,
            (string)r.name,
            (int)r.max_per_day,
            RoomBoolHelper.ToBool(r.is_active),
            (DateTime)r.created_at,
            (DateTime)r.updated_at)).ToList();

        return new PagedResult<RoomAdminResponse>(items, q.Page, q.PageSize, total);
    }

}

internal static class RoomBoolHelper
{
    internal static bool ToBool(dynamic val)
    {
        if (val is bool b) return b;
        if (val is int i) return i != 0;
        if (val is sbyte sb) return sb != 0;
        if (val is byte by) return by != 0;
        return val != null && ((object)val).ToString() != "0";
    }
}

public class GetRoomHandler : IRequestHandler<GetRoomQuery, Result<RoomAdminResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetRoomHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<RoomAdminResponse>> Handle(GetRoomQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, tenant_id, code AS room_code, name, capacity AS max_per_day, is_active, created_at, updated_at FROM diab_his_sys_rooms WHERE id = @id AND (tenant_id = @tenantId OR tenant_id IS NULL) AND deleted_at IS NULL",
            new { id = q.Id, tenantId });

        if (r == null)
            return Result<RoomAdminResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        return Result<RoomAdminResponse>.Success(new RoomAdminResponse(
            r.id.ToString(),
            (int?)r.tenant_id,
            (string)r.room_code,
            (string)r.name,
            (int)r.max_per_day,
            RoomBoolHelper.ToBool(r.is_active),
            (DateTime)r.created_at,
            (DateTime)r.updated_at));
    }
}

public class CreateRoomHandler : IRequestHandler<CreateRoomCommand, Result<RoomAdminResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateRoomHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<RoomAdminResponse>> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var req = cmd.Request;

        if (string.IsNullOrWhiteSpace(req.RoomCode))
            return Result<RoomAdminResponse>.Failure("VALIDATION_ERROR", "Mã phòng không được để trống");
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<RoomAdminResponse>.Failure("VALIDATION_ERROR", "Tên phòng không được để trống");

        // Kiem tra trung room_code trong tenant
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_rooms WHERE tenant_id = @tenantId AND code = @code AND deleted_at IS NULL",
            new { tenantId, code = req.RoomCode });
        if (exists > 0)
            return Result<RoomAdminResponse>.Failure("ROOM_CODE_DUPLICATE", "Mã phòng đã tồn tại");

        var newId = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sys_rooms (id, tenant_id, code, name, room_type, capacity, is_active, created_at, updated_at)
              VALUES (@id, @tenantId, @code, @name, 'EXAM', @maxPerDay, @isActive, NOW(), NOW())",
            new { id = newId, tenantId, code = req.RoomCode, name = req.Name, maxPerDay = req.MaxPerDay, isActive = req.IsActive ? 1 : 0 });

        return Result<RoomAdminResponse>.Success(new RoomAdminResponse(
            newId, tenantId, req.RoomCode, req.Name, req.MaxPerDay, req.IsActive,
            DateTime.UtcNow, DateTime.UtcNow));
    }
}

public class UpdateRoomHandler : IRequestHandler<UpdateRoomCommand, Result<RoomAdminResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdateRoomHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<RoomAdminResponse>> Handle(UpdateRoomCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, tenant_id, code AS room_code, name, capacity AS max_per_day, is_active, created_at FROM diab_his_sys_rooms WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });

        if (existing == null)
            return Result<RoomAdminResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        var newCode = cmd.Request.RoomCode ?? (string)existing.room_code;
        var newName = cmd.Request.Name ?? (string)existing.name;
        var newMax = cmd.Request.MaxPerDay ?? (int)existing.max_per_day;
        var newActive = cmd.Request.IsActive.HasValue ? (cmd.Request.IsActive.Value ? 1 : 0) : (RoomBoolHelper.ToBool(existing.is_active) ? 1 : 0);

        // Kiem tra trung code neu doi
        if (cmd.Request.RoomCode != null && cmd.Request.RoomCode != (string)existing.room_code)
        {
            var dup = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sys_rooms WHERE tenant_id = @tenantId AND code = @code AND id != @id AND deleted_at IS NULL",
                new { tenantId, code = newCode, id = cmd.Id });
            if (dup > 0)
                return Result<RoomAdminResponse>.Failure("ROOM_CODE_DUPLICATE", "Mã phòng đã tồn tại");
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_sys_rooms SET code = @code, name = @name, capacity = @maxPerDay, is_active = @isActive, updated_at = NOW() WHERE id = @id",
            new { code = newCode, name = newName, maxPerDay = newMax, isActive = newActive, id = cmd.Id });

        return Result<RoomAdminResponse>.Success(new RoomAdminResponse(
            cmd.Id, tenantId, newCode, newName, newMax, newActive == 1,
            (DateTime)existing.created_at, DateTime.UtcNow));
    }
}

public class DeleteRoomHandler : IRequestHandler<DeleteRoomCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeleteRoomHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteRoomCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var affected = await conn.ExecuteAsync(
            "UPDATE diab_his_sys_rooms SET deleted_at = NOW(), is_active = 0 WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });

        if (affected == 0)
            return Result<bool>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        return Result<bool>.Success(true);
    }
}
