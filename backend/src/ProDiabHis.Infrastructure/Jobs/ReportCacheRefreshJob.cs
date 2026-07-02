using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — lam moi 5 bang cache bao cao moi dem luc 02:00.
/// Cron: 0 2 * * *
/// </summary>
public class ReportCacheRefreshJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IReportingService _reporting;
    private readonly IReportCache _cache;
    private readonly ILogger<ReportCacheRefreshJob> _logger;

    public ReportCacheRefreshJob(
        IDapperConnectionFactory db,
        IReportingService reporting,
        IReportCache cache,
        ILogger<ReportCacheRefreshJob> logger)
    {
        _db = db;
        _reporting = reporting;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bat dau lam moi cache bao cao...");

        var tenants = await GetActiveTenantIdsAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        // tenants is IReadOnlyList<int>
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var periodKey = $"MONTH_{monthStart:yyyy-MM-dd}_{today:yyyy-MM-dd}";

        foreach (int tenantId in tenants)
        {
            try
            {
                // 1. Revenue cache
                var revenue = await _reporting.GetRevenueReportAsync(tenantId, "DAY", monthStart, today, null, ct);
                await _cache.SetAsync("diab_his_rep_daily_revenue_cache", tenantId, periodKey,
                    JsonSerializer.Serialize(revenue), ct);

                // 2. Doctor KPI cache
                var kpi = await _reporting.GetDoctorKpiAsync(tenantId, monthStart, today, 50, ct);
                await _cache.SetAsync("diab_his_rep_doctor_kpi_cache", tenantId, periodKey,
                    JsonSerializer.Serialize(kpi), ct);

                // 3. Top drugs cache
                var drugs = await _reporting.GetTopDrugsAsync(tenantId, monthStart, today, 50, ct);
                await _cache.SetAsync("diab_his_rep_top_drugs_cache", tenantId, periodKey,
                    JsonSerializer.Serialize(drugs), ct);

                // 4. Diabetes cohort cache
                var cohort = await _reporting.GetDiabetesCohortAsync(tenantId, monthStart, today, ct);
                await _cache.SetAsync("diab_his_rep_diabetes_cohort_cache", tenantId, periodKey,
                    JsonSerializer.Serialize(cohort), ct);

                // 5. Inventory value cache — stub (real impl queries his_inventory_items)
                await _cache.SetAsync("diab_his_rep_inventory_value_cache", tenantId, periodKey,
                    JsonSerializer.Serialize(new { refreshed_at = DateTime.UtcNow }), ct);

                _logger.LogInformation("Da lam moi cache cho tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi lam moi cache cho tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Hoan thanh lam moi cache bao cao.");
    }

    private async Task<IReadOnlyList<int>> GetActiveTenantIdsAsync(CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var ids = await conn.QueryAsync<int>(
            "SELECT id FROM his_tenants WHERE status = 'ACTIVE'");
        return ids.ToList();
    }
}
