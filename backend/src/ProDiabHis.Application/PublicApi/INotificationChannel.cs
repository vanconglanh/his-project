namespace ProDiabHis.Application.PublicApi;

/// <summary>
/// Mot kenh gui thong bao cho benh nhan (portal). Moi kenh (Email, WebPush, ...)
/// implement rieng — PatientNotifyService fan-out theo notify_prefs_json cua benh nhan.
/// </summary>
public interface INotificationChannel
{
    /// <summary>Ma dinh danh kenh: "PUSH" | "EMAIL"</summary>
    string ChannelType { get; }

    /// <summary>Gui thong bao cho 1 benh nhan. Tra ve true neu gui thanh cong (co it nhat 1 dich hop le).</summary>
    Task<bool> SendToPatientAsync(Guid patientId, int tenantId, string title, string body, string? url,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fan-out thong bao cho benh nhan theo tuy chon kenh (notify_prefs_json), fallback push -> email
/// neu push khong gui duoc (chua co subscription). Luon ghi log vao diab_his_nti_notifications
/// (recipient_type = PATIENT).
/// </summary>
public interface IPatientNotifyService
{
    Task NotifyAsync(Guid patientId, int tenantId, string type, string title, string body,
        string? url = null, CancellationToken cancellationToken = default);
}
