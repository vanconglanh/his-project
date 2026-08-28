using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job - FR-1206: quet dinh ky cac subscription "Goi dinh muc tra truoc":
///   1) Sap het han (trong pkg.expiry_remind_days = 7 ngay, mac dinh)
///   2) Sap het dinh muc (bat ky balance nao remaining/total &lt;= 15%)
///   3) Cong no qua han (amount_due > 0 va qua pkg.overdue_alert_days = 30 ngay tu purchase_date)
///   4) Set status='expired' cho subscription qua han (RULE-S4)
/// Ghi thong bao vao diab_his_nti_notifications cho user co role admin/ke_toan/le_tan cua tenant.
/// Chong gui trung bang cac cot *_reminded_at / *_alerted_at tren subscription/balance.
/// Cron de xuat: 15 0 * * * (00:15 hang ngay, gio VN - server chay UTC nen chinh doi trong Program.cs neu can).
/// </summary>
public class PackageAlertJob
{
    // Gia tri fallback neu diab_his_sys_settings chua co dong tuong ung (khong xay ra sau seed 9095)
    private const int DefaultExpiryRemindDays = 7;   // pkg.expiry_remind_days (D8)
    private const int DefaultOverdueAlertDays = 30;  // pkg.overdue_alert_days (D8)
    private const decimal LowBalanceThreshold = 0.15m;

    private readonly IDapperConnectionFactory _db;
    private readonly ISettingsProvider _settings;
    private readonly ILogger<PackageAlertJob> _logger;

    public PackageAlertJob(IDapperConnectionFactory db, ISettingsProvider settings, ILogger<PackageAlertJob> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("PackageAlertJob started at {Time}", DateTime.UtcNow);
        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow;
        var expiryRemindDays = await _settings.GetIntAsync("pkg.expiry_remind_days", DefaultExpiryRemindDays);
        var overdueAlertDays = await _settings.GetIntAsync("pkg.overdue_alert_days", DefaultOverdueAlertDays);

        // 1) RULE-S4: het han
        var expired = await conn.ExecuteAsync(
            @"UPDATE diab_his_pkg_subscriptions SET status='expired', updated_at=UTC_TIMESTAMP()
              WHERE status IN ('active','suspended','exhausted') AND expiry_date < CURDATE() AND deleted_at IS NULL");
        if (expired > 0) _logger.LogInformation("PackageAlertJob: {Count} subscriptions chuyen sang expired", expired);

        // 2) Sap het han
        var expiringSoon = await conn.QueryAsync<dynamic>(
            @"SELECT id, tenant_id, patient_id, subscription_no, package_name_snapshot, expiry_date
              FROM diab_his_pkg_subscriptions
              WHERE status='active' AND deleted_at IS NULL
                AND expiry_date BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL @days DAY)
                AND expiry_reminded_at IS NULL",
            new { days = expiryRemindDays });

        foreach (var s in expiringSoon)
        {
            var subId = (string)s.id;
            var tenantId = (int)s.tenant_id;
            await NotifyStaffAsync(conn, tenantId, "PACKAGE_EXPIRING_SOON",
                "Cảnh báo: Gói định mức sắp hết hạn",
                $"Gói '{(string)s.package_name_snapshot}' (số {(string)s.subscription_no}) sẽ hết hạn ngày {((DateTime)s.expiry_date):dd/MM/yyyy}.",
                "PackageSubscription", subId, now);
            await conn.ExecuteAsync("UPDATE diab_his_pkg_subscriptions SET expiry_reminded_at=@now WHERE id=@subId", new { now, subId });
        }

        // 3) Sap het dinh muc
        var lowBalances = await conn.QueryAsync<dynamic>(
            @"SELECT b.id, b.tenant_id, b.subscription_id, b.item_name, b.remaining_quantity, b.total_quantity,
                     s.subscription_no, s.package_name_snapshot
              FROM diab_his_pkg_entitlement_balances b
              JOIN diab_his_pkg_subscriptions s ON s.id = b.subscription_id
              WHERE s.status='active' AND s.deleted_at IS NULL AND b.deleted_at IS NULL
                AND b.total_quantity > 0 AND (b.remaining_quantity / b.total_quantity) <= @threshold
                AND b.remaining_quantity > 0 AND b.low_alerted_at IS NULL",
            new { threshold = LowBalanceThreshold });

        foreach (var b in lowBalances)
        {
            var balId = (string)b.id;
            var tenantId = (int)b.tenant_id;
            await NotifyStaffAsync(conn, tenantId, "PACKAGE_BALANCE_LOW",
                "Cảnh báo: Định mức gói sắp hết",
                $"Gói '{(string)b.package_name_snapshot}' (số {(string)b.subscription_no}) - hạng mục '{(string)b.item_name}' " +
                $"chỉ còn {(decimal)b.remaining_quantity:0.###}/{(decimal)b.total_quantity:0.###}.",
                "PackageEntitlementBalance", balId, now);
            await conn.ExecuteAsync("UPDATE diab_his_pkg_entitlement_balances SET low_alerted_at=@now WHERE id=@balId", new { now, balId });
        }

        // 4) Cong no qua han (FR-1203/1206 - D9: KHONG khoa, chi canh bao)
        var overdue = await conn.QueryAsync<dynamic>(
            @"SELECT id, tenant_id, subscription_no, package_name_snapshot, amount_due, purchase_date
              FROM diab_his_pkg_subscriptions
              WHERE status IN ('active','suspended') AND deleted_at IS NULL AND amount_due > 0
                AND purchase_date <= DATE_SUB(CURDATE(), INTERVAL @days DAY)
                AND overdue_alerted_at IS NULL",
            new { days = overdueAlertDays });

        foreach (var s in overdue)
        {
            var subId = (string)s.id;
            var tenantId = (int)s.tenant_id;
            await NotifyStaffAsync(conn, tenantId, "PACKAGE_HAS_OUTSTANDING_DEBT",
                "Cảnh báo: Gói định mức còn công nợ quá hạn",
                $"Gói '{(string)s.package_name_snapshot}' (số {(string)s.subscription_no}) còn nợ {(decimal)s.amount_due:N0} VNĐ " +
                $"quá {overdueAlertDays} ngày kể từ ngày mua.",
                "PackageSubscription", subId, now);
            await conn.ExecuteAsync("UPDATE diab_his_pkg_subscriptions SET overdue_alerted_at=@now WHERE id=@subId", new { now, subId });
        }

        _logger.LogInformation("PackageAlertJob finished: expiring={Expiring} low_balance={Low} overdue={Overdue}",
            expiringSoon.Count(), lowBalances.Count(), overdue.Count());
    }

    private static async Task NotifyStaffAsync(System.Data.IDbConnection conn, int tenantId, string type, string title, string body,
        string refType, string refId, DateTime now)
    {
        var recipients = await conn.QueryAsync<string>(
            @"SELECT DISTINCT ur.user_id
              FROM diab_his_sec_user_roles ur
              JOIN diab_his_sec_roles r ON r.id = ur.role_id
              WHERE ur.tenant_id = @tenantId AND r.code IN ('admin', 'ke_toan', 'le_tan')",
            new { tenantId });

        foreach (var userId in recipients)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_nti_notifications
                  (id, tenant_id, recipient_id, type, title, body, ref_type, ref_id, created_at, updated_at)
                  VALUES (UUID(), @TenantId, @RecipientId, @Type, @Title, @Body, @RefType, @RefId, @Now, @Now)",
                new { TenantId = tenantId, RecipientId = userId, Type = type, Title = title, Body = body, RefType = refType, RefId = refId, Now = now });
        }
    }
}
