using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — FR-511 [P1]: quet dinh ky cac LabOrder qua han SLA
/// cam ket voi doi tac lab (ordered_at + LabPartner.sla_days &lt; now) ma van
/// chua co ket qua (status khac 'done'/'cancelled'), ghi log canh bao +
/// tao thong bao cho nhan vien phu trach (ordered_by) va quan ly (KTV_TRUONG/ADMIN
/// duoc thong bao qua kenh chung neu can, hien tai notify nguoi chi dinh).
/// Cron: 0 * * * * (moi gio).
/// </summary>
public class LabOrderOverdueAlertJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<LabOrderOverdueAlertJob> _logger;

    public LabOrderOverdueAlertJob(IDapperConnectionFactory db, ILogger<LabOrderOverdueAlertJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("LabOrderOverdueAlertJob started at {Time}", DateTime.UtcNow);

        using var conn = _db.CreateConnection();

        var overdue = await conn.QueryAsync<dynamic>(@"
            SELECT lo.id, lo.tenant_id, lo.encounter_id, lo.test_name, lo.ordered_at, lo.ordered_by,
                   lp.name AS partner_name, COALESCE(lp.sla_days, 3) AS sla_days
            FROM diab_his_cli_lab_orders lo
            LEFT JOIN diab_his_int_lab_partners lp ON lp.id = lo.lab_partner_id
            WHERE lo.deleted_at IS NULL
              AND lo.lab_partner_id IS NOT NULL
              AND lo.status NOT IN ('done', 'cancelled')
              AND lo.overdue_alert_sent_at IS NULL
              AND DATE_ADD(lo.ordered_at, INTERVAL COALESCE(lp.sla_days, 3) DAY) < UTC_TIMESTAMP()");

        var list = overdue.ToList();
        _logger.LogInformation("LabOrderOverdueAlertJob: found {Count} overdue lab orders", list.Count);

        var now = DateTime.UtcNow;
        foreach (var o in list)
        {
            try
            {
                var orderId = (string)o.id;
                var tenantId = (int)o.tenant_id;
                var orderedBy = (string?)o.ordered_by;

                if (!string.IsNullOrEmpty(orderedBy))
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_nti_notifications
                            (id, tenant_id, recipient_id, type, title, body, ref_type, ref_id, created_at, updated_at)
                        VALUES (UUID(), @TId, @RecId, 'LAB_ORDER_OVERDUE_SLA',
                                'Cảnh báo: Kết quả XN quá hạn cam kết',
                                @Body, 'LabOrder', @RefId, @Now, @Now)",
                        new
                        {
                            TId = tenantId,
                            RecId = orderedBy,
                            Body = $"Xét nghiệm '{(string)o.test_name}' gửi đối tác {(string?)o.partner_name ?? "N/A"} " +
                                   $"đã quá hạn SLA {(int)o.sla_days} ngày (chỉ định lúc {(DateTime)o.ordered_at:dd/MM/yyyy HH:mm}).",
                            RefId = orderId,
                            Now = now
                        });
                }

                await conn.ExecuteAsync(
                    "UPDATE diab_his_cli_lab_orders SET overdue_alert_sent_at=@Now WHERE id=@Id",
                    new { Id = orderId, Now = now });

                _logger.LogWarning("LAB_ORDER_OVERDUE_SLA: order={OrderId} tenant={TenantId} sla_days={Sla}",
                    orderId, tenantId, (int)o.sla_days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LabOrderOverdueAlertJob: failed to process order {OrderId}", (string)o.id);
            }
        }

        _logger.LogInformation("LabOrderOverdueAlertJob finished. Processed {Count}", list.Count);
    }
}
