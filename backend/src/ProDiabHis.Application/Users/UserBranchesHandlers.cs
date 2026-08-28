using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

// ---- DTO ----
public record UserBranchResponse(int BranchId, string BranchCode, string BranchName, bool IsPrimary);

public record SetUserBranchesRequest(IReadOnlyList<int> BranchIds, int? PrimaryBranchId);

// ---- Queries / Commands ----
public record GetUserBranchesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<UserBranchResponse>>>;

public record SetUserBranchesCommand(Guid UserId, SetUserBranchesRequest Request) : IRequest<Result<IReadOnlyList<UserBranchResponse>>>;

// ---- Handlers ----

/// <summary>GET /api/v1/users/{id}/branches — xem danh sach chi nhanh da gan cho 1 user</summary>
public class GetUserBranchesQueryHandler : IRequestHandler<GetUserBranchesQuery, Result<IReadOnlyList<UserBranchResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetUserBranchesQueryHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<IReadOnlyList<UserBranchResponse>>> Handle(GetUserBranchesQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;

        var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId && u.TenantId == tenantId && u.DeletedAt == null, ct);
        if (!userExists)
            return Result<IReadOnlyList<UserBranchResponse>>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        var items = await (
            from ub in _db.UserBranches
            join b in _db.Branches on ub.BranchId equals b.Id
            where ub.UserId == request.UserId && ub.TenantId == tenantId && ub.DeletedAt == null && b.DeletedAt == null
            orderby b.SortOrder, b.Name
            select new UserBranchResponse(b.Id, b.Code, b.Name, ub.IsPrimary)
        ).ToListAsync(ct);

        return Result<IReadOnlyList<UserBranchResponse>>.Success(items);
    }
}

/// <summary>PUT /api/v1/users/{id}/branches — gan lai toan bo danh sach chi nhanh cho 1 user (thay the).
/// Chi user co quyen branch.assign_user (enforce o controller) moi goi duoc endpoint nay.</summary>
public class SetUserBranchesCommandHandler : IRequestHandler<SetUserBranchesCommand, Result<IReadOnlyList<UserBranchResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public SetUserBranchesCommandHandler(IApplicationDbContext db, ITenantProvider tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<UserBranchResponse>>> Handle(SetUserBranchesCommand request, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        var req = request.Request;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == tenantId && u.DeletedAt == null, ct);
        if (user is null)
            return Result<IReadOnlyList<UserBranchResponse>>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        var branchIds = (req.BranchIds ?? Array.Empty<int>()).Distinct().ToList();
        if (branchIds.Count == 0)
            return Result<IReadOnlyList<UserBranchResponse>>.Failure("BRANCH_REQUIRED", "Phải gán ít nhất một chi nhánh cho người dùng");

        // Chi cho phep gan cac branch thuoc dung tenant (khong trust id chi nhanh tu client blindly)
        var validBranches = await _db.Branches
            .Where(b => branchIds.Contains(b.Id) && b.TenantId == tenantId && b.DeletedAt == null)
            .Select(b => b.Id)
            .ToListAsync(ct);

        if (validBranches.Count != branchIds.Count)
            return Result<IReadOnlyList<UserBranchResponse>>.Failure("BRANCH_NOT_FOUND", "Một hoặc nhiều chi nhánh không tồn tại");

        var primaryBranchId = req.PrimaryBranchId ?? branchIds[0];
        if (!branchIds.Contains(primaryBranchId))
            return Result<IReadOnlyList<UserBranchResponse>>.Failure(
                "BRANCH_PRIMARY_INVALID", "Chi nhánh mặc định phải nằm trong danh sách chi nhánh được gán");

        // Thay the toan bo danh sach hien tai (soft-delete cac dong khong con trong request)
        var existing = await _db.UserBranches
            .Where(ub => ub.UserId == request.UserId && ub.TenantId == tenantId && ub.DeletedAt == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var ub in existing.Where(e => !branchIds.Contains(e.BranchId)))
            ub.DeletedAt = now;

        foreach (var branchId in branchIds)
        {
            var row = existing.FirstOrDefault(e => e.BranchId == branchId);
            if (row is null)
            {
                _db.UserBranches.Add(new UserBranch
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = request.UserId,
                    BranchId = branchId,
                    IsPrimary = branchId == primaryBranchId
                });
            }
            else
            {
                row.IsPrimary = branchId == primaryBranchId;
                row.DeletedAt = null;
            }
        }

        // Dong bo chi nhanh mac dinh tren sec_users (denormalize, dung khi dang nhap)
        user.BranchId = primaryBranchId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", request.UserId.ToString(),
            new { action = "set_branches", branchIds, primaryBranchId }, ct);

        var items = await (
            from ub in _db.UserBranches
            join b in _db.Branches on ub.BranchId equals b.Id
            where ub.UserId == request.UserId && ub.TenantId == tenantId && ub.DeletedAt == null && b.DeletedAt == null
            orderby b.SortOrder, b.Name
            select new UserBranchResponse(b.Id, b.Code, b.Name, ub.IsPrimary)
        ).ToListAsync(ct);

        return Result<IReadOnlyList<UserBranchResponse>>.Success(items);
    }
}
