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

// ─── NV1/NV2: BHYT compliance, clone, readiness (Dot 5) ────────────────────────

public record GetBranchBhytComplianceQuery : IRequest<Result<List<BranchBhytComplianceDto>>>;

public record CloneBranchCommand(CloneBranchRequest Request) : IRequest<Result<BranchDto>>;

public record GetBranchReadinessQuery(int Id) : IRequest<Result<BranchReadinessDto>>;

public record ActivateBranchCommand(int Id) : IRequest<Result<BranchDto>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────

file static class BranchSql
{
    public const string Select = @"
        SELECT b.id, b.tenant_id, b.code, b.name, b.cskcb_code, b.address, b.phone, b.email,
               b.working_hours, b.timezone, b.is_active, b.is_default, b.sort_order,
               b.status, b.hospital_rank, b.kcb_tuyen, b.bhyt_contract_code,
               b.bhyt_contract_valid_from, b.bhyt_contract_valid_to, b.bhyt_enabled, b.dtqg_enabled,
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
        (string?)r.status ?? Domain.Entities.BranchStatus.Active,
        (string?)r.hospital_rank,
        (string?)r.kcb_tuyen,
        (string?)r.bhyt_contract_code,
        (DateTime?)r.bhyt_contract_valid_from,
        (DateTime?)r.bhyt_contract_valid_to,
        ToBool(r.bhyt_enabled),
        ToBool(r.dtqg_enabled),
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
    private readonly IPermissionChecker _permissionChecker;

    public ListBranchesHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IPermissionChecker permissionChecker)
    {
        _db = db;
        _currentUser = currentUser;
        _branchProvider = branchProvider;
        _permissionChecker = permissionChecker;
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

        // BR-110: chi user co branch.create/branch.update moi thay chi nhanh DRAFT (dang cau hinh,
        // chua go-live) — user thuong chi thay ACTIVE/SUSPENDED (khong thay CLOSED de tranh nham lan).
        if (!_permissionChecker.HasPermission("branch.create") && !_permissionChecker.HasPermission("branch.update"))
            where += " AND b.status IN ('ACTIVE', 'SUSPENDED')";

        // Man quan ly chi nhanh la READ cross-branch: pham vi theo ENTITLEMENT (quyen) cua user, KHONG
        // phu thuoc chi nhanh dang chon qua X-Branch-Id. Neu dung IgnoreBranchFilter (phu thuoc branch
        // dang chon) thi admin cross_view khi da chon 1 chi nhanh se chi thay dung chi nhanh do -> sai.
        // => cross_view: thay tat ca chi nhanh tenant; nguoc lai: chi branch duoc gan/nhom (BR-33/7.1).
        if (!_permissionChecker.HasPermission("branch.cross_view"))
        {
            if (_branchProvider.AllowedBranchIds.Count > 0)
                where += " AND b.id IN @allowedIds";
            else
                where += " AND 1 = 0";
        }

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

        // BR-110: chi nhanh tao moi luon o trang thai DRAFT, phai qua checklist go-live (BR-112)
        // moi duoc kich hoat ACTIVE (xem ActivateBranchHandler).
        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_sys_branches
                (tenant_id, code, name, cskcb_code, address, phone, email, working_hours, timezone,
                 is_active, is_default, sort_order, status,
                 hospital_rank, kcb_tuyen, bhyt_contract_code, bhyt_contract_valid_from, bhyt_contract_valid_to,
                 bhyt_enabled, dtqg_enabled, created_at, updated_at)
              VALUES
                (@tenantId, @code, @name, @cskcbCode, @address, @phone, @email, @workingHours, @timezone,
                 @isActive, 0, @sortOrder, 'DRAFT',
                 @hospitalRank, @kcbTuyen, @bhytContractCode, @bhytContractValidFrom, @bhytContractValidTo,
                 @bhytEnabled, @dtqgEnabled, NOW(), NOW());
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
                sortOrder = req.SortOrder,
                hospitalRank = req.HospitalRank,
                kcbTuyen = req.KcbTuyen,
                bhytContractCode = req.BhytContractCode,
                bhytContractValidFrom = req.BhytContractValidFrom,
                bhytContractValidTo = req.BhytContractValidTo,
                bhytEnabled = req.BhytEnabled ? 1 : 0,
                dtqgEnabled = req.DtqgEnabled ? 1 : 0
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
                hospital_rank = COALESCE(@hospitalRank, hospital_rank),
                kcb_tuyen = COALESCE(@kcbTuyen, kcb_tuyen),
                bhyt_contract_code = COALESCE(@bhytContractCode, bhyt_contract_code),
                bhyt_contract_valid_from = COALESCE(@bhytContractValidFrom, bhyt_contract_valid_from),
                bhyt_contract_valid_to = COALESCE(@bhytContractValidTo, bhyt_contract_valid_to),
                bhyt_enabled = COALESCE(@bhytEnabled, bhyt_enabled),
                dtqg_enabled = COALESCE(@dtqgEnabled, dtqg_enabled),
                updated_at = NOW()
              WHERE id = @id AND tenant_id = @tenantId",
            new
            {
                id = cmd.Id, tenantId,
                code = req.Code, name = req.Name, cskcbCode = req.CskcbCode, address = req.Address,
                phone = req.Phone, email = req.Email, workingHours = req.WorkingHours,
                timezone = req.Timezone, sortOrder = req.SortOrder,
                hospitalRank = req.HospitalRank, kcbTuyen = req.KcbTuyen, bhytContractCode = req.BhytContractCode,
                bhytContractValidFrom = req.BhytContractValidFrom, bhytContractValidTo = req.BhytContractValidTo,
                bhytEnabled = req.BhytEnabled.HasValue ? (req.BhytEnabled.Value ? 1 : 0) : (int?)null,
                dtqgEnabled = req.DtqgEnabled.HasValue ? (req.DtqgEnabled.Value ? 1 : 0) : (int?)null
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

// ─── NV1: BHYT compliance theo chi nhanh (BR-100..108, US-7.1) ─────────────────

public class GetBranchBhytComplianceHandler : IRequestHandler<GetBranchBhytComplianceQuery, Result<List<BranchBhytComplianceDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IPermissionChecker _permissionChecker;

    public GetBranchBhytComplianceHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IPermissionChecker permissionChecker)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider; _permissionChecker = permissionChecker;
    }

    public async Task<Result<List<BranchBhytComplianceDto>>> Handle(GetBranchBhytComplianceQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        // READ cross-branch: pham vi theo entitlement (cross_view -> tat ca), khong theo branch dang chon.
        var where = "WHERE b.tenant_id = @tenantId AND b.deleted_at IS NULL";
        if (!_permissionChecker.HasPermission("branch.cross_view"))
        {
            if (_branchProvider.AllowedBranchIds.Count > 0)
                where += " AND b.id IN @allowedIds";
            else
                where += " AND 1 = 0";
        }

        var branches = await conn.QueryAsync<dynamic>(
            $@"SELECT b.id, b.name, b.cskcb_code, b.bhyt_enabled, b.bhyt_contract_valid_to
                 FROM diab_his_sys_branches b {where} ORDER BY b.sort_order, b.code",
            new { tenantId, allowedIds = _branchProvider.AllowedBranchIds });

        var today = DateTime.UtcNow.Date;
        var result = new List<BranchBhytComplianceDto>();
        foreach (var b in branches)
        {
            int branchId = (int)b.id;

            var cred = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT token_expires_at FROM diab_his_int_dtqg_credentials
                   WHERE tenant_id = @tenantId AND branch_id = @branchId AND is_active = 1 AND deleted_at IS NULL
                   LIMIT 1",
                new { tenantId, branchId });

            string? lastPeriod = await conn.ExecuteScalarAsync<string?>(
                @"SELECT period_month FROM diab_his_int_bhyt_exports
                   WHERE tenant_id = @tenantId AND branch_id = @branchId AND deleted_at IS NULL
                   ORDER BY period_month DESC LIMIT 1",
                new { tenantId, branchId });

            DateTime? contractTo = (DateTime?)b.bhyt_contract_valid_to;
            DateTime? tokenExpiresAt = cred != null ? (DateTime?)cred.token_expires_at : null;

            result.Add(new BranchBhytComplianceDto(
                BranchId: branchId,
                Name: (string)b.name,
                HasCskcb: !string.IsNullOrWhiteSpace((string?)b.cskcb_code),
                BhytEnabled: BranchMapper.ToBool(b.bhyt_enabled),
                BhytContractValid: contractTo.HasValue && contractTo.Value.Date >= today,
                DtqgConnected: cred != null,
                DtqgTokenValid: tokenExpiresAt.HasValue && tokenExpiresAt.Value > DateTime.UtcNow,
                LastBhytExportPeriod: lastPeriod));
        }

        return Result<List<BranchBhytComplianceDto>>.Success(result);
    }
}

// ─── NV2: Clone chi nhanh + checklist go-live (BR-110/111/112, US-8.1) ─────────

public class CloneBranchHandler : IRequestHandler<CloneBranchCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CloneBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<BranchDto>> Handle(CloneBranchCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        using var conn = _db.CreateConnection();
        conn.Open();
        var tenantId = _currentUser.TenantId!.Value;

        var source = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = req.SourceBranchId, tenantId });
        if (source == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh nguồn");

        if (string.IsNullOrWhiteSpace(req.Code) || req.Code.Length is < 2 or > 20)
            return Result<BranchDto>.Failure("VALIDATION_ERROR", "Mã chi nhánh phải từ 2-20 ký tự");
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<BranchDto>.Failure("VALIDATION_ERROR", "Tên chi nhánh không được để trống");

        var dupCode = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE tenant_id = @tenantId AND code = @code AND deleted_at IS NULL",
            new { tenantId, code = req.Code });
        if (dupCode > 0)
            return Result<BranchDto>.Failure("BRANCH_CODE_DUPLICATED", "Mã chi nhánh đã tồn tại trong tổ chức");

        using var tx = conn.BeginTransaction();
        try
        {
            // BR-111/AC-8.1.1: chi nhanh moi luon DRAFT, KHONG copy cskcb_code/credential/nhan su/ton kho.
            var newId = await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO diab_his_sys_branches
                    (tenant_id, code, name, cskcb_code, address, phone, email, working_hours, timezone,
                     is_active, is_default, sort_order, status, group_id, created_at, updated_at)
                  VALUES
                    (@tenantId, @code, @name, NULL, @address, @phone, @email, NULL, @timezone,
                     1, 0, 0, 'DRAFT', @groupId, NOW(), NOW());
                  SELECT LAST_INSERT_ID();",
                new
                {
                    tenantId,
                    code = req.Code,
                    name = req.Name,
                    address = req.Address,
                    phone = req.Phone,
                    email = req.Email,
                    timezone = string.IsNullOrWhiteSpace(req.Timezone) ? "Asia/Ho_Chi_Minh" : req.Timezone,
                    groupId = req.GroupId
                }, tx);

            // Copy cau truc phong (diab_his_sys_rooms). Code room unique theo (tenant, code) — KHONG
            // theo branch — nen phai sinh code moi cho chi nhanh clone (suffix -B{newId}) de tranh trung.
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_sys_rooms (id, tenant_id, branch_id, code, name, room_type, floor, capacity, is_active, created_at, updated_at)
                  SELECT UUID(), tenant_id, @newId, CONCAT(code, '-B', @newId), name, room_type, floor, capacity, is_active, NOW(), NOW()
                    FROM diab_his_sys_rooms WHERE branch_id = @sourceId AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { newId, sourceId = req.SourceBranchId, tenantId }, tx);

            // Copy cau hinh kho (pha_warehouses). Code kho cung unique theo (tenant, code) -> suffix -B{newId}.
            await conn.ExecuteAsync(
                @"INSERT INTO pha_warehouses (tenant_id, code, name, type, address, branch_id, created_at, updated_at)
                  SELECT tenant_id, CONCAT(code, '-B', @newId), name, type, address, @newId, NOW(), NOW()
                    FROM pha_warehouses WHERE branch_id = @sourceId AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { newId, sourceId = req.SourceBranchId, tenantId }, tx);

            // Copy bo dem so phieu (diab_his_bil_counters)
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_counters (id, tenant_id, code, name, sort_order, status, branch_id, created_at, updated_at)
                  SELECT UUID(), tenant_id, code, name, sort_order, status, @newId, NOW(), NOW()
                    FROM diab_his_bil_counters WHERE branch_id = @sourceId AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { newId, sourceId = req.SourceBranchId, tenantId }, tx);

            // Copy gia dich vu override scope=BRANCH (doi scope sang branch moi)
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_bil_service_branch_prices
                    (id, tenant_id, service_id, scope, branch_id, group_id, price, effective_from, effective_to, note, created_at, updated_at)
                  SELECT UUID(), tenant_id, service_id, scope, @newId, NULL, price, effective_from, effective_to, note, NOW(), NOW()
                    FROM diab_his_bil_service_branch_prices
                   WHERE branch_id = @sourceId AND tenant_id = @tenantId AND scope = 'BRANCH' AND deleted_at IS NULL",
                new { newId, sourceId = req.SourceBranchId, tenantId }, tx);

            // TODO(BR-111): lich truc cu the (diab_his_sch_doctor_schedules) KHONG duoc copy vi
            // gan voi doctor_ref cu the (nhan su khong copy theo AC-8.1.1) — chi nhanh moi tu cau hinh lai.
            // KHONG copy: benh nhan, pha_stock (ton kho), cskcb_code, credential DTQG/BHYT, sec_user_branches.

            tx.Commit();

            var r = await conn.QueryFirstAsync<dynamic>($"{BranchSql.Select} WHERE b.id = @id", new { id = newId });
            return Result<BranchDto>.Success(BranchMapper.Map(r));
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public class GetBranchReadinessHandler : IRequestHandler<GetBranchReadinessQuery, Result<BranchReadinessDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetBranchReadinessHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<BranchReadinessDto>> Handle(GetBranchReadinessQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var branch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, bhyt_enabled, dtqg_enabled, cskcb_code, bhyt_contract_valid_to FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.Id, tenantId });
        if (branch == null)
            return Result<BranchReadinessDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        var input = await BuildInputAsync(conn, tenantId, q.Id, branch);
        var dto = BranchReadinessCalculator.Build(q.Id, input);
        return Result<BranchReadinessDto>.Success(dto);
    }

    internal static async Task<BranchReadinessInput> BuildInputAsync(System.Data.IDbConnection conn, int tenantId, int branchId, dynamic branch)
    {
        var examRoomCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_rooms WHERE branch_id=@b AND tenant_id=@t AND room_type='EXAM' AND is_active=1 AND deleted_at IS NULL",
            new { b = branchId, t = tenantId });

        var warehouseCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pha_warehouses WHERE branch_id=@b AND tenant_id=@t AND deleted_at IS NULL",
            new { b = branchId, t = tenantId });

        var doctorCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT ub.user_id) FROM diab_his_sec_user_branches ub
               JOIN diab_his_sec_user_roles ur ON ur.user_id = ub.user_id COLLATE utf8mb4_0900_ai_ci
               JOIN diab_his_sec_roles r ON r.id = ur.role_id
              WHERE ub.branch_id=@b AND ub.tenant_id=@t AND ub.deleted_at IS NULL AND r.code='bac_si'",
            new { b = branchId, t = tenantId });

        var receptionistCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT ub.user_id) FROM diab_his_sec_user_branches ub
               JOIN diab_his_sec_user_roles ur ON ur.user_id = ub.user_id COLLATE utf8mb4_0900_ai_ci
               JOIN diab_his_sec_roles r ON r.id = ur.role_id
              WHERE ub.branch_id=@b AND ub.tenant_id=@t AND ub.deleted_at IS NULL AND r.code='le_tan'",
            new { b = branchId, t = tenantId });

        var upcomingShiftCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_sch_doctor_schedules
               WHERE branch_id=@b AND tenant_id=@t AND enabled=1 AND deleted_at IS NULL
                 AND (effective_to IS NULL OR effective_to >= CURDATE())
                 AND (effective_from IS NULL OR effective_from <= DATE_ADD(CURDATE(), INTERVAL 7 DAY))",
            new { b = branchId, t = tenantId });

        var counterCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_bil_counters WHERE branch_id=@b AND tenant_id=@t AND status=1 AND deleted_at IS NULL",
            new { b = branchId, t = tenantId });

        bool bhytEnabled = BranchMapper.ToBool(branch.bhyt_enabled);
        bool dtqgEnabled = BranchMapper.ToBool(branch.dtqg_enabled);
        bool hasCskcb = !string.IsNullOrWhiteSpace((string?)branch.cskcb_code);
        DateTime? contractTo = (DateTime?)branch.bhyt_contract_valid_to;
        bool contractValid = contractTo.HasValue && contractTo.Value.Date >= DateTime.UtcNow.Date;

        var cred = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT token_expires_at FROM diab_his_int_dtqg_credentials WHERE tenant_id=@t AND branch_id=@b AND is_active=1 AND deleted_at IS NULL LIMIT 1",
            new { b = branchId, t = tenantId });
        bool dtqgConnected = cred != null;
        DateTime? tokenExpiresAt = cred != null ? (DateTime?)cred.token_expires_at : null;
        bool dtqgTokenValid = tokenExpiresAt.HasValue && tokenExpiresAt.Value > DateTime.UtcNow;

        return new BranchReadinessInput(
            examRoomCount, warehouseCount, doctorCount, receptionistCount, upcomingShiftCount, counterCount,
            bhytEnabled, hasCskcb, contractValid, dtqgEnabled, dtqgConnected, dtqgTokenValid);
    }
}

public class ActivateBranchHandler : IRequestHandler<ActivateBranchCommand, Result<BranchDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public ActivateBranchHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<BranchDto>> Handle(ActivateBranchCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var branch = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, bhyt_enabled, dtqg_enabled, cskcb_code, bhyt_contract_valid_to, status FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (branch == null)
            return Result<BranchDto>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");

        BranchReadinessInput input = await GetBranchReadinessHandler.BuildInputAsync(conn, tenantId, cmd.Id, branch);
        BranchReadinessDto readiness = BranchReadinessCalculator.Build(cmd.Id, input);

        if (!readiness.AllPassed)
        {
            var failed = readiness.Items.Where(i => !i.Passed).ToList();
            return Result<BranchDto>.Failure(
                "BRANCH_NOT_READY",
                "Chi nhánh chưa đạt checklist go-live, không thể kích hoạt",
                new { failedItems = failed });
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_sys_branches SET status = 'ACTIVE', is_active = 1, updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id, tenantId });

        await _audit.LogAsync(
            "branch.activate",
            "Branch",
            cmd.Id.ToString(),
            details: new { branchId = cmd.Id, activatedBy = _currentUser.UserId, activatedAt = DateTime.UtcNow },
            cancellationToken: ct);

        var r = await conn.QueryFirstAsync<dynamic>($"{BranchSql.Select} WHERE b.id = @id", new { id = cmd.Id });
        return Result<BranchDto>.Success(BranchMapper.Map(r));
    }
}
