using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — quet recall (diab_his_cli_followup_recall) PENDING co due_date
/// tu hom nay den +3 ngay, chua gui (notified_at NULL) -> gui thong bao qua PatientNotifyService,
/// danh dau notified_at. Gop luon nhac lich hen T-1 (diab_his_sch_appointments ngay mai).
/// Cron: 0 8 * * * (08:00 hang ngay).
/// </summary>
public class RecallNotifyJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPatientNotifyService _notify;
    private readonly ILogger<RecallNotifyJob> _logger;

    public RecallNotifyJob(IDapperConnectionFactory db, IPatientNotifyService notify, ILogger<RecallNotifyJob> logger)
    {
        _db = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bat dau quet nhac tai kham + nhac lich hen...");
        using var conn = (IDbConnection)_db.CreateConnection();

        var tenants = (await conn.QueryAsync<int>(
            "SELECT id FROM diab_his_sys_tenants WHERE status = 'ACTIVE' AND deleted_at IS NULL")).ToList();

        foreach (var tenantId in tenants)
        {
            try
            {
                await ProcessRecallsAsync(conn, tenantId, ct);
                await ProcessAppointmentRemindersAsync(conn, tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi quet nhac cho tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Hoan thanh quet nhac tai kham + lich hen.");
    }

    private async Task ProcessRecallsAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var recalls = (await conn.QueryAsync<(string id, string patient_id, string recall_type, DateTime due_date)>(
            @"SELECT id, patient_id, recall_type, due_date
              FROM diab_his_cli_followup_recall
              WHERE tenant_id = @tenantId AND status = 'PENDING' AND notified_at IS NULL
                AND due_date BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 3 DAY)
                AND deleted_at IS NULL",
            new { tenantId })).ToList();

        foreach (var r in recalls)
        {
            var title = "Nhắc lịch tái khám";
            var body = $"Bạn có lịch tái khám dự kiến vào ngày {r.due_date:dd/MM/yyyy}. Vui lòng đặt lịch hẹn sớm.";

            try
            {
                await _notify.NotifyAsync(Guid.Parse(r.patient_id), tenantId, "RECALL", title, body, null, ct);
                await conn.ExecuteAsync(
                    @"UPDATE diab_his_cli_followup_recall
                      SET notified_at = UTC_TIMESTAMP(), notify_status = 'SENT'
                      WHERE id = @Id",
                    new { Id = r.id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gui nhac recall {RecallId} that bai", r.id);
                await conn.ExecuteAsync(
                    "UPDATE diab_his_cli_followup_recall SET notify_status = 'FAILED' WHERE id = @Id",
                    new { Id = r.id });
            }
        }
    }

    private async Task ProcessAppointmentRemindersAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var appts = (await conn.QueryAsync<(string patient_ref, DateTime appointment_at)>(
            @"SELECT patient_ref, appointment_at
              FROM diab_his_sch_appointments
              WHERE tenant_id = @tenantId AND status IN ('PENDING','CONFIRMED')
                AND DATE(appointment_at) = DATE_ADD(CURDATE(), INTERVAL 1 DAY)
                AND deleted_at IS NULL",
            new { tenantId })).ToList();

        foreach (var a in appts)
        {
            var title = "Nhắc lịch hẹn khám";
            var body = $"Bạn có lịch hẹn khám vào lúc {a.appointment_at:HH:mm dd/MM/yyyy}. Vui lòng đến đúng giờ.";

            try
            {
                await _notify.NotifyAsync(Guid.Parse(a.patient_ref), tenantId, "APPOINTMENT_REMINDER", title, body, null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gui nhac lich hen cho benh nhan {PatientRef} that bai", a.patient_ref);
            }
        }
    }
}
