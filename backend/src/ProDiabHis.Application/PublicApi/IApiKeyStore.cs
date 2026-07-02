namespace ProDiabHis.Application.PublicApi;

public record ApiPartnerContext(
    Guid PartnerId,
    int TenantId,
    string Name,
    IReadOnlyList<string> Scopes,
    int RateLimitPerMin,
    int DailyQuota,
    string Status,
    DateTime? ExpiresAt,
    IReadOnlyList<string> IpWhitelist
);

/// <summary>Tra cuu API partner tu hash cua API key</summary>
public interface IApiKeyStore
{
    Task<ApiPartnerContext?> FindByHashAsync(string sha256Hex, CancellationToken cancellationToken = default);
    Task LogRequestAsync(Guid partnerId, int tenantId, string method, string path,
        int statusCode, int durationMs, string? ip, string? errorCode,
        CancellationToken cancellationToken = default);
}
