using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — quet diab_his_ptl_med_reminders enabled=1, con hieu luc
/// (start_date..end_date bao trum hom nay), remind_time roi vao 30 phut vua qua va
/// last_notified_date chua = hom nay -> gui thong bao + danh dau last_notified_date.
/// Cron: */30 * * * *.
/// </summary>
public class MedReminderJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPatientNotifyService _notify;
    private readonly ILogger<MedReminderJob> _logger;

    public MedReminderJob(IDapperConnectionFactory db, IPatientNotifyService notify, ILogger<MedReminderJob> logger)
    {
        _db = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bat dau quet nhac uong thuoc...");
        using var conn = (IDbConnection)_db.CreateConnection();

        var now = DateTime.Now.TimeOfDay;
        var windowStart = now.Add(TimeSpan.FromMinutes(-30));

        var reminders = (await conn.QueryAsync<(string id, int tenant_id, string patient_id, string drug_name,
                string? dose_label, string time_slot)>(
            @"SELECT id, tenant_id, patient_id, drug_name, dose_label, time_slot
              FROM diab_his_ptl_med_reminders
              WHERE enabled = 1 AND deleted_at IS NULL
                AND start_date <= CURDATE() AND (end_date IS NULL OR end_date >= CURDATE())
                AND (last_notified_date IS NULL OR last_notified_date < CURDATE())
                AND remind_time <= @Now
                AND remind_time > @WindowStart",
            new { Now = now, WindowStart = windowStart })).ToList();

        foreach (var r in reminders)
        {
            var title = "Nhắc uống thuốc";
            var body = $"Đã đến giờ uống {r.drug_name}" + (r.dose_label != null ? $" ({r.dose_label})" : "") + $" — buổi {SlotLabel(r.time_slot)}.";

            try
            {
                await _notify.NotifyAsync(Guid.Parse(r.patient_id), r.tenant_id, "MED_REMINDER", title, body, null, ct);
                await conn.ExecuteAsync(
                    "UPDATE diab_his_ptl_med_reminders SET last_notified_date = CURDATE() WHERE id = @Id",
                    new { Id = r.id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gui nhac uong thuoc {ReminderId} that bai", r.id);
            }
        }

        _logger.LogInformation("Hoan thanh quet nhac uong thuoc, da xu ly {Count} nhac", reminders.Count);
    }

    private static string SlotLabel(string slot) => slot switch
    {
        "SANG" => "sáng",
        "TRUA" => "trưa",
        "CHIEU" => "chiều",
        "TOI" => "tối",
        _ => slot
    };
}
