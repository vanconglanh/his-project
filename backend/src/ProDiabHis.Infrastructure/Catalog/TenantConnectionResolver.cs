using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Catalog;

/// <summary>
/// Resolve connection string per tenant tu catalog (cat_tenant_databases) + resolve tenant tu Host
/// (cat_tenant_domains). Singleton — cache ket qua trong IMemoryCache (TTL 5 phut).
/// Fallback DefaultConnection khi tenant chua co DB rieng -> che do shared-DB chay nhu cu.
/// </summary>
public class TenantConnectionResolver : ITenantConnectionResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly string _defaultConn;
    private readonly string _catalogConn;
    private readonly IEncryptionService _encryption;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantConnectionResolver> _logger;

    public TenantConnectionResolver(
        IConfiguration config,
        IEncryptionService encryption,
        IMemoryCache cache,
        ILogger<TenantConnectionResolver> logger)
    {
        _defaultConn = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        // Catalog rieng neu cau hinh, mac dinh dung chung DefaultConnection (giai doan dau)
        _catalogConn = config.GetConnectionString("Catalog") ?? _defaultConn;
        _encryption = encryption;
        _cache = cache;
        _logger = logger;
    }

    public string ResolveConnectionString(int tenantId)
    {
        if (tenantId <= 0) return _defaultConn;
        return _cache.GetOrCreate($"tenant-conn:{tenantId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return BuildConnectionString(tenantId);
        })!;
    }

    private string BuildConnectionString(int tenantId)
    {
        try
        {
            using var conn = new MySqlConnection(_catalogConn);
            var row = conn.QueryFirstOrDefault<TenantDbRow>(
                @"SELECT server_host AS ServerHost, server_port AS ServerPort, db_name AS DbName,
                         db_user AS DbUser, db_password_encrypted AS DbPasswordEncrypted
                  FROM cat_tenant_databases WHERE tenant_id = @tenantId",
                new { tenantId });

            if (row is null)
            {
                // Chua co DB rieng -> shared DB (tenant_id + query filter van cach ly du lieu)
                _logger.LogDebug("Tenant {TenantId} chua co DB rieng trong catalog, dung DefaultConnection", tenantId);
                return _defaultConn;
            }

            var password = _encryption.Decrypt(row.DbPasswordEncrypted);
            // Clone template DefaultConnection de giu nguyen option (GuidFormat, SSL, pooling, charset...)
            var builder = new MySqlConnectionStringBuilder(_defaultConn)
            {
                Server = row.ServerHost,
                Port = (uint)row.ServerPort,
                Database = row.DbName,
                UserID = row.DbUser,
                Password = password,
            };
            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi resolve connection tenant {TenantId}, fallback DefaultConnection", tenantId);
            return _defaultConn;
        }
    }

    public TenantRouteInfo? ResolveByHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        var key = host.Split(':')[0].Trim().ToLowerInvariant(); // bo port neu co
        return _cache.GetOrCreate($"tenant-host:{key}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            try
            {
                using var conn = new MySqlConnection(_catalogConn);
                return conn.QueryFirstOrDefault<TenantRouteInfo>(
                    @"SELECT t.id AS TenantId, t.code AS Code, t.status AS Status
                      FROM cat_tenant_domains d
                      JOIN cat_tenants t ON t.id = d.tenant_id
                      WHERE d.domain = @key
                        AND d.verification_status = 'verified'
                        AND t.deleted_at IS NULL
                      LIMIT 1",
                    new { key });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi resolve tenant tu host {Host}", key);
                return null;
            }
        });
    }

    public void InvalidateTenant(int tenantId) => _cache.Remove($"tenant-conn:{tenantId}");

    private sealed class TenantDbRow
    {
        public string ServerHost { get; set; } = "";
        public int ServerPort { get; set; }
        public string DbName { get; set; } = "";
        public string DbUser { get; set; } = "";
        public string DbPasswordEncrypted { get; set; } = "";
    }
}
