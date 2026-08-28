using System.Globalization;
using Dapper;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Services;

/// <summary>
/// Trien khai ISettingsProvider - doc/ghi bang diab_his_sys_settings (migration 9095).
/// Uu tien tenant-specific row truoc, roi den row global (tenant_id IS NULL).
/// </summary>
public class SettingsProvider : ISettingsProvider
{
    private readonly IDapperConnectionFactory _dbFactory;
    private readonly ITenantProvider _tenant;

    public SettingsProvider(IDapperConnectionFactory dbFactory, ITenantProvider tenant)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
    }

    public async Task<string?> GetRawAsync(string key, CancellationToken ct = default)
    {
        using var conn = _dbFactory.CreateConnection();
        var tenantId = _tenant.TenantId;

        // Uu tien row rieng cua tenant, fallback row global (tenant_id IS NULL)
        var value = await conn.QueryFirstOrDefaultAsync<string?>(
            @"SELECT setting_value FROM diab_his_sys_settings
              WHERE setting_key=@key AND tenant_id=@tenantId
              ORDER BY tenant_id DESC LIMIT 1",
            new { key, tenantId });
        if (value != null) return value;

        return await conn.QueryFirstOrDefaultAsync<string?>(
            @"SELECT setting_value FROM diab_his_sys_settings
              WHERE setting_key=@key AND tenant_id IS NULL LIMIT 1",
            new { key });
    }

    public async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return raw != null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return raw != null && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return raw != null && bool.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        using var conn = _dbFactory.CreateConnection();
        var tenantId = _tenant.TenantId;
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sys_settings (id, tenant_id, setting_key, setting_value)
              VALUES (UUID(), @tenantId, @key, @value)
              ON DUPLICATE KEY UPDATE setting_value=@value, updated_at=UTC_TIMESTAMP(3)",
            new { tenantId, key, value });
    }
}
