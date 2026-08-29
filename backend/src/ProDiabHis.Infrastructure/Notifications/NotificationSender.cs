using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Facade dieu phoi gui thong bao: resolve config per-tenant/branch qua
/// <see cref="INotificationChannelCredentialProvider"/> roi route toi <see cref="IChannelSender"/>
/// tuong ung (SMS / Zalo ZNS). Moi lan gui deu doc lai config -> doi/reset credential qua UI
/// co hieu luc ngay.
/// </summary>
public class NotificationSender : INotificationSender
{
    private readonly INotificationChannelCredentialProvider _credentials;
    private readonly IReadOnlyDictionary<NotificationChannel, IChannelSender> _senders;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(
        INotificationChannelCredentialProvider credentials,
        IEnumerable<IChannelSender> senders,
        ILogger<NotificationSender> logger)
    {
        _credentials = credentials;
        _senders = senders.ToDictionary(s => s.Channel);
        _logger = logger;
    }

    public async Task<Result<NotificationSendResult>> SendAsync(
        NotificationChannel channel, string recipientPhone, string templateCode,
        IDictionary<string, string> templateData, CancellationToken ct = default)
    {
        var config = await _credentials.GetForCurrentAsync(channel, ct);
        return await SendWithConfigAsync(config, channel, recipientPhone, templateCode, templateData, ct);
    }

    public async Task<Result<NotificationSendResult>> SendForTenantAsync(
        int tenantId, int? branchId, NotificationChannel channel, string recipientPhone,
        string templateCode, IDictionary<string, string> templateData, CancellationToken ct = default)
    {
        var config = await _credentials.GetAsync(tenantId, branchId, channel, ct);
        return await SendWithConfigAsync(config, channel, recipientPhone, templateCode, templateData, ct);
    }

    public async Task<Result<bool>> TestConnectionAsync(NotificationChannel channel, CancellationToken ct = default)
    {
        if (!_senders.TryGetValue(channel, out var sender))
            return Result<bool>.Failure("NOTIFICATION_CHANNEL_UNSUPPORTED", "Kênh gửi không được hỗ trợ.");

        var config = await _credentials.GetForCurrentAsync(channel, ct);
        if (config is null)
            return Result<bool>.Failure("NOTIFICATION_CHANNEL_NOT_CONFIGURED",
                "Kênh chưa được cấu hình hoặc đang tắt. Vui lòng lưu cấu hình trước khi test.");

        return await sender.TestConnectionAsync(config, ct);
    }

    private async Task<Result<NotificationSendResult>> SendWithConfigAsync(
        NotificationChannelConfig? config, NotificationChannel channel, string recipientPhone,
        string templateCode, IDictionary<string, string> templateData, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recipientPhone))
            return Result<NotificationSendResult>.Failure("NOTIFICATION_RECIPIENT_INVALID", "Thiếu số điện thoại người nhận.");
        if (!_senders.TryGetValue(channel, out var sender))
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CHANNEL_UNSUPPORTED", "Kênh gửi không được hỗ trợ.");
        if (config is null)
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CHANNEL_NOT_CONFIGURED",
                "Kênh chưa được cấu hình hoặc đang tắt.");

        return await sender.SendAsync(config, recipientPhone, templateCode, templateData, ct);
    }
}
