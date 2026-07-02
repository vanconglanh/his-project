using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record UpdateMeCommand(string? FullName, string? Phone, string? AvatarUrl)
    : IRequest<Result<UserResponse>>;

public class UpdateMeCommandHandler : IRequestHandler<UpdateMeCommand, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateMeCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserResponse>> Handle(UpdateMeCommand req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<UserResponse>.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value && u.DeletedAt == null, ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.Phone != null) user.Phone = req.Phone;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;

        await _db.SaveChangesAsync(ct);
        return Result<UserResponse>.Success(user.ToResponse());
    }
}
