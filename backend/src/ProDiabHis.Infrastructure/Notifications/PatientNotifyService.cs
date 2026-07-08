using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>Kenh gui qua Web Push (dung IWebPushSender.SendToPatientAsync — hien la stub, chi log).</summary>
public class WebPushPatientChannel : INotificationChannel
{
    private readonly IWebPushSender _webPush;
    public WebPushPatientChannel(IWebPushSender webPush) => _webPush = webPush;

    public string ChannelType => "PUSH";

    public Task<bool> SendToPatientAsync(Guid patientId, int tenantId, string title, string body, string? url,
        CancellationToken cancellationToken = default)
        => _webPush.SendToPatientAsync(patientId, tenantId,
            new WebPushPayload(title, body, Data: url != null ? new { url } : null), cancellationToken);
}

/// <summary>Kenh gui qua Email (IEmailSender — hoat dong that qua SMTP). Lay email tu portal_accounts,
/// fallback diab_his_pat_patients.email.</summary>
public class EmailPatientChannel : INotificationChannel
{
    private readonly IEmailSender _email;
    private readonly Application.Common.IDapperConnectionFactory _db;
    private readonly ILogger<EmailPatientChannel> _logger;

    public EmailPatientChannel(IEmailSender email, Application.Common.IDapperConnectionFactory db, ILogger<EmailPatientChannel> logger)
    {
        _email = email;
        _db = db;
        _logger = logger;
    }

    public string ChannelType => "EMAIL";

    public async Task<bool> SendToPatientAsync(Guid patientId, int tenantId, string title, string body, string? url,
        CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var email = await conn.ExecuteScalarAsync<string?>(
            "SELECT email FROM diab_his_pat_portal_accounts WHERE patient_id = @PatientId AND tenant_id = @TenantId",
            new { PatientId = patientId.ToString(), TenantId = tenantId });

        if (string.IsNullOrWhiteSpace(email))
        {
            email = await conn.ExecuteScalarAsync<string?>(
                "SELECT email FROM diab_his_pat_patients WHERE id = @PatientId AND tenant_id = @TenantId",
                new { PatientId = patientId.ToString(), TenantId = tenantId });
        }

        if (string.IsNullOrWhiteSpace(email)) return false;

        var htmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>"
            + (url != null ? $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Xem chi tiết</a></p>" : "");

        try
        {
            await _email.SendAsync(email, title, htmlBody, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gui email thong bao cho benh nhan {PatientId} that bai", patientId);
            return false;
        }
    }
}

/// <summary>
/// Fan-out thong bao cho benh nhan theo notify_prefs_json (mac dinh push+email deu bat).
/// Uu tien push, fallback email neu push khong gui duoc (chua co subscription hoac tat push).
/// Luon ghi vao diab_his_nti_notifications (recipient_type='PATIENT').
/// </summary>
public class PatientNotifyService : IPatientNotifyService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly Application.Common.IDapperConnectionFactory _db;
    private readonly ILogger<PatientNotifyService> _logger;

    public PatientNotifyService(
        IEnumerable<INotificationChannel> channels,
        Application.Common.IDapperConnectionFactory db,
        ILogger<PatientNotifyService> logger)
    {
        _channels = channels;
        _db = db;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid patientId, int tenantId, string type, string title, string body,
        string? url = null, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var prefsJson = await conn.ExecuteScalarAsync<string?>(
            "SELECT notify_prefs_json FROM diab_his_pat_portal_accounts WHERE patient_id = @PatientId AND tenant_id = @TenantId",
            new { PatientId = patientId.ToString(), TenantId = tenantId });

        var prefs = ProDiabHis.Application.PublicApi.GetPortalNotifyPreferencesHandler.ParsePrefs(prefsJson);

        var pushChannel = _channels.FirstOrDefault(c => c.ChannelType == "PUSH");
        var emailChannel = _channels.FirstOrDefault(c => c.ChannelType == "EMAIL");

        bool sent = false;

        if (prefs.Push && pushChannel is not null)
        {
            var channel = pushChannel;
            try
            {
                sent = await channel.SendToPatientAsync(patientId, tenantId, title, body, url, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gui push cho benh nhan {PatientId} loi, se fallback email", patientId);
            }
        }

        if (!sent && prefs.Email && emailChannel is not null)
        {
            var channel = emailChannel;
            try
            {
                sent = await channel.SendToPatientAsync(patientId, tenantId, title, body, url, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gui email cho benh nhan {PatientId} that bai", patientId);
            }
        }

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_notifications
                (id, tenant_id, user_id, patient_id, recipient_type, type, title, body, created_at)
              VALUES (@Id, @TenantId, @PatientId, @PatientId, 'PATIENT', @Type, @Title, @Body, UTC_TIMESTAMP())",
            new
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, PatientId = patientId.ToString(),
                Type = type, Title = title, Body = body
            });

        if (!sent)
            _logger.LogWarning("Khong gui duoc thong bao {Type} cho benh nhan {PatientId} qua bat ky kenh nao", type, patientId);
    }
}
