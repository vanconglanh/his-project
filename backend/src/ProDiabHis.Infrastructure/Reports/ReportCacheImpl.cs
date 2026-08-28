using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Infrastructure.Reports;

public class ReportCacheImpl : IReportCache
{
    private readonly IDapperConnectionFactory _db;

    public ReportCacheImpl(IDapperConnectionFactory db) => _db = db;

    public async Task<string?> GetAsync(string tableName, int tenantId, string periodKey, int? branchId = null, CancellationToken ct = default)
    {
        // Whitelist table names to prevent SQL injection
        if (!AllowedTables.Contains(tableName))
            throw new ArgumentException($"Bang cache khong hop le: {tableName}");

        using var conn = _db.CreateConnection();
        // branch_id la 1 phan cua khoa (xem migration 9087) — NULL nghia la cache dung chung toan tenant.
        var sql = $@"SELECT data_json FROM `{tableName}`
                     WHERE tenant_id = @tid AND period_key = @key
                       AND (branch_id <=> @branchId)
                     LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<string>(sql, new { tid = tenantId, key = periodKey, branchId });
    }

    public async Task SetAsync(string tableName, int tenantId, string periodKey, string dataJson, int? branchId = null, CancellationToken ct = default)
    {
        if (!AllowedTables.Contains(tableName))
            throw new ArgumentException($"Bang cache khong hop le: {tableName}");

        using var conn = _db.CreateConnection();
        // Khong dung ON DUPLICATE KEY don gian vi UNIQUE moi la (tenant_id, branch_id, period_key)
        // va branch_id co the NULL (MySQL coi nhieu NULL la khac nhau trong unique index) -> upsert thu cong.
        var existing = await conn.ExecuteScalarAsync<string?>(
            $"SELECT id FROM `{tableName}` WHERE tenant_id=@tid AND period_key=@key AND (branch_id <=> @branchId) LIMIT 1",
            new { tid = tenantId, key = periodKey, branchId });

        if (existing != null)
        {
            await conn.ExecuteAsync(
                $"UPDATE `{tableName}` SET data_json=@json, refreshed_at=NOW(3), updated_at=NOW(3) WHERE id=@id",
                new { id = existing, json = dataJson });
        }
        else
        {
            await conn.ExecuteAsync(
                $@"INSERT INTO `{tableName}` (id, tenant_id, branch_id, period_key, data_json, refreshed_at)
                   VALUES (UUID(), @tid, @branchId, @key, @json, NOW(3))",
                new { tid = tenantId, branchId, key = periodKey, json = dataJson });
        }
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
