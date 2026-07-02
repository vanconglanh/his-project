using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — check encounters IN_PROGRESS > 12h.
/// Runs every 10 minutes: cron */10 * * * *
/// </summary>
public class EncounterOver12hAlertJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<EncounterOver12hAlertJob> _logger;

    public EncounterOver12hAlertJob(IDapperConnectionFactory db, ILogger<EncounterOver12hAlertJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("EncounterOver12hAlertJob started at {Time}", DateTime.UtcNow);

        using var conn = _db.CreateConnection();

        var encounters = await conn.QueryAsync<dynamic>(@"
            SELECT e.id, e.tenant_id, e.patient_id, e.doctor_id, e.started_at,
                   p.full_name AS patient_name
            FROM cli_visits e
            LEFT JOIN pat_patients p ON p.id=e.patient_id
            WHERE e.status='IN_PROGRESS'
              AND e.started_at < DATE_SUB(UTC_TIMESTAMP(), INTERVAL 12 HOUR)
              AND e.alert_sent_at IS NULL
              AND e.deleted_at IS NULL");

        var encList = encounters.ToList();
        _logger.LogInformation("Found {Count} encounters over 12h", encList.Count);

        foreach (var enc in encList)
        {
            try
            {
                var now = DateTime.UtcNow;
                var tenantId = (int)enc.tenant_id;
                var encId = (string)enc.id;

                // Notify doctor
                if (!string.IsNullOrEmpty((string?)enc.doctor_id))
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_nti_notifications
                            (id, tenant_id, recipient_id, type, title, body, ref_type, ref_id, created_at, updated_at)
                        VALUES (UUID(), @TId, @RecId, 'ENCOUNTER_OVER_12H',
                                'Cảnh báo: Lượt khám quá 12 giờ',
                                @Body, 'Encounter', @RefId, @Now, @Now)",
                        new
                        {
                            TId = tenantId,
                            RecId = (string)enc.doctor_id,
                            Body = $"Bệnh nhân {(string?)enc.patient_name ?? "N/A"} đã khám hơn 12 giờ (TT46/2018/TT-BYT).",
                            RefId = encId,
                            Now = now
                        });
                }

                // Mark alert sent
                await conn.ExecuteAsync(
                    "UPDATE cli_visits SET alert_sent_at=@Now WHERE id=@Id",
                    new { Id = encId, Now = now });

                _logger.LogInformation("Alert sent for encounter {EncId}", encId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert for encounter {EncId}", (string)enc.id);
            }
        }

        _logger.LogInformation("EncounterOver12hAlertJob finished. Processed {Count}", encList.Count);
    }
}
