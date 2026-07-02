using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Phong kham / tenant trong he thong SaaS. Map bang diab_his_sys_tenants</summary>
public class Tenant : BaseEntity
{
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
    public string Subdomain { get; set; } = string.Empty;
    public int StorageQuotaGb { get; set; } = 20;
    public string Status { get; set; } = TenantStatus.Active;
    public DateTime? ExpiresAt { get; set; }
    /// <summary>BHYT token ma hoa AES-256-GCM</summary>
    public string? BhytTokenEncrypted { get; set; }

}

public static class TenantStatus
{
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Terminated = "TERMINATED";
}
