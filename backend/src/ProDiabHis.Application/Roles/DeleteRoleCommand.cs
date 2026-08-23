using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Roles;

public record DeleteRoleCommand(string Code) : IRequest<Result>;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public DeleteRoleCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result> Handle(DeleteRoleCommand req, CancellationToken ct)
    {
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Code == req.Code && r.DeletedAt == null, ct);

        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Không tìm thấy vai trò");

        if (role.RoleType == RoleType.System)
        {
            // Ghi nhan hanh vi bi tu choi (compliance CLAUDE.md: audit moi thao tac tren vai tro,
            // dac biet vi role la vector vua duoc va lo hong leo thang quyen ROLE_CODE_RESERVED)
            await _audit.LogAsync(
                "DELETE_DENIED",
                "ROLE",
                role.Id.ToString(),
                AuditSeverity.WARN,
                crossTenantAttempt: false,
                requestId: null,
                details: new { code = role.Code, reason = "ROLE_SYSTEM_PROTECTED", userId = _currentUser.UserId },
                cancellationToken: ct);

            return Result.Failure("ROLE_SYSTEM_PROTECTED", "Không thể xóa vai trò hệ thống");
        }

        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = _currentUser.UserId;
        role.IsActive = false;

        // Chu dong don dep UserRole tro toi role vua bi xoa mem, tranh de ton du khien user van
        // duoc nap nham quyen tu role da xoa (xem RevokeRoleCommand la luoi an toan phu cho truong
        // hop nay). Xoa cung 1 SaveChangesAsync (1 transaction) voi thao tac soft-delete role o tren.
        var userRolesToRemove = await _db.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .ToListAsync(ct);
        if (userRolesToRemove.Count > 0)
            _db.UserRoles.RemoveRange(userRolesToRemove);

        await _db.SaveChangesAsync(ct);

        // Audit: xoa vai tro thanh cong (role la vector vua duoc va lo hong leo thang quyen)
        await _audit.LogAsync("DELETE", "ROLE", role.Id.ToString(),
            new { code = role.Code, name = role.Name, userId = _currentUser.UserId, revokedUserRoleCount = userRolesToRemove.Count },
            ct);

        return Result.Success();
    }
}
