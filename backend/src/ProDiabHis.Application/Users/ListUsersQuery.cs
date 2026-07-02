using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Users;

public record ListUsersQuery(
    int Page,
    int PageSize,
    string? Role,
    string? Status,
    string? Search
) : IRequest<PagedResult<UserResponse>>;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserResponse>> Handle(ListUsersQuery req, CancellationToken ct)
    {
        var query = _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrEmpty(req.Status))
            query = query.Where(u => u.Status == req.Status);

        if (!string.IsNullOrEmpty(req.Role))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Code == req.Role));

        if (!string.IsNullOrEmpty(req.Search))
            query = query.Where(u => u.FullName.Contains(req.Search) || u.Email.Contains(req.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserResponse>(items.Select(u => u.ToResponse()), req.Page, req.PageSize, total);
    }
}
