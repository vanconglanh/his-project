using Dapper;
using ProDiabHis.Application.PublicApi;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.ApiKey;

public class ApiKeyStoreImpl : IApiKeyStore
{
    private readonly Application.Common.IDapperConnectionFactory _factory;

    public ApiKeyStoreImpl(Application.Common.IDapperConnectionFactory factory)
        => _factory = factory;

    public async Task<ApiPartnerContext?> FindByHashAsync(string sha256Hex, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT BIN_TO_UUID(id) AS id, tenant_id, name, scopes, rate_limit_per_min,
                     daily_quota, status, expires_at, ip_whitelist
              FROM diab_his_api_partners
              WHERE api_key_hash = @Hash AND deleted_at IS NULL",
            new { Hash = sha256Hex });

        if (row == null) return null;

        var scopes = row.scopes != null
            ? JsonSerializer.Deserialize<List<string>>((string)row.scopes) ?? new()
            : new List<string>();
        var ipList = row.ip_whitelist != null
            ? JsonSerializer.Deserialize<List<string>>((string)row.ip_whitelist) ?? new()
            : new List<string>();

        return new ApiPartnerContext(
            Guid.Parse((string)row.id),
            (int)row.tenant_id,
            (string)row.name,
            scopes.AsReadOnly(),
            (int)row.rate_limit_per_min,
            (int)row.daily_quota,
            (string)row.status,
            (DateTime?)row.expires_at,
            ipList.AsReadOnly());
    }

    public async Task LogRequestAsync(Guid partnerId, int tenantId, string method, string path,
        int statusCode, int durationMs, string? ip, string? errorCode,
        CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_api_request_logs
                (id, tenant_id, partner_id, method, path, status_code, duration_ms, ip, error_code, called_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@PartnerId), @Method, @Path,
                      @StatusCode, @DurationMs, @Ip, @ErrorCode, UTC_TIMESTAMP())",
            new
            {
                Id = Guid.NewGuid().ToString(), tenantId, PartnerId = partnerId.ToString(),
                method, path, statusCode, durationMs, ip, errorCode
            });
    }
}
