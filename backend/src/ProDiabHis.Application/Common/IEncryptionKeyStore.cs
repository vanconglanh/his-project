namespace ProDiabHis.Application.Common;

/// <summary>Muc dich cua encryption key</summary>
public enum KeyPurpose
{
    PII,
    BHYT,
    OAUTH_TOKEN,
    VAPID,
    OTHER
}

/// <summary>Thong tin encryption key (khong lo thiet key material)</summary>
public record EncryptionKeyInfo(
    long Id,
    int? TenantId,
    int KeyVersion,
    KeyPurpose Purpose,
    string Algorithm,
    bool IsActive,
    DateTime? RotatedAt,
    DateTime CreatedAt
);

/// <summary>Load va cache encryption keys tu storage</summary>
public interface IEncryptionKeyStore
{
    /// <summary>Lay active key material (byte[]) cho (tenantId, purpose). tenantId=null => global key.</summary>
    Task<byte[]> GetActiveKeyMaterialAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default);

    /// <summary>Lay tat ca key info (khong lo material) cho tenant/purpose</summary>
    Task<IReadOnlyList<EncryptionKeyInfo>> ListKeysAsync(int? tenantId, KeyPurpose? purpose, CancellationToken ct = default);

    /// <summary>Invalidate cache cho (tenantId, purpose) sau khi rotate</summary>
    Task InvalidateCacheAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default);
}
