using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Roles;

public static class RoleMappingExtensions
{
    public static RoleResponse ToResponse(this Role r) => new(
        Code: r.Code,
        Name: r.Name,
        Description: r.Description,
        RoleType: r.RoleType,
        TenantId: r.TenantId,
        PermissionCodes: r.RolePermissions
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .ToList()
    );
}
