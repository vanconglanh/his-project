using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record RevokeRoleCommand(Guid UserId, string RoleCode) : IRequest<Result>;

public class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public RevokeRoleCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Result> Handle(RevokeRoleCommand req, CancellationToken ct)
    {
        // IgnoreQueryFilters() o day de tim duoc UserRole tro toi Role DA BI XOA MEM (Role.DeletedAt
        // != null) — muc dich la de DON DEP (xoa UserRole thua), KHONG phai de cap quyen. Neu khong
        // co dong nay, global query filter cua Role se loc mat navigation ur.Role khi role da bi xoa
        // mem, khien handler tra 404 sai va Admin khong the tu don dep UserRole tro toi role da xoa.
        var userRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ur =>
                ur.UserId == req.UserId &&
                ur.Role != null &&
                ur.Role.Code == req.RoleCode, ct);

        if (userRole is null)
            return Result.Failure("USER_ROLE_NOT_FOUND", "Người dùng không có vai trò này");

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", req.UserId.ToString(),
            new { action = "revoke_role", role = req.RoleCode }, ct);

        return Result.Success();
    }
}
