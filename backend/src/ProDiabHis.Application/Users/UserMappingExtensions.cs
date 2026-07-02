using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public static class UserMappingExtensions
{
    public static UserResponse ToResponse(this User u) => new(
        Id: u.Id,
        TenantId: u.TenantId,
        Email: u.Email,
        FullName: u.FullName,
        Phone: u.Phone,
        AvatarUrl: u.AvatarUrl,
        Status: u.Status,
        Roles: u.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => new RoleRef(ur.Role!.Code, ur.Role!.Name))
            .ToList(),
        Permissions: u.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToList(),
        TwoFaEnabled: u.TwoFaEnabled,
        LastLoginAt: u.LastLoginAt,
        CreatedAt: u.CreatedAt
    );
}
