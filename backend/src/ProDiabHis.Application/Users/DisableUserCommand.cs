using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record DisableUserCommand(Guid Id) : IRequest<Result<UserResponse>>;

public class DisableUserCommandHandler : IRequestHandler<DisableUserCommand, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public DisableUserCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> Handle(DisableUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == req.Id && u.DeletedAt == null, ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        user.Status = UserStatus.Locked;
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", req.Id.ToString(), new { action = "disable" }, ct);

        return Result<UserResponse>.Success(user.ToResponse());
    }
}

public record EnableUserCommand(Guid Id) : IRequest<Result<UserResponse>>;

public class EnableUserCommandHandler : IRequestHandler<EnableUserCommand, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public EnableUserCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> Handle(EnableUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == req.Id && u.DeletedAt == null, ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        user.Status = UserStatus.Active;
        user.IsActive = true;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", req.Id.ToString(), new { action = "enable" }, ct);

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
