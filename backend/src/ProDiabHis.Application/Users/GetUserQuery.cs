using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record GetUserQuery(Guid Id) : IRequest<Result<UserResponse>>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetUserQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<UserResponse>> Handle(GetUserQuery req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.Id == req.Id && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<UserResponse>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng");

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
