using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.FeatureFlags;

/// <summary>
/// Feature flag service. Priority: config override > DB table diab_his_sys_feature_flags.
/// Config key: FeatureFlags:{key} = true/false
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IDapperConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(
        IDapperConnectionFactory db,
        IConfiguration config,
        ILogger<FeatureFlagService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string key, CancellationToken ct = default)
    {
        // Config override
        var configVal = _config[$"FeatureFlags:{key}"];
        if (!string.IsNullOrEmpty(configVal) && bool.TryParse(configVal, out var configEnabled))
            return configEnabled;

        // DB lookup
        try
        {
            using var conn = _db.CreateConnection();
            var enabled = await conn.QuerySingleOrDefaultAsync<bool?>(
                "SELECT enabled FROM diab_his_sys_feature_flags WHERE `key` = @key LIMIT 1",
                new { key });
            return enabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feature flag DB lookup failed for key={Key}, defaulting to false", key);
            return false;
        }
    }

    public async Task<IReadOnlyList<FeatureFlagDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<FeatureFlagRow>(
            "SELECT `key`, enabled, description, updated_at FROM diab_his_sys_feature_flags ORDER BY `key`");

        return rows
            .Select(r => new FeatureFlagDto(r.Key, r.Enabled, r.Description, r.UpdatedAt))
            .ToList()
            .AsReadOnly();
    }

    public async Task SetEnabledAsync(string key, bool enabled, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sys_feature_flags (`key`, enabled, updated_at)
              VALUES (@key, @enabled, @now)
              ON DUPLICATE KEY UPDATE enabled = @enabled, updated_at = @now",
            new { key, enabled, now = DateTime.UtcNow });

        _logger.LogInformation("Feature flag {Key} set to {Enabled}", key, enabled);
    }

    private record FeatureFlagRow(string Key, bool Enabled, string? Description, DateTime UpdatedAt);
}
