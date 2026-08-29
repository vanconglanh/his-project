using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Notifications;

/// <summary>Loai kenh gui thong bao ngoai (FR-112).</summary>
public enum NotificationChannel
{
    /// <summary>Tin nhan SMS qua nha cung cap trong nuoc (vd eSMS).</summary>
    Sms = 0,
    /// <summary>Zalo Notification Service (ZNS) qua Zalo Official Account.</summary>
    ZaloZns = 1
}

/// <summary>Ket qua gui 1 thong bao.</summary>
/// <param name="Success">Gui thanh cong hay khong.</param>
/// <param name="ProviderMessageId">Ma tham chieu do nha cung cap tra ve (neu co).</param>
/// <param name="RawResponse">Phan hoi tho (da cat ngan) de doi soat / debug.</param>
public record NotificationSendResult(bool Success, string? ProviderMessageId, string? RawResponse);

/// <summary>
/// Dich vu gui thong bao qua kenh ngoai (SMS / Zalo ZNS), doc credential per-tenant/branch
/// da ma hoa tu <c>diab_his_int_notification_channels</c>. Doi/reset credential qua UI khong
/// can deploy lai (moi lan gui deu doc lai config moi nhat tu DB).
/// </summary>
public interface INotificationSender
{
    /// <summary>Gui theo tenant/branch dang dang nhap (dung o luong co HttpContext).</summary>
    Task<Result<NotificationSendResult>> SendAsync(
        NotificationChannel channel, string recipientPhone, string templateCode,
        IDictionary<string, string> templateData, CancellationToken ct = default);

    /// <summary>
    /// Gui cho 1 tenant/branch cu the (dung trong background job khong co HttpContext,
    /// vd nhac lich hen tu dong). branchId null = dung credential dung chung cua tenant.
    /// </summary>
    Task<Result<NotificationSendResult>> SendForTenantAsync(
        int tenantId, int? branchId, NotificationChannel channel, string recipientPhone,
        string templateCode, IDictionary<string, string> templateData, CancellationToken ct = default);

    /// <summary>Kiem tra ket noi cua kenh theo tenant/branch dang dang nhap (nut "Test ket noi").</summary>
    Task<Result<bool>> TestConnectionAsync(NotificationChannel channel, CancellationToken ct = default);
}

/// <summary>
/// Config da giai ma cua 1 kenh thong bao. <see cref="Config"/> la dictionary key-value
/// (api_key, secret, access_token, endpoint, template_id...) lay tu JSON da ma hoa.
/// </summary>
public record NotificationChannelConfig(
    NotificationChannel Channel, string Provider, IReadOnlyDictionary<string, string> Config);

/// <summary>
/// Doc + giai ma config kenh thong bao theo tenant/branch tu <c>diab_his_int_notification_channels</c>
/// (giong pattern <c>IDtqgCredentialProvider</c>). Uu tien dong khop branch hien tai, fallback branch_id NULL.
/// </summary>
public interface INotificationChannelCredentialProvider
{
    /// <summary>Config kenh cua tenant/branch dang dang nhap. Null neu chua cau hinh / khong active.</summary>
    Task<NotificationChannelConfig?> GetForCurrentAsync(NotificationChannel channel, CancellationToken ct = default);

    /// <summary>Config kenh cua 1 tenant/branch cu the (dung trong background job).</summary>
    Task<NotificationChannelConfig?> GetAsync(int tenantId, int? branchId, NotificationChannel channel, CancellationToken ct = default);
}

/// <summary>
/// Adapter gui thuc te cho 1 kenh cu the (SMS hoac Zalo ZNS). Nhan config da giai ma,
/// tu goi API nha cung cap. <see cref="NotificationSender"/> route theo <see cref="Channel"/>.
/// </summary>
public interface IChannelSender
{
    NotificationChannel Channel { get; }

    Task<Result<NotificationSendResult>> SendAsync(
        NotificationChannelConfig config, string recipientPhone, string templateCode,
        IDictionary<string, string> templateData, CancellationToken ct = default);

    Task<Result<bool>> TestConnectionAsync(NotificationChannelConfig config, CancellationToken ct = default);
}
