using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record UpdateUserCommand(Guid Id, string? FullName, string? Phone, string? AvatarUrl)
    : IRequest<Result<UserResponse>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public UpdateUserCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == req.Id && u.DeletedAt == null, ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.Phone != null) user.Phone = req.Phone;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "user", req.Id.ToString(),
            new { fields = new[] { "full_name", "phone", "avatar_url" } }, ct);

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
