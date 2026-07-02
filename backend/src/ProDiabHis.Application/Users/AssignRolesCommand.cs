using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record AssignRolesCommand(Guid UserId, IEnumerable<string> RoleCodes) : IRequest<Result<UserResponse>>;

public class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _audit;

    public AssignRolesCommandHandler(IApplicationDbContext db, ITenantProvider tenantProvider, IAuditService audit)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> Handle(AssignRolesCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == req.UserId && u.DeletedAt == null, ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        var roleCodes = req.RoleCodes.ToList();
        var roles = await _db.Roles.IgnoreQueryFilters()
            .Where(r => roleCodes.Contains(r.Code) && r.DeletedAt == null)
            .ToListAsync(ct);

        if (roles.Count != roleCodes.Count)
            return Result<UserResponse>.Failure("ROLE_NOT_FOUND", "Một hoặc nhiều vai trò không tồn tại");

        foreach (var role in roles)
        {
            if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    TenantId = _tenantProvider.TenantId
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", req.UserId.ToString(),
            new { action = "assign_roles", roles = roleCodes }, ct);

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
