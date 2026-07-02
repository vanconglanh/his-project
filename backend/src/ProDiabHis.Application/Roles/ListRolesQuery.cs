using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Roles;

public record ListRolesQuery : IRequest<Result<IReadOnlyList<RoleResponse>>>;

public class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    private readonly IApplicationDbContext _db;

    public ListRolesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(ListRolesQuery req, CancellationToken ct)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Where(r => r.DeletedAt == null && r.IsActive)
            .OrderBy(r => r.RoleType)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

        return Result<IReadOnlyList<RoleResponse>>.Success(
            roles.Select(r => r.ToResponse()).ToList());
    }
}
