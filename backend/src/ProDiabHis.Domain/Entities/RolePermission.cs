namespace ProDiabHis.Domain.Entities;

/// <summary>Mapping many-to-many giua Role va Permission. Map bang sec_role_permissions</summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
