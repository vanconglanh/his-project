using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Infrastructure.Dapper;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job: doi voi moi notification moi insert, dispatch Web Push
/// toi tat ca subscription cua recipient_user_id.
/// </summary>
public class NotificationDispatcherJob
{
    private readonly IWebPushSender _webPushSender;
    private readonly DapperConnectionFactory _factory;
    private readonly ILogger<NotificationDispatcherJob> _logger;

    public NotificationDispatcherJob(
        IWebPushSender webPushSender,
        Application.Common.IDapperConnectionFactory factory,
        ILogger<NotificationDispatcherJob> logger)
    {
        _webPushSender = webPushSender;
        _factory = (DapperConnectionFactory)factory;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid notificationId)
    {
        using var conn = _factory.CreateConnection();

        var notif = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT BIN_TO_UUID(user_id) AS user_id, tenant_id, title, body, data_json
              FROM diab_his_nti_notifications
              WHERE id = UUID_TO_BIN(@Id)",
            new { Id = notificationId.ToString() });

        if (notif == null)
        {
            _logger.LogWarning("NotificationDispatcherJob: notification {Id} not found", notificationId);
            return;
        }

        var userId = Guid.Parse((string)notif.user_id);
        int tenantId = (int)notif.tenant_id;

        var payload = new WebPushPayload(
            (string)notif.title,
            (string)notif.body,
            null,
            notif.data_json);

        await _webPushSender.SendToUserAsync(userId, tenantId, payload);
        _logger.LogInformation("NotificationDispatcherJob: dispatched push for user {UserId} tenant {TenantId}", userId, tenantId);
    }
}
