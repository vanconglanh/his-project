using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Infrastructure.Security;
using System.Security.Cryptography;

namespace ProDiabHis.Infrastructure.Notifications;

public class VapidKeyServiceImpl : IVapidKeyService
{
    private readonly Application.Common.IDapperConnectionFactory _factory;
    private readonly IEncryptionService _encryption;

    public VapidKeyServiceImpl(Application.Common.IDapperConnectionFactory factory, IEncryptionService encryption)
    {
        _factory = factory;
        _encryption = encryption;
    }

    public async Task<string?> GetPublicKeyAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT public_key FROM diab_his_nti_vapid_keys WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }

    public async Task<VapidKeyStatus> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT public_key, created_at FROM diab_his_nti_vapid_keys WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });

        if (row == null)
            return new VapidKeyStatus(false, null, null);

        return new VapidKeyStatus(true, (string)row.public_key, (DateTime?)row.created_at);
    }

    public async Task<VapidKeyPair> RegenerateAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var exportedPub = ecdsa.ExportSubjectPublicKeyInfo();
        var exportedPriv = ecdsa.ExportPkcs8PrivateKey();

        var pubBase64 = Convert.ToBase64String(exportedPub);
        var privBase64 = Convert.ToBase64String(exportedPriv);

        var privEncrypted = _encryption.Encrypt(privBase64);
        var privBytes = Convert.FromBase64String(privEncrypted);

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_vapid_keys (id, tenant_id, public_key, private_key_encrypted, created_at, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @PubKey, @PrivKey, UTC_TIMESTAMP(), UTC_TIMESTAMP())
              ON DUPLICATE KEY UPDATE public_key = @PubKey, private_key_encrypted = @PrivKey,
                                      created_at = UTC_TIMESTAMP(), updated_at = UTC_TIMESTAMP()",
            new { Id = Guid.NewGuid().ToString(), TenantId = tenantId, PubKey = pubBase64, PrivKey = privBytes });

        return new VapidKeyPair(pubBase64, privBase64);
    }

    public async Task<VapidKeyPair> GetOrCreateKeyPairAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<(string public_key, byte[] private_key_encrypted)>(
            "SELECT public_key, private_key_encrypted FROM diab_his_nti_vapid_keys WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });

        if (row != default)
        {
            var privDecrypted = _encryption.Decrypt(Convert.ToBase64String(row.private_key_encrypted));
            return new VapidKeyPair(row.public_key, privDecrypted);
        }

        // Generate new ECDSA P-256 keypair for VAPID
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var exportedPub = ecdsa.ExportSubjectPublicKeyInfo();
        var exportedPriv = ecdsa.ExportPkcs8PrivateKey();

        var pubBase64 = Convert.ToBase64String(exportedPub);
        var privBase64 = Convert.ToBase64String(exportedPriv);

        var privEncrypted = _encryption.Encrypt(privBase64);
        var privBytes = Convert.FromBase64String(privEncrypted);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_vapid_keys (id, tenant_id, public_key, private_key_encrypted, created_at, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @PubKey, @PrivKey, UTC_TIMESTAMP(), UTC_TIMESTAMP())
              ON DUPLICATE KEY UPDATE public_key = @PubKey, private_key_encrypted = @PrivKey, updated_at = UTC_TIMESTAMP()",
            new { Id = Guid.NewGuid().ToString(), TenantId = tenantId, PubKey = pubBase64, PrivKey = privBytes });

        return new VapidKeyPair(pubBase64, privBase64);
    }
}
