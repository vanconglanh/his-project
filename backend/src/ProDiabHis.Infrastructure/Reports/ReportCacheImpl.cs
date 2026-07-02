using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Infrastructure.Reports;

public class ReportCacheImpl : IReportCache
{
    private readonly IDapperConnectionFactory _db;

    public ReportCacheImpl(IDapperConnectionFactory db) => _db = db;

    public async Task<string?> GetAsync(string tableName, int tenantId, string periodKey, CancellationToken ct = default)
    {
        // Whitelist table names to prevent SQL injection
        if (!AllowedTables.Contains(tableName))
            throw new ArgumentException($"Bang cache khong hop le: {tableName}");

        using var conn = _db.CreateConnection();
        var sql = $"SELECT data_json FROM `{tableName}` WHERE tenant_id = @tid AND period_key = @key LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<string>(sql, new { tid = tenantId, key = periodKey });
    }

    public async Task SetAsync(string tableName, int tenantId, string periodKey, string dataJson, CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(tableName))
            throw new ArgumentException($"Bang cache khong hop le: {tableName}");

        using var conn = _db.CreateConnection();
        var sql = $@"
            INSERT INTO `{tableName}` (id, tenant_id, period_key, data_json, refreshed_at)
            VALUES (UUID(), @tid, @key, @json, NOW(3))
            ON DUPLICATE KEY UPDATE data_json = @json, refreshed_at = NOW(3), updated_at = NOW(3)";

        await conn.ExecuteAsync(sql, new { tid = tenantId, key = periodKey, json = dataJson });
    }

    private static readonly HashSet<string> AllowedTables =
    [
        "diab_his_rep_daily_revenue_cache",
        "diab_his_rep_doctor_kpi_cache",
        "diab_his_rep_top_drugs_cache",
        "diab_his_rep_inventory_value_cache",
        "diab_his_rep_diabetes_cohort_cache",
    ];
}
