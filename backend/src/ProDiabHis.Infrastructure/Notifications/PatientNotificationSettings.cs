using Microsoft.Extensions.Configuration;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Cong tat tong cho cac job nhac benh nhan (recall, lich hen, uong thuoc, sap den luot).
/// Cau hinh: "PatientNotifications:Enabled" (env: PatientNotifications__Enabled).
///
/// MAC DINH BAT (true) khi key khong co hoac khong parse duoc -> giu nguyen hanh vi staging/dev.
/// Production dat PatientNotifications__Enabled=false toi khi go-live de tranh nhan trung/nhan
/// benh nhan that trong khi phong kham van chay tren SUNS va HIS chua go-live.
///
/// Khi tat: cac job nhac BO QUA viec gui (log info, khong danh FAILED, khong set reminder_sent_at),
/// nhung recall van duoc sinh cho nhan vien xem (ChronicCareRecallJob khong bi anh huong).
/// </summary>
public static class PatientNotificationSettings
{
    public const string EnabledKey = "PatientNotifications:Enabled";

    public static bool IsEnabled(IConfiguration config)
        => !bool.TryParse(config[EnabledKey], out var enabled) || enabled;
}
