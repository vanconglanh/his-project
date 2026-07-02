using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using StackExchange.Redis;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Load encryption key material tu DB, decrypt bang master KEK, cache vao Redis 10 phut.
/// Key material luu trong DB duoc encrypt bang master key (Encryption:MasterKey trong config).
/// </summary>
public class EncryptionKeyStoreImpl : IEncryptionKeyStore
{
    private readonly IDapperConnectionFactory _dapper;
    private readonly IConnectionMultiplexer? _redis;
    private readonly byte[] _masterKey;
    private readonly ILogger<EncryptionKeyStoreImpl> _logger;
    private const int CacheMinutes = 10;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public EncryptionKeyStoreImpl(
        IDapperConnectionFactory dapper,
        IConfiguration configuration,
        ILogger<EncryptionKeyStoreImpl> logger,
        IConnectionMultiplexer? redis = null)
    {
        _dapper = dapper;
        _redis = redis;
        _logger = logger;

        var masterKey = configuration["Encryption:MasterKey"]
            ?? throw new InvalidOperationException("Encryption:MasterKey chua duoc cau hinh");
        _masterKey = Convert.FromBase64String(masterKey);
        if (_masterKey.Length != 32)
            throw new InvalidOperationException("Encryption:MasterKey phai la 32 bytes (256 bit)");
    }

    public async Task<byte[]> GetActiveKeyMaterialAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(tenantId, purpose);

        // Try Redis cache
        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
                return (byte[])cached!;
        }

        // Query DB
        using var conn = _dapper.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT key_material_encrypted FROM diab_his_sec_encryption_keys
              WHERE (tenant_id = @tenantId OR (tenant_id IS NULL AND @tenantId IS NULL))
                AND key_purpose = @purpose
                AND is_active = 1
              ORDER BY key_version DESC
              LIMIT 1",
            new { tenantId, purpose = purpose.ToString() });

        if (row == null)
            throw new InvalidOperationException($"Khong tim thay active encryption key cho purpose={purpose} tenant={tenantId}");

        byte[] encryptedMaterial = (byte[])row.key_material_encrypted;
        var rawKey = DecryptKeyMaterial(encryptedMaterial);

        // Cache vao Redis
        if (_redis != null && _redis.IsConnected)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(cacheKey, rawKey, TimeSpan.FromMinutes(CacheMinutes));
        }

        return rawKey;
    }

    public async Task<IReadOnlyList<EncryptionKeyInfo>> ListKeysAsync(int? tenantId, KeyPurpose? purpose, CancellationToken ct = default)
    {
        using var conn = _dapper.CreateConnection();

        var sql = @"SELECT id, tenant_id, key_version, key_purpose, algorithm, is_active, rotated_at, created_at
                    FROM diab_his_sec_encryption_keys
                    WHERE 1=1";

        var parameters = new DynamicParameters();
        if (tenantId.HasValue)
        {
            sql += " AND (tenant_id = @tenantId OR tenant_id IS NULL)";
            parameters.Add("tenantId", tenantId.Value);
        }
        else
        {
            sql += " AND tenant_id IS NULL";
        }

        if (purpose.HasValue)
        {
            sql += " AND key_purpose = @purpose";
            parameters.Add("purpose", purpose.ToString());
        }

        sql += " ORDER BY key_purpose, key_version DESC";

        var rows = await conn.QueryAsync<dynamic>(sql, parameters);

        return rows.Select(r => new EncryptionKeyInfo(
            Id: (long)r.id,
            TenantId: (int?)r.tenant_id,
            KeyVersion: (int)r.key_version,
            Purpose: Enum.Parse<KeyPurpose>((string)r.key_purpose),
            Algorithm: (string)r.algorithm,
            IsActive: (bool)((sbyte)r.is_active == 1),
            RotatedAt: (DateTime?)r.rotated_at,
            CreatedAt: (DateTime)r.created_at
        )).ToList();
    }

    public async Task InvalidateCacheAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default)
    {
        if (_redis == null || !_redis.IsConnected) return;

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildCacheKey(tenantId, purpose));
        _logger.LogInformation("Invalidated encryption key cache: tenant={TenantId} purpose={Purpose}", tenantId, purpose);
    }

    private static string BuildCacheKey(int? tenantId, KeyPurpose purpose)
        => $"enc_key:{(tenantId?.ToString() ?? "global")}:{purpose}";

    private byte[] DecryptKeyMaterial(byte[] encryptedMaterial)
    {
        // Format: nonce[12] + tag[16] + ciphertext
        if (encryptedMaterial.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Key material format khong hop le");

        var nonce = encryptedMaterial[..NonceSizeBytes];
        var tag = encryptedMaterial[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var ciphertext = encryptedMaterial[(NonceSizeBytes + TagSizeBytes)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    /// <summary>Encrypt raw key bytes bang master KEK de luu vao DB</summary>
    public byte[] EncryptKeyMaterial(byte[] rawKey)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[rawKey.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Encrypt(nonce, rawKey, ciphertext, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertext.CopyTo(result, NonceSizeBytes + TagSizeBytes);
        return result;
    }
}
