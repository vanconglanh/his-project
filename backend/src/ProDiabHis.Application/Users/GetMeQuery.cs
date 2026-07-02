using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record GetMeQuery : IRequest<Result<UserResponse>>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMeQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserResponse>> Handle(GetMeQuery req, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<UserResponse>.Failure("AUTH_TOKEN_INVALID", "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại");

        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.Id == _currentUser.UserId.Value && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
