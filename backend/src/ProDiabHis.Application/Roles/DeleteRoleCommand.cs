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

    public DeleteRoleCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteRoleCommand req, CancellationToken ct)
    {
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Code == req.Code && r.DeletedAt == null, ct);

        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Không tìm thấy vai trò");

        if (role.RoleType == RoleType.System)
            return Result.Failure("ROLE_SYSTEM_PROTECTED", "Không thể xóa vai trò hệ thống");

        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = _currentUser.UserId;
        role.IsActive = false;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
