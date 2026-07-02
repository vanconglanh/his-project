using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>Thuc hien rotation encryption key: gen new, deactivate old, cache invalidate</summary>
public class KeyRotationServiceImpl : IKeyRotationService
{
    private readonly IDapperConnectionFactory _dapper;
    private readonly EncryptionKeyStoreImpl _keyStore;
    private readonly ILogger<KeyRotationServiceImpl> _logger;
    private const int KeySizeBytes = 32; // AES-256

    public KeyRotationServiceImpl(
        IDapperConnectionFactory dapper,
        EncryptionKeyStoreImpl keyStore,
        ILogger<KeyRotationServiceImpl> logger)
    {
        _dapper = dapper;
        _keyStore = keyStore;
        _logger = logger;
    }

    public async Task<RotateKeyResult> RotateKeyAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting key rotation: tenant={TenantId} purpose={Purpose}", tenantId, purpose);

        using var conn = _dapper.CreateConnection();

        // Lay version hien tai cao nhat
        var maxVersion = await conn.ExecuteScalarAsync<int?>(
            @"SELECT MAX(key_version) FROM diab_his_sec_encryption_keys
              WHERE key_purpose = @purpose
                AND (tenant_id = @tenantId OR (tenant_id IS NULL AND @tenantId IS NULL))",
            new { purpose = purpose.ToString(), tenantId }) ?? 0;

        var newVersion = maxVersion + 1;

        // Gen new random key
        var rawKey = RandomNumberGenerator.GetBytes(KeySizeBytes);
        var encryptedMaterial = _keyStore.EncryptKeyMaterial(rawKey);

        // Deactivate old keys
        var deactivated = await conn.ExecuteAsync(
            @"UPDATE diab_his_sec_encryption_keys
              SET is_active = 0, rotated_at = @now
              WHERE key_purpose = @purpose
                AND is_active = 1
                AND (tenant_id = @tenantId OR (tenant_id IS NULL AND @tenantId IS NULL))",
            new { purpose = purpose.ToString(), tenantId, now = DateTime.UtcNow });

        // Insert new key
        var newKeyId = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO diab_his_sec_encryption_keys
                (tenant_id, key_version, key_purpose, key_material_encrypted, algorithm, is_active, created_at)
              VALUES
                (@tenantId, @newVersion, @purpose, @encMaterial, 'AES-256-GCM', 1, @now);
              SELECT LAST_INSERT_ID();",
            new
            {
                tenantId,
                newVersion,
                purpose = purpose.ToString(),
                encMaterial = encryptedMaterial,
                now = DateTime.UtcNow
            });

        // Invalidate Redis cache
        await _keyStore.InvalidateCacheAsync(tenantId, purpose, ct);

        _logger.LogInformation(
            "Key rotation complete: tenant={TenantId} purpose={Purpose} newVersion={Version} newId={Id} deactivated={Deactivated}",
            tenantId, purpose, newVersion, newKeyId, deactivated);

        return new RotateKeyResult(newKeyId, newVersion, deactivated);
    }

    public async Task RotateExpiredKeysAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking expired encryption keys (>365 days)...");

        using var conn = _dapper.CreateConnection();

        // Lay cac active key qua 365 ngay chua duoc rotate
        var expiredKeys = await conn.QueryAsync<dynamic>(
            @"SELECT DISTINCT tenant_id, key_purpose
              FROM diab_his_sec_encryption_keys
              WHERE is_active = 1
                AND created_at < DATE_SUB(NOW(), INTERVAL 365 DAY)");

        var count = 0;
        foreach (var key in expiredKeys)
        {
            if (ct.IsCancellationRequested) break;

            // Precompute typed variables truoc try block de tranh CS1973 (dynamic in catch)
            int? tenantId = (int?)key.tenant_id;
            string purposeStr = (string)key.key_purpose;
            string tenantLabel = tenantId?.ToString() ?? "global";

            try
            {
                var purpose = Enum.Parse<KeyPurpose>(purposeStr);
                await RotateKeyAsync(tenantId, purpose, ct);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi auto-rotate key: tenant={TenantId} purpose={Purpose}",
                    tenantLabel, purposeStr);
            }
        }

        _logger.LogInformation("Auto-rotation complete: {Count} keys rotated", count);
    }
}
