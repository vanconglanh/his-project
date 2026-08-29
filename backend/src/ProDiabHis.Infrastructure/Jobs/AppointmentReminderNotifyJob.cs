using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// FR-112 (H-1): Hangfire recurring job — quet lich hen sap toi (trong nguong gio cau hinh duoc,
/// mac dinh 24h) chua duoc nhac (reminder_sent_at IS NULL), gui nhac qua kenh ngoai (Zalo ZNS uu tien,
/// fallback SMS) bang credential per-tenant/branch da cau hinh. Danh dau reminder_sent_at khi gui
/// thanh cong -> chong gui trung (SMS/ZNS ton phi). Neu tenant chua cau hinh kenh nao -> bo qua im lang.
///
/// Nguong gio: config "Notifications:AppointmentReminderHours" (mac dinh 24).
/// Cron dang ky o Program.cs (mac dinh moi gio).
/// </summary>
public class AppointmentReminderNotifyJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly INotificationSender _sender;
    private readonly IConfiguration _config;
    private readonly ILogger<AppointmentReminderNotifyJob> _logger;

    public AppointmentReminderNotifyJob(
        IDapperConnectionFactory db, INotificationSender sender, IConfiguration config,
        ILogger<AppointmentReminderNotifyJob> logger)
    {
        _db = db; _sender = sender; _config = config; _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var hours = int.TryParse(_config["Notifications:AppointmentReminderHours"], out var h) && h > 0 ? h : 24;
        _logger.LogInformation("Bat dau quet nhac lich hen qua SMS/Zalo (nguong {Hours}h)...", hours);

        using var conn = (IDbConnection)_db.CreateConnection();
        var tenants = (await conn.QueryAsync<int>(
            "SELECT id FROM diab_his_sys_tenants WHERE status = 'ACTIVE' AND deleted_at IS NULL")).ToList();

        var totalSent = 0;
        foreach (var tenantId in tenants)
        {
            try { totalSent += await ProcessTenantAsync(conn, tenantId, hours, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Loi quet nhac lich hen cho tenant {TenantId}", tenantId); }
        }

        _logger.LogInformation("Hoan thanh nhac lich hen qua SMS/Zalo. Da gui {Count} nhac.", totalSent);
    }

    private async Task<int> ProcessTenantAsync(IDbConnection conn, int tenantId, int hours, CancellationToken ct)
    {
        var appts = (await conn.QueryAsync<ApptRow>(
            @"SELECT a.id AS Id, a.branch_id AS BranchId, a.appointment_at AS AppointmentAt,
                     COALESCE(pat.full_name, a.patient_name_temp) AS PatientName,
                     COALESCE(pat.phone_enc, a.patient_phone) AS PatientPhone
                FROM diab_his_sch_appointments a
                LEFT JOIN diab_his_pat_patients pat ON pat.id = a.patient_ref AND pat.tenant_id = a.tenant_id
               WHERE a.tenant_id = @tenantId
                 AND a.status IN ('PENDING','CONFIRMED')
                 AND a.deleted_at IS NULL
                 AND a.reminder_sent_at IS NULL
                 AND a.appointment_at BETWEEN NOW() AND DATE_ADD(NOW(), INTERVAL @hours HOUR)",
            new { tenantId, hours })).ToList();

        if (appts.Count == 0) return 0;

        var sent = 0;
        foreach (var a in appts)
        {
            var phone = PiiCrypto.Unprotect(a.PatientPhone);
            if (string.IsNullOrWhiteSpace(phone)) continue;

            var branchId = a.BranchId > 0 ? a.BranchId : (int?)null;
            var message = $"Nhắc lịch hẹn khám: bạn có lịch hẹn lúc {a.AppointmentAt:HH:mm dd/MM/yyyy}. Vui lòng đến đúng giờ.";
            var data = new Dictionary<string, string>
            {
                ["message"] = message,
                ["patient_name"] = a.PatientName ?? "",
                ["time"] = a.AppointmentAt.ToString("HH:mm"),
                ["date"] = a.AppointmentAt.ToString("dd/MM/yyyy")
            };

            // Uu tien Zalo ZNS (re + giau noi dung), fallback SMS. Chi can 1 kenh thanh cong.
            var ok = await TrySendAsync(NotificationChannel.ZaloZns, tenantId, branchId, phone, data, ct)
                  || await TrySendAsync(NotificationChannel.Sms, tenantId, branchId, phone, data, ct);

            if (ok)
            {
                await conn.ExecuteAsync(
                    "UPDATE diab_his_sch_appointments SET reminder_sent_at = NOW() WHERE id = @id",
                    new { id = a.Id });
                sent++;
            }
        }
        return sent;
    }

    private async Task<bool> TrySendAsync(NotificationChannel channel, int tenantId, int? branchId,
        string phone, Dictionary<string, string> data, CancellationToken ct)
    {
        try
        {
            var result = await _sender.SendForTenantAsync(tenantId, branchId, channel, phone,
                "APPOINTMENT_REMINDER", data, ct);
            return result.IsSuccess && result.Value!.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gui nhac lich hen qua {Channel} that bai cho tenant {TenantId}", channel, tenantId);
            return false;
        }
    }

    private sealed class ApptRow
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public DateTime AppointmentAt { get; set; }
        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
    }
}
