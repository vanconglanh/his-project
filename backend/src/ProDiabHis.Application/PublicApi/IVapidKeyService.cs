namespace ProDiabHis.Application.PublicApi;

public record VapidKeyPair(string PublicKey, string PrivateKey);
public record VapidKeyStatus(bool Configured, string? PublicKey, DateTime? GeneratedAt);

/// <summary>Quan ly VAPID keypair per tenant</summary>
public interface IVapidKeyService
{
    Task<string?> GetPublicKeyAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<VapidKeyPair> GetOrCreateKeyPairAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<VapidKeyStatus> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<VapidKeyPair> RegenerateAsync(int tenantId, CancellationToken cancellationToken = default);
}
