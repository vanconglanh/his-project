using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Doi tac B2B su dung Public API. Map bang diab_his_api_partners</summary>
public class ApiPartner : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public string ApiKeyPrefix { get; set; } = string.Empty;
    public string ScopesJson { get; set; } = "[]";
    public int RateLimitPerMin { get; set; } = 60;
    public int DailyQuota { get; set; } = 10000;
    public string Status { get; set; } = ApiPartnerStatus.Active;
    public DateTime? ExpiresAt { get; set; }
    public string? IpWhitelistJson { get; set; }
}

public static class ApiPartnerStatus
{
    public const string Active = "ACTIVE";
    public const string Disabled = "DISABLED";
    public const string Expired = "EXPIRED";
}
