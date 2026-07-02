using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Roles;

public record ListPermissionsQuery(string? Resource) : IRequest<Result<IReadOnlyList<PermissionResponse>>>;

public class ListPermissionsQueryHandler : IRequestHandler<ListPermissionsQuery, Result<IReadOnlyList<PermissionResponse>>>
{
    private readonly IApplicationDbContext _db;

    public ListPermissionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(ListPermissionsQuery req, CancellationToken ct)
    {
        var query = _db.Permissions.AsQueryable();

        if (!string.IsNullOrEmpty(req.Resource))
            query = query.Where(p => p.Resource == req.Resource);

        var permissions = await query
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionResponse(p.Code, p.Resource, p.Action, p.Description))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PermissionResponse>>.Success(permissions);
    }
}
