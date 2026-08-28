namespace ProDiabHis.Application.Reports;

public interface IReportCache
{
    /// <summary>
    /// branchId: null = cache dung chung toan tenant (bao cao xem xuyen chi nhanh / branch.cross_view);
    /// != null = cache rieng cho 1 chi nhanh. Xem migration 9087_bil_counters_rep_cache_branch_unique.sql
    /// - unique key doi tu (tenant_id, period_key) sang (tenant_id, branch_id, period_key).
    /// </summary>
    Task<string?> GetAsync(string tableName, int tenantId, string periodKey, int? branchId = null, CancellationToken ct = default);
    Task SetAsync(string tableName, int tenantId, string periodKey, string dataJson, int? branchId = null, CancellationToken ct = default);
}
