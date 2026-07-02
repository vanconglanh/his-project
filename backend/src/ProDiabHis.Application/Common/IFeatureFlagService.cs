namespace ProDiabHis.Application.Common;

/// <summary>
/// Feature flag service. Doc tu config + DB table diab_his_sys_feature_flags.
/// Config key override DB: FeatureFlags:{key} = true/false.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>Kiem tra feature flag co enabled khong.</summary>
    Task<bool> IsEnabledAsync(string key, CancellationToken ct = default);

    /// <summary>Lay danh sach tat ca feature flags.</summary>
    Task<IReadOnlyList<FeatureFlagDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Update trang thai feature flag.</summary>
    Task SetEnabledAsync(string key, bool enabled, CancellationToken ct = default);
}

public record FeatureFlagDto(string Key, bool Enabled, string? Description, DateTime UpdatedAt);
