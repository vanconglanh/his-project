using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Roles;

public record UpdateRoleCommand(
    string Code,
    string? Name,
    string? Description,
    IEnumerable<string>? PermissionCodes
) : IRequest<Result<RoleResponse>>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _db;

    public UpdateRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand req, CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Code == req.Code && r.DeletedAt == null, ct);

        if (role is null)
            return Result<RoleResponse>.Failure("ROLE_NOT_FOUND", "Không tìm thấy vai trò");

        if (role.RoleType == RoleType.System)
            return Result<RoleResponse>.Failure("ROLE_SYSTEM_PROTECTED", "Không thể xóa vai trò hệ thống");

        if (req.Name != null) role.Name = req.Name;
        if (req.Description != null) role.Description = req.Description;

        if (req.PermissionCodes != null)
        {
            var permCodes = req.PermissionCodes.ToList();
            var permissions = await _db.Permissions
                .Where(p => permCodes.Contains(p.Code))
                .ToListAsync(ct);

            if (permissions.Count != permCodes.Count)
                return Result<RoleResponse>.Failure("PERMISSION_NOT_FOUND", "Một hoặc nhiều quyền không tồn tại");

            // Xoa toan bo va them lai
            foreach (var rp in role.RolePermissions.ToList())
                _db.RolePermissions.Remove(rp);

            foreach (var perm in permissions)
                _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        }

        await _db.SaveChangesAsync(ct);
        return Result<RoleResponse>.Success(role.ToResponse());
    }
}
