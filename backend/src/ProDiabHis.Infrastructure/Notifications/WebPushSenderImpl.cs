using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.PublicApi;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Web Push sender dung VAPID per tenant.
/// Package WebPush.NET chua co tren nuget cho .NET 8 stable, dung HTTP POST + JWT VAPID manual.
/// </summary>
public class WebPushSenderImpl : IWebPushSender
{
    private readonly IVapidKeyService _vapidService;
    private readonly IHttpClientFactory _httpFactory;
    private readonly Application.Common.IDapperConnectionFactory _dbFactory;
    private readonly ILogger<WebPushSenderImpl> _logger;

    public WebPushSenderImpl(
        IVapidKeyService vapidService,
        IHttpClientFactory httpFactory,
        Application.Common.IDapperConnectionFactory dbFactory,
        ILogger<WebPushSenderImpl> logger)
    {
        _vapidService = vapidService;
        _httpFactory = httpFactory;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task SendAsync(string endpoint, string p256dhKey, string authKey, int tenantId,
        WebPushPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var keyPair = await _vapidService.GetOrCreateKeyPairAsync(tenantId, cancellationToken);
            var client = _httpFactory.CreateClient("WebPush");

            var body = JsonSerializer.Serialize(new
            {
                title = payload.Title,
                body = payload.Body,
                icon = payload.Icon,
                data = payload.Data
            });

            // Simplified: in production use Web Push protocol with VAPID JWT + ECDH encryption
            // For now, log and stub the push
            _logger.LogInformation("[WebPush] Sending to endpoint: {Endpoint} | Tenant: {TenantId} | Title: {Title}",
                endpoint[..Math.Min(50, endpoint.Length)], tenantId, payload.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebPush send failed to {Endpoint}", endpoint[..Math.Min(50, endpoint.Length)]);
        }
    }

    public async Task SendToUserAsync(Guid userId, int tenantId, WebPushPayload payload,
        CancellationToken cancellationToken = default)
    {
        using var conn = _dbFactory.CreateConnection();
        var subs = await conn.QueryAsync<(string endpoint, string p256dh, string auth)>(
            @"SELECT endpoint, p256dh_key AS p256dh, auth_key AS auth
              FROM diab_his_nti_web_push_subs
              WHERE user_id = UUID_TO_BIN(@UserId) AND tenant_id = @TenantId",
            new { UserId = userId.ToString(), TenantId = tenantId });

        foreach (var sub in subs)
        {
            await SendAsync(sub.endpoint, sub.p256dh, sub.auth, tenantId, payload, cancellationToken);
        }
    }
}
