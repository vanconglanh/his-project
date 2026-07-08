using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Phong kham / tenant trong he thong SaaS. Map bang diab_his_sys_tenants.
/// PK la INT AUTO_INCREMENT (khong dung Guid cua BaseEntity) de khop cot tenant_id INT
/// o toan bo bang nghiep vu + claim tenant_id trong JWT.
/// </summary>
public class Tenant : IAuditTimestamps
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CskcbCode { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? EmailSupport { get; set; }
    public string? LogoUrl { get; set; }
    public string? Slogan { get; set; }
    public string? Website { get; set; }
    public string Subdomain { get; set; } = string.Empty;
    public int StorageQuotaGb { get; set; } = 20;
    public string Status { get; set; } = TenantStatus.Active;
    public DateTime? ExpiresAt { get; set; }
    /// <summary>BHYT token ma hoa AES-256-GCM</summary>
    public string? BhytTokenEncrypted { get; set; }

    // Audit columns (kieu int khop schema diab_his_sys_tenants)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}

public static class TenantStatus
{
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Terminated = "TERMINATED";
}
