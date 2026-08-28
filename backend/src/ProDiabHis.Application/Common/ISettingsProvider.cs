namespace ProDiabHis.Application.Common;

/// <summary>
/// D8 (docs/erd/goi-dich-vu-dinh-muc.md) - doc cau hinh dang key-value tu
/// bang diab_his_sys_settings. Uu tien: gia tri rieng cua tenant hien tai
/// (neu co) > gia tri global (tenant_id IS NULL) > defaultValue truyen vao.
/// </summary>
public interface ISettingsProvider
{
    Task<string?> GetRawAsync(string key, CancellationToken ct = default);

    Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default);

    Task<decimal> GetDecimalAsync(string key, decimal defaultValue, CancellationToken ct = default);

    Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default);

    /// <summary>Set/override gia tri cho tenant hien tai (tenant_id lay tu ITenantProvider).</summary>
    Task SetAsync(string key, string value, CancellationToken ct = default);
}
