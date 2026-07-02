namespace ProDiabHis.Domain.Entities;

/// <summary>Bang trung gian User - Role. Map bang sec_user_roles</summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public int TenantId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}
