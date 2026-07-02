namespace ProDiabHis.Application.PublicApi;

public record WebPushPayload(string Title, string Body, string? Icon = null, object? Data = null);

/// <summary>Gui Web Push notification den browser subscription</summary>
public interface IWebPushSender
{
    Task SendAsync(string endpoint, string p256dhKey, string authKey, int tenantId,
        WebPushPayload payload, CancellationToken cancellationToken = default);

    Task SendToUserAsync(Guid userId, int tenantId, WebPushPayload payload,
        CancellationToken cancellationToken = default);
}
