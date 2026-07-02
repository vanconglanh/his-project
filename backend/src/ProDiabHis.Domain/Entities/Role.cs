using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Vai tro trong he thong. Map bang sec_roles</summary>
public class Role : BaseEntity
{
    /// <summary>Ma vai tro duy nhat, vd: ADMIN, BACSI, CUSTOM_ROLE_01</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>SYSTEM hoac CUSTOM</summary>
    public string RoleType { get; set; } = "SYSTEM";
    /// <summary>NULL cho SYSTEM role, UUID cho CUSTOM role</summary>
    public int? TenantId { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public static class RoleType
{
    public const string System = "SYSTEM";
    public const string Custom = "CUSTOM";
}
