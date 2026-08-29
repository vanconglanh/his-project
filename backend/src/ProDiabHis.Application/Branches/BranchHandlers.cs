using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Branches;

// ─── Commands & Queries ───────────────────────────────────────────────────────

public record ListBranchesQuery(bool? IsActive, string? Q, int Page, int PageSize)
    : IRequest<PagedResult<BranchDto>>;

public record GetBranchQuery(int Id) : IRequest<Result<BranchDto>>;

public record CreateBranchCommand(CreateBranchRequest Request) : IRequest<Result<BranchDto>>;

public record UpdateBranchCommand(int Id, UpdateBranchRequest Request) : IRequest<Result<BranchDto>>;

public record SetBranchStatusCommand(int Id, bool IsActive) : IRequest<Result<BranchDto>>;

public record SetDefaultBranchCommand(int Id) : IRequest<Result<BranchDto>>;

public record DeleteBranchCommand(int Id) : IRequest<Result<bool>>;

public record ListBranchUsersQuery(int BranchId) : IRequest<Result<List<UserBranchDto>>>;

public record AssignUsersToBranchCommand(int BranchId, AssignUsersToBranchRequest Request) : IRequest<Result<bool>>;

public record RemoveUserFromBranchCommand(int BranchId, Guid UserId) : IRequest<Result<bool>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────

file static class BranchSql
{
    public const string Select = @"
        SELECT b.id, b.tenant_id, b.code, b.name, b.cskcb_code, b.address, b.phone, b.email,
               b.working_hours, b.timezone, b.is_active, b.is_default, b.sort_order,
               b.created_at, b.updated_at,
               (SELECT COUNT(*) FROM diab_his_sec_user_branches ub
                 WHERE ub.branch_id = b.id AND ub.deleted_at IS NULL) AS user_count
          FROM diab_his_sys_branches b";
}

internal static class BranchMapper
{
    public static BranchDto Map(dynamic r) => new(
        (int)r.id,
        (int)r.tenant_id,
        (string)r.code,
        (string)r.name,
        (string?)r.cskcb_code,
        (string?)r.address,
        (string?)r.phone,
        (string?)r.email,
        (string?)r.working_hours,
        (string)r.timezone,
        ToBool(r.is_active),
        ToBool(r.is_default),
        (int)r.sort_order,
        (int)r.user_count,
        (DateTime)r.created_at,
        (DateTime)r.updated_at);

    public static bool ToBool(dynamic val)
    {
        if (val is bool b) return b;
        if (val is sbyte sb) return sb != 0;
        if (val is byte by) return by != 0;
        if (val is int i) return i != 0;
        return val != null && ((object)val).ToString() != "0";
    }
}

public class ListBranchesHandler : IRequestHandler<ListBranchesQuery, PagedResult<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public ListBranchesHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _branchProvider = branchProvider;
    }

    public async Task<PagedResult<BranchDto>> Handle(ListBranchesQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = "WHERE b.tenant_id = @tenantId AND b.deleted_at IS NULL";
        if (q.IsActive.HasValue)
            where += " AND b.is_active = @isActive";
        if (!string.IsNullOrWhiteSpace(q.Q))
            where += " AND (b.name LIKE @kw OR b.code LIKE @kw)";

        // User khong co branch.cross_view chi thay branch trong branch_ids duoc gan (7.1)
        if (!_branchProvider.IgnoreBranchFilter && _branchProvider.AllowedBranchIds.Count > 0)
            where += " AND b.id IN @allowedIds";
        else if (!_branchProvider.IgnoreBranchFilter)
            where += " AND 1 = 0";

        var parameters = new
        {
            tenantId,
            isActive = q.IsActive.HasValue ? (q.IsActive.Value ? 1 : 0) : (int?)null,
            kw = $"%{q.Q}%",
            allowedIds = _branchProvider.AllowedBranchIds
        };

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_sys_branches b {where}", parameters);

        var rows = await conn.QueryAsync<dynamic>(
            $"{BranchSql.Select} {where} ORDER BY b.sort_order, b.code LIMIT @limit OFFSET @offset",
            new
            {
                parameters.tenantId, parameters.isActive, parameters.kw, parameters.allowedIds,
                limit = q.PageSize, offset
            });

        var items = rows.Select(BranchMapper.Map).ToList();
        return new PagedResult<BranchDto>(items, q.Page, q.PageSize, total);
    }
}

public class GetBranchHandler : IRequestHandler<GetBranchQuery, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(GetBranchQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $"{BranchSql.Select} WHERE b.id = @id AND b.tenant_id = @tenantId AND b.deleted_at IS NULL",
            new { id = q.Id, tenantId });

        if (r == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;
        var req = cmd.Request;

        if (string.IsNullOrWhiteSpace(req.Code) || req.Code.Length is < 2 or > 20)
            return Result<BranchDto>.Failure("VALIDATION_ERROR", "Mã chi nhánh phải từ 2-20 ký tự");
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<BranchDto>.Failure("VALIDATION_ERROR", "Tên chi nhánh không được để trống");

        var dupCode = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE tenant_id = @tenantId AND code = @code AND deleted_at IS NULL",
            new { tenantId, code = req.Code });
        if (dupCode > 0)
            return Result<BranchDto>.Failure("BRANCH_CODE_DUPLICATED", "Mã chi nhánh đã tồn tại trong tổ chức");

        if (!string.IsNullOrWhiteSpace(req.CskcbCode))
        {
            var dupCskcb = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sys_branches WHERE cskcb_code = @cskcb AND deleted_at IS NULL",
                new { cskcb = req.CskcbCode });
            if (dupCskcb > 0)
                return Result<BranchDto>.Failure("BRANCH_CSKCB_DUPLICATED", "Mã CSKCB đã được sử dụng bởi chi nhánh khác");
        }

        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_sys_branches
                (tenant_id, code, name, cskcb_code, address, phone, email, working_hours, timezone,
                 is_active, is_default, sort_order, created_at, updated_at)
              VALUES
                (@tenantId, @code, @name, @cskcbCode, @address, @phone, @email, @workingHours, @timezone,
                 @isActive, 0, @sortOrder, NOW(), NOW());
              SELECT LAST_INSERT_ID();",
            new
            {
                tenantId,
                code = req.Code,
                name = req.Name,
                cskcbCode = req.CskcbCode,
                address = req.Address,
                phone = req.Phone,
                email = req.Email,
                workingHours = req.WorkingHours,
                timezone = string.IsNullOrWhiteSpace(req.Timezone) ? "Asia/Ho_Chi_Minh" : req.Timezone,
                isActive = req.IsActive ? 1 : 0,
                sortOrder = req.SortOrder
            });

        var r = await conn.QueryFirstAsync<dynamic>(
            $"{BranchSql.Select} WHERE b.id = @id", new { id });
        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}

public class UpdateBranchHandler : IRequestHandler<UpdateBranchCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdateBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(UpdateBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, code FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        var req = cmd.Request;
        if (!string.IsNullOrWhiteSpace(req.Code) && req.Code != (string)existing.code)
        {
            var dup = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sys_branches WHERE tenant_id = @tenantId AND code = @code AND id != @id AND deleted_at IS NULL",
                new { tenantId, code = req.Code, id = cmd.Id });
            if (dup > 0)
                return Result<BranchDto>.Failure("BRANCH_CODE_DUPLICATED", "Mã chi nhánh đã tồn tại trong tổ chức");
        }

        if (!string.IsNullOrWhiteSpace(req.CskcbCode))
        {
            var dupCskcb = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sys_branches WHERE cskcb_code = @cskcb AND id != @id AND deleted_at IS NULL",
                new { cskcb = req.CskcbCode, id = cmd.Id });
            if (dupCskcb > 0)
                return Result<BranchDto>.Failure("BRANCH_CSKCB_DUPLICATED", "Mã CSKCB đã được sử dụng bởi chi nhánh khác");
        }

        await conn.ExecuteAsync(
            @"UPDATE diab_his_sys_branches SET
                code = COALESCE(@code, code),
                name = COALESCE(@name, name),
                cskcb_code = COALESCE(@cskcbCode, cskcb_code),
                address = COALESCE(@address, address),
                phone = COALESCE(@phone, phone),
                email = COALESCE(@email, email),
                working_hours = COALESCE(@workingHours, working_hours),
                timezone = COALESCE(@timezone, timezone),
                sort_order = COALESCE(@sortOrder, sort_order),
                updated_at = NOW()
              WHERE id = @id AND tenant_id = @tenantId",
            new
            {
                id = cmd.Id, tenantId,
                code = req.Code, name = req.Name, cskcbCode = req.CskcbCode, address = req.Address,
                phone = req.Phone, email = req.Email, workingHours = req.WorkingHours,
                timezone = req.Timezone, sortOrder = req.SortOrder
            });

        var r = await conn.QueryFirstAsync<dynamic>($"{BranchSql.Select} WHERE b.id = @id", new { id = cmd.Id });
        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}

public class SetBranchStatusHandler : IRequestHandler<SetBranchStatusCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public SetBranchStatusHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(SetBranchStatusCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, is_default FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        // INV-2: khong duoc vo hieu hoa branch mac dinh
        if (!cmd.IsActive && BranchMapper.ToBool(existing.is_default))
            return Result<BranchDto>.Failure("BRANCH_IS_DEFAULT", "Không thể xoá/vô hiệu hoá chi nhánh mặc định");

        await conn.ExecuteAsync(
            "UPDATE diab_his_sys_branches SET is_active = @isActive, updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id, tenantId, isActive = cmd.IsActive ? 1 : 0 });

        var r = await conn.QueryFirstAsync<dynamic>($"{BranchSql.Select} WHERE b.id = @id", new { id = cmd.Id });
        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}

public class SetDefaultBranchHandler : IRequestHandler<SetDefaultBranchCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public SetDefaultBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(SetDefaultBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, is_active FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");
        if (!BranchMapper.ToBool(existing.is_active))
            return Result<BranchDto>.Failure("BRANCH_INACTIVE", "Chi nhánh đã ngừng hoạt động");

        using var tx = conn.BeginTransaction();
        try
        {
            // INV-1: dung 1 branch is_default=1 per tenant -> tu go default cu trong cung 1 transaction
            await conn.ExecuteAsync(
                "UPDATE diab_his_sys_branches SET is_default = 0, updated_at = NOW() WHERE tenant_id = @tenantId AND is_default = 1",
                new { tenantId }, tx);
            await conn.ExecuteAsync(
                "UPDATE diab_his_sys_branches SET is_default = 1, updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
                new { id = cmd.Id, tenantId }, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        var r = await conn.QueryFirstAsync<dynamic>($"{BranchSql.Select} WHERE b.id = @id", new { id = cmd.Id });
        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}

public class DeleteBranchHandler : IRequestHandler<DeleteBranchCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeleteBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, is_default FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing == null)
            return Result<bool>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        if (BranchMapper.ToBool(existing.is_default))
            return Result<bool>.Failure("BRANCH_IS_DEFAULT", "Không thể xoá/vô hiệu hoá chi nhánh mặc định");

        // INV-3: chan xoa neu con du lieu van hanh
        var hasData = await conn.ExecuteScalarAsync<int>(
            @"SELECT
                (SELECT COUNT(*) FROM diab_his_enc_encounters WHERE branch_id = @id) +
                (SELECT COUNT(*) FROM diab_his_bil_billing WHERE branch_id = @id) +
                (SELECT COUNT(*) FROM diab_his_pha_stock WHERE branch_id = @id)",
            new { id = cmd.Id });
        if (hasData > 0)
            return Result<bool>.Failure("BRANCH_HAS_DATA", "Không thể xoá chi nhánh vì đang có dữ liệu nghiệp vụ");

        await conn.ExecuteAsync(
            "UPDATE diab_his_sys_branches SET deleted_at = NOW(), is_active = 0 WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id, tenantId });

        return Result<bool>.Success(true);
    }
}

public class ListBranchUsersHandler : IRequestHandler<ListBranchUsersQuery, Result<List<UserBranchDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListBranchUsersHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<UserBranchDto>>> Handle(ListBranchUsersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var branchExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.BranchId, tenantId });
        if (branchExists == 0)
            return Result<List<UserBranchDto>>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        // BUG FIX: Dapper KHONG tu convert string -> Guid non-nullable (Convert.ChangeType khong
        // ho tro Guid IConvertible) -> QueryAsync<UserBranchDto> voi UserId kieu Guid nem
        // InvalidCastException khi cot tra ve la string (GuidFormat=None, xem
        // Infrastructure/DependencyInjection.cs). Doc UserId dang string roi Guid.Parse thu cong.
        var rawRows = await conn.QueryAsync<(string UserId, string FullName, string Email, bool IsPrimary)>(
            @"SELECT u.id AS UserId, u.full_name AS FullName, u.email AS Email, ub.is_primary AS IsPrimary
                FROM diab_his_sec_user_branches ub
                JOIN diab_his_sec_users u ON u.id = ub.user_id
               WHERE ub.branch_id = @branchId AND ub.tenant_id = @tenantId AND ub.deleted_at IS NULL
                 AND u.deleted_at IS NULL
               ORDER BY u.full_name",
            new { branchId = q.BranchId, tenantId });

        var rows = rawRows.Select(r => new UserBranchDto
        {
            UserId = Guid.TryParse(r.UserId, out var uid) ? uid : Guid.Empty,
            FullName = r.FullName,
            Email = r.Email,
            IsPrimary = r.IsPrimary
        }).ToList();

        return Result<List<UserBranchDto>>.Success(rows);
    }
}

public class AssignUsersToBranchHandler : IRequestHandler<AssignUsersToBranchCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public AssignUsersToBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(AssignUsersToBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var branchExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.BranchId, tenantId });
        if (branchExists == 0)
            return Result<bool>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        foreach (var userId in cmd.Request.UserIds)
        {
            var exists = await conn.ExecuteScalarAsync<string?>(
                "SELECT id FROM diab_his_sec_user_branches WHERE user_id = @userId AND branch_id = @branchId",
                new { userId = userId.ToString(), branchId = cmd.BranchId });

            if (exists == null)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_sec_user_branches (id, tenant_id, user_id, branch_id, is_primary, created_at, updated_at)
                      VALUES (UUID(), @tenantId, @userId, @branchId, @isPrimary, NOW(), NOW())",
                    new { tenantId, userId = userId.ToString(), branchId = cmd.BranchId, isPrimary = cmd.Request.IsPrimary == true ? 1 : 0 });
            }
            else
            {
                await conn.ExecuteAsync(
                    "UPDATE diab_his_sec_user_branches SET deleted_at = NULL, updated_at = NOW() WHERE id = @id",
                    new { id = exists });
            }

            if (cmd.Request.IsPrimary == true)
            {
                await conn.ExecuteAsync(
                    "UPDATE diab_his_sec_user_branches SET is_primary = 0 WHERE user_id = @userId AND branch_id != @branchId",
                    new { userId = userId.ToString(), branchId = cmd.BranchId });
                await conn.ExecuteAsync(
                    "UPDATE diab_his_sec_users SET branch_id = @branchId, updated_at = NOW() WHERE id = @userId AND tenant_id = @tenantId",
                    new { branchId = cmd.BranchId, userId = userId.ToString(), tenantId });
            }
        }

        return Result<bool>.Success(true);
    }
}

public class RemoveUserFromBranchHandler : IRequestHandler<RemoveUserFromBranchCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public RemoveUserFromBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(RemoveUserFromBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_sec_user_branches SET deleted_at = NOW()
               WHERE branch_id = @branchId AND user_id = @userId AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { branchId = cmd.BranchId, userId = cmd.UserId.ToString(), tenantId });

        if (affected == 0)
            return Result<bool>.Failure("USER_NOT_IN_BRANCH", "Người dùng chưa được phân công vào chi nhánh này");

        return Result<bool>.Success(true);
    }
}
