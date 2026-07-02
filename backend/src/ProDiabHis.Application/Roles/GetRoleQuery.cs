using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Roles;

public record GetRoleQuery(string Code) : IRequest<Result<RoleResponse>>;

public class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetRoleQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoleResponse>> Handle(GetRoleQuery req, CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Code == req.Code && r.DeletedAt == null, ct);

        if (role is null)
            return Result<RoleResponse>.Failure("ROLE_NOT_FOUND", "Không tìm thấy vai trò");

        return Result<RoleResponse>.Success(role.ToResponse());
    }
}
