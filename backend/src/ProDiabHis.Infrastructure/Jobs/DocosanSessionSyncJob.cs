using System.Data;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Telehealth;
using ProDiabHis.Application.Telehealth.Integration;
using ProDiabHis.Infrastructure.Integrations.Docosan;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — dong bo trang thai phien telehealth tu Docosan (khong co webhook,
/// xem docs/erd/telehealth-docosan.md muc 7). Cron mac dinh moi 5 phut.
/// Quet cua so [-LookBackHours, +LookAheadHours] quanh gio hien tai, chi cac session
/// his_status IN (PENDING, CONFIRMED) va last_synced_at qua han.
/// </summary>
public class DocosanSessionSyncJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IDocosanClient _client;
    private readonly IEncryptionService _enc;
    private readonly DocosanOptions _opt;
    private readonly ILogger<DocosanSessionSyncJob> _logger;

    public DocosanSessionSyncJob(IDapperConnectionFactory db, IDocosanClient client, IEncryptionService enc,
        DocosanOptions opt, ILogger<DocosanSessionSyncJob> logger)
    { _db = db; _client = client; _enc = enc; _opt = opt; _logger = logger; }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("DocosanSessionSyncJob started at {Time}", DateTime.UtcNow);
        using var conn = (IDbConnection)_db.CreateConnection();

        var from = DateTime.UtcNow.AddHours(-_opt.SyncJob.LookBackHours);
        var to = DateTime.UtcNow.AddHours(_opt.SyncJob.LookAheadHours);
        var staleBefore = DateTime.UtcNow.AddMinutes(-_opt.SyncJob.IntervalMinutes);

        var sessions = (await conn.QueryAsync<dynamic>(@"
            SELECT s.*, m.access_token_enc, m.token_expires_at
            FROM diab_his_tel_sessions s
            LEFT JOIN diab_his_int_docosan_patient_mapping m
                ON m.tenant_id = s.tenant_id AND m.patient_id = s.patient_id AND m.environment = @Env
            WHERE s.deleted_at IS NULL
              AND s.his_status IN ('PENDING','CONFIRMED')
              AND s.scheduled_start BETWEEN @From AND @To
              AND (s.last_synced_at IS NULL OR s.last_synced_at < @StaleBefore)",
            new { Env = _opt.Environment, From = from, To = to, StaleBefore = staleBefore })).ToList();

        _logger.LogInformation("DocosanSessionSyncJob: {Count} phien can dong bo", sessions.Count);

        foreach (var s in sessions)
        {
            try { await SyncOneAsync(conn, s, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocosanSessionSyncJob: loi dong bo session {Id}", (string)s.id);
            }
        }

        _logger.LogInformation("DocosanSessionSyncJob finished");
    }

    private async Task SyncOneAsync(IDbConnection conn, dynamic s, CancellationToken ct)
    {
        string sessionId = (string)s.id;
        int appointmentId = (int)s.docosan_appointment_id;
        var now = DateTime.UtcNow;

        if (s.access_token_enc is null)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_tel_sessions SET last_synced_at=@Now, sync_error=@Err WHERE id=@Id",
                new { Now = now, Err = "Benh nhan chua lien ket tai khoan Docosan", Id = sessionId });
            return;
        }

        string patientToken;
        try { patientToken = _enc.Decrypt(Encoding.UTF8.GetString((byte[])s.access_token_enc)); }
        catch
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_tel_sessions SET last_synced_at=@Now, sync_error='Loi giai ma token' WHERE id=@Id",
                new { Now = now, Id = sessionId });
            return;
        }

        var apt = await _client.GetAppointmentDetailAsync(appointmentId, patientToken, ct);

        if (!apt.Success)
        {
            // 401/loi -> khong danh dau FAILED ngay, chi tang sync_error, cho lan sau
            await conn.ExecuteAsync(
                "UPDATE diab_his_tel_sessions SET last_synced_at=@Now, sync_error=@Err WHERE id=@Id",
                new { Now = now, Err = apt.ErrorCode, Id = sessionId });
            return;
        }

        var newHisStatus = MapHisStatusFromSync((string)s.his_status, apt.Status, (DateTime)s.scheduled_start, now);
        var changed = newHisStatus != (string)s.his_status || apt.Status != (string)s.docosan_status;

        byte[]? joinUrlEnc = s.join_url_enc;
        DateTime? joinUrlExpiresAt = s.join_url_expires_at;
        if (!string.IsNullOrWhiteSpace(apt.AppointmentLink))
        {
            joinUrlEnc = Encoding.UTF8.GetBytes(_enc.Encrypt(apt.AppointmentLink));
            joinUrlExpiresAt = now.AddMinutes(120);
        }

        await conn.ExecuteAsync(@"
            UPDATE diab_his_tel_sessions
            SET docosan_status=@DoStatus, his_status=@HisStatus, docosan_telemedicine_id=@TeleId,
                join_url_enc=@Join, join_url_expires_at=@JoinExp, payment_status=@Pay,
                last_synced_at=@Now, sync_error=NULL, updated_at=@Now
            WHERE id=@Id",
            new
            {
                DoStatus = apt.Status ?? (string)s.docosan_status, HisStatus = newHisStatus,
                TeleId = apt.TeleMedicineId ?? (int?)s.docosan_telemedicine_id,
                Join = joinUrlEnc, JoinExp = joinUrlExpiresAt, Pay = apt.PaymentStatus ?? (string?)s.payment_status,
                Now = now, Id = sessionId
            });

        if (changed)
        {
            // Dong bo nguoc lich HIS + thong bao (best-effort, khong chan job neu that bai)
            try
            {
                var apptId = (string?)s.appointment_id;
                if (!string.IsNullOrEmpty(apptId))
                {
                    var appointmentHisStatus = newHisStatus switch
                    {
                        "CONFIRMED" => "CONFIRMED",
                        "CANCELLED" => "CANCELLED",
                        "COMPLETED" => "COMPLETED",
                        "NO_SHOW" => "NO_SHOW",
                        _ => "SCHEDULED"
                    };
                    await conn.ExecuteAsync(
                        "UPDATE diab_his_sch_appointments SET status=@St, updated_at=@Now WHERE id=@Id",
                        new { St = appointmentHisStatus, Now = now, Id = apptId });
                }

                var doctorId = (string?)s.doctor_user_id;
                if (!string.IsNullOrEmpty(doctorId))
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_nti_notifications
                            (id, tenant_id, recipient_id, type, title, body, ref_type, ref_id, created_at, updated_at)
                        VALUES (UUID(), @TId, @RecId, 'TELEHEALTH_SESSION_STATUS_CHANGED',
                                'Cập nhật phiên tư vấn từ xa',
                                @Body, 'TelehealthSession', @RefId, @Now, @Now)",
                        new
                        {
                            TId = (int)s.tenant_id, RecId = doctorId,
                            Body = $"Phiên tư vấn từ xa đã chuyển trạng thái: {newHisStatus}",
                            RefId = sessionId, Now = now
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DocosanSessionSyncJob: khong the dong bo nguoc/thong bao cho session {Id}", sessionId);
            }
        }
    }

    private static string MapHisStatusFromSync(string currentHisStatus, string? docosanStatus, DateTime scheduledStart, DateTime now)
    {
        var mapped = docosanStatus switch
        {
            "approve" => "CONFIRMED",
            "reject" => "CANCELLED",
            "on-hold" => "PENDING",
            "request" => "PENDING",
            _ => currentHisStatus
        };

        // Qua gio hen ma van PENDING/CONFIRMED va chua co encounter -> NO_SHOW (job danh dau)
        if ((mapped == "PENDING" || mapped == "CONFIRMED") && scheduledStart.AddHours(2) < now)
            mapped = "NO_SHOW";

        return mapped;
    }
}
