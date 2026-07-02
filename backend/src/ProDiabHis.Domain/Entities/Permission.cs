using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Quyen thao tac trong he thong. Map bang sec_permissions</summary>
public class Permission : BaseEntity
{
    /// <summary>Ma quyen dang resource.action, vd: patient.read</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Nhom tai nguyen, vd: patient</summary>
    public string Resource { get; set; } = string.Empty;
    /// <summary>Hanh dong, vd: read</summary>
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
