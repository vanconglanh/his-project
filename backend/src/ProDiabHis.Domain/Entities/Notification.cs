using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Thong bao in-app. Map bang diab_his_nti_notifications</summary>
public class Notification : BaseEntity
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>Dang ky Web Push subscription. Map bang diab_his_nti_web_push_subs</summary>
public class WebPushSubscription : BaseEntity
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

/// <summary>Cai dat thong bao cua nguoi dung. Map bang diab_his_nti_preferences</summary>
public class NotificationPreference : BaseEntity
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Position { get; set; } = "TOP_RIGHT";
    public bool SoundEnabled { get; set; } = true;
    public string SoundName { get; set; } = "default";
    public bool BrowserPushEnabled { get; set; } = false;
    public string? TypesDisabledJson { get; set; }
}

/// <summary>VAPID key per tenant. Map bang diab_his_nti_vapid_keys</summary>
public class VapidKey : BaseEntity
{
    public int TenantId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public byte[] PrivateKeyEncrypted { get; set; } = Array.Empty<byte>();
}
