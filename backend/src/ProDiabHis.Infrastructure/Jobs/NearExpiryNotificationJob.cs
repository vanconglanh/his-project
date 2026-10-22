using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire cron job: daily check stocks near expiry (< 30 days) -> notify DUOCSI.
/// Cron: 0 7 * * * (7:00 AM daily).
/// </summary>
public class NearExpiryNotificationJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<NearExpiryNotificationJob> _logger;

    public NearExpiryNotificationJob(IDapperConnectionFactory db, ILogger<NearExpiryNotificationJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var nearExpiryStocks = await conn.QueryAsync<dynamic>(
            @"SELECT s.drug_id, COALESCE(NULLIF(d.name_vi, ''), d.name) AS drug_name,
                     s.lot_number AS batch_no, s.exp_date AS expiry_date, s.quantity AS quantity_available, s.tenant_id
              FROM diab_his_pha_stock s
              JOIN diab_his_pha_drugs d ON d.id = s.drug_id
              WHERE s.exp_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY)
                AND s.quantity > 0
              ORDER BY s.exp_date ASC");

        var stockList = nearExpiryStocks.ToList();
        if (!stockList.Any())
        {
            _logger.LogInformation("NearExpiryNotificationJob: no near-expiry stocks found");
            return;
        }

        _logger.LogWarning("NearExpiryNotificationJob: {Count} near-expiry batches found. Notification to DUOCSI role.", stockList.Count);

        // In production: send push notification / email to users with DUOCSI role in each tenant.
        // Sprint 6-7: log only (push notification infra from Sprint 5 can be integrated here)
        foreach (var s in stockList)
        {
            _logger.LogWarning("NEAR_EXPIRY: tenant={TenantId} drug={DrugName} batch={Batch} expiry={Expiry} qty={Qty}",
                (int)s.tenant_id, (string)s.drug_name, (string)s.batch_no, (DateTime)s.expiry_date, (decimal)s.quantity_available);
        }
    }
}
