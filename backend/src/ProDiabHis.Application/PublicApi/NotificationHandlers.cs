using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using System.Text.Json;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Inbox: List
// ============================================================
public record ListNotificationsQuery(Guid UserId, int TenantId, int Page, int PageSize, bool UnreadOnly)
    : IRequest<(List<NotificationResponse> Items, int Total)>;

public class ListNotificationsHandler : IRequestHandler<ListNotificationsQuery, (List<NotificationResponse>, int)>
{
    private readonly IDapperConnectionFactory _db;
    public ListNotificationsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<(List<NotificationResponse>, int)> Handle(ListNotificationsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var unreadFilter = q.UnreadOnly ? "AND read_at IS NULL" : "";
        int offset = (q.Page - 1) * q.PageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_nti_notifications WHERE tenant_id = @TenantId AND user_id = @UserId {unreadFilter}",
            new { q.TenantId, UserId = q.UserId.ToString() });

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, type, title, body, data_json, read_at, created_at
               FROM diab_his_nti_notifications
               WHERE tenant_id = @TenantId AND user_id = @UserId {unreadFilter}
               ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset",
            new { q.TenantId, UserId = q.UserId.ToString(), q.PageSize, Offset = offset });

        var items = rows.Select(r =>
        {
            Guid.TryParse(r.id?.ToString(), out Guid notifId);
            return new NotificationResponse(
                notifId, (string)r.type, (string)r.title, (string)r.body,
                r.data_json != null ? JsonSerializer.Deserialize<object>((string)r.data_json) : null,
                (DateTime?)r.read_at, (DateTime)r.created_at);
        }).ToList();

        return (items, total);
    }
}

// ============================================================
// Inbox: Unread count
// ============================================================
public record GetUnreadCountQuery(Guid UserId, int TenantId) : IRequest<int>;

public class GetUnreadCountHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IDapperConnectionFactory _db;
    public GetUnreadCountHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<int> Handle(GetUnreadCountQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_nti_notifications WHERE tenant_id = @TenantId AND user_id = @UserId AND read_at IS NULL",
            new { q.TenantId, UserId = q.UserId.ToString() });
    }
}

// ============================================================
// Inbox: Mark Read
// ============================================================
public record MarkNotificationReadCommand(Guid Id, Guid UserId, int TenantId) : IRequest;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IDapperConnectionFactory _db;
    public MarkNotificationReadHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(MarkNotificationReadCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE diab_his_nti_notifications SET read_at = UTC_TIMESTAMP()
              WHERE id = @Id AND user_id = @UserId AND tenant_id = @TenantId AND read_at IS NULL",
            new { Id = cmd.Id.ToString(), UserId = cmd.UserId.ToString(), cmd.TenantId });
    }
}

// ============================================================
// Inbox: Mark All Read
// ============================================================
public record MarkAllNotificationsReadCommand(Guid UserId, int TenantId) : IRequest;

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IDapperConnectionFactory _db;
    public MarkAllNotificationsReadHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(MarkAllNotificationsReadCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE diab_his_nti_notifications SET read_at = UTC_TIMESTAMP() WHERE tenant_id = @TenantId AND user_id = @UserId AND read_at IS NULL",
            new { cmd.TenantId, UserId = cmd.UserId.ToString() });
    }
}

// ============================================================
// Inbox: Delete
// ============================================================
public record DeleteNotificationCommand(Guid Id, Guid UserId, int TenantId) : IRequest;

public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand>
{
    private readonly IDapperConnectionFactory _db;
    public DeleteNotificationHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(DeleteNotificationCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM diab_his_nti_notifications WHERE id = @Id AND user_id = @UserId AND tenant_id = @TenantId",
            new { Id = cmd.Id.ToString(), UserId = cmd.UserId.ToString(), cmd.TenantId });
    }
}

// ============================================================
// Web Push: Subscribe
// ============================================================
public record WebPushSubscribeCommand(Guid UserId, int TenantId, WebPushSubscriptionRequest Request) : IRequest;

public class WebPushSubscribeHandler : IRequestHandler<WebPushSubscribeCommand>
{
    private readonly IDapperConnectionFactory _db;
    public WebPushSubscribeHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(WebPushSubscribeCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_web_push_subs
                (id, tenant_id, user_id, endpoint, p256dh_key, auth_key, user_agent, created_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@UserId), @Endpoint, @P256dh, @Auth, @Ua, UTC_TIMESTAMP())
              ON DUPLICATE KEY UPDATE p256dh_key = @P256dh, auth_key = @Auth, user_agent = @Ua",
            new
            {
                Id = Guid.NewGuid().ToString(), cmd.TenantId, UserId = cmd.UserId.ToString(),
                Endpoint = cmd.Request.Endpoint, P256dh = cmd.Request.P256dhKey,
                Auth = cmd.Request.AuthKey, Ua = cmd.Request.UserAgent
            });
    }
}

// ============================================================
// Web Push: Unsubscribe
// ============================================================
public record WebPushUnsubscribeCommand(Guid UserId, int TenantId, string Endpoint) : IRequest;

public class WebPushUnsubscribeHandler : IRequestHandler<WebPushUnsubscribeCommand>
{
    private readonly IDapperConnectionFactory _db;
    public WebPushUnsubscribeHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(WebPushUnsubscribeCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM diab_his_nti_web_push_subs WHERE endpoint = @Endpoint AND user_id = @UserId AND tenant_id = @TenantId",
            new { cmd.Endpoint, UserId = cmd.UserId.ToString(), cmd.TenantId });
    }
}

// ============================================================
// Web Push: Vapid Public Key
// ============================================================
public record GetVapidPublicKeyQuery(int TenantId) : IRequest<string?>;

public class GetVapidPublicKeyHandler : IRequestHandler<GetVapidPublicKeyQuery, string?>
{
    private readonly IVapidKeyService _vapidService;
    public GetVapidPublicKeyHandler(IVapidKeyService vapidService) => _vapidService = vapidService;

    public async Task<string?> Handle(GetVapidPublicKeyQuery q, CancellationToken cancellationToken)
        => await _vapidService.GetPublicKeyAsync(q.TenantId, cancellationToken);
}

// ============================================================
// Web Push: Vapid Status
// ============================================================
public record GetVapidStatusQuery(int TenantId) : IRequest<VapidStatusResponse>;

public class GetVapidStatusHandler : IRequestHandler<GetVapidStatusQuery, VapidStatusResponse>
{
    private readonly IVapidKeyService _vapidService;
    public GetVapidStatusHandler(IVapidKeyService vapidService) => _vapidService = vapidService;

    public async Task<VapidStatusResponse> Handle(GetVapidStatusQuery q, CancellationToken cancellationToken)
    {
        var status = await _vapidService.GetStatusAsync(q.TenantId, cancellationToken);
        return new VapidStatusResponse(status.Configured, status.PublicKey, status.GeneratedAt);
    }
}

// ============================================================
// Web Push: Vapid Generate (regenerate)
// ============================================================
public record GenerateVapidKeyCommand(int TenantId) : IRequest<VapidGenerateResponse>;

public class GenerateVapidKeyHandler : IRequestHandler<GenerateVapidKeyCommand, VapidGenerateResponse>
{
    private readonly IVapidKeyService _vapidService;
    public GenerateVapidKeyHandler(IVapidKeyService vapidService) => _vapidService = vapidService;

    public async Task<VapidGenerateResponse> Handle(GenerateVapidKeyCommand cmd, CancellationToken cancellationToken)
    {
        var pair = await _vapidService.RegenerateAsync(cmd.TenantId, cancellationToken);
        return new VapidGenerateResponse(pair.PublicKey, DateTime.UtcNow);
    }
}

// ============================================================
// Preferences: Get
// ============================================================
public record GetPreferencesQuery(Guid UserId, int TenantId) : IRequest<NotificationPreferenceResponse>;

public class GetPreferencesHandler : IRequestHandler<GetPreferencesQuery, NotificationPreferenceResponse>
{
    private readonly IDapperConnectionFactory _db;
    public GetPreferencesHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<NotificationPreferenceResponse> Handle(GetPreferencesQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT position, sound_enabled, sound_name, browser_push_enabled, types_disabled, updated_at FROM diab_his_nti_preferences WHERE tenant_id = @TenantId AND user_id = @UserId",
            new { q.TenantId, UserId = q.UserId.ToString() });

        if (row == null)
            return new NotificationPreferenceResponse("TOP_RIGHT", true, "default", false, new(), DateTime.UtcNow);

        var typesDisabled = row.types_disabled != null
            ? JsonSerializer.Deserialize<List<string>>((string)row.types_disabled) ?? new()
            : new List<string>();

        return new NotificationPreferenceResponse(
            (string)row.position, (bool)row.sound_enabled, (string)row.sound_name,
            (bool)row.browser_push_enabled, typesDisabled, (DateTime)row.updated_at);
    }
}

// ============================================================
// Preferences: Update
// ============================================================
public record UpdatePreferencesCommand(Guid UserId, int TenantId, NotificationPreferenceRequest Request)
    : IRequest<NotificationPreferenceResponse>;

public class UpdatePreferencesHandler : IRequestHandler<UpdatePreferencesCommand, NotificationPreferenceResponse>
{
    private readonly IDapperConnectionFactory _db;
    public UpdatePreferencesHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<NotificationPreferenceResponse> Handle(UpdatePreferencesCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var typesJson = JsonSerializer.Serialize(cmd.Request.TypesDisabled ?? new());

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_preferences
                (id, tenant_id, user_id, position, sound_enabled, sound_name, browser_push_enabled, types_disabled, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@UserId), @Position, @SoundEnabled, @SoundName, @BrowserPush, @TypesJson, UTC_TIMESTAMP())
              ON DUPLICATE KEY UPDATE
                position = @Position, sound_enabled = @SoundEnabled, sound_name = @SoundName,
                browser_push_enabled = @BrowserPush, types_disabled = @TypesJson, updated_at = UTC_TIMESTAMP()",
            new
            {
                Id = Guid.NewGuid().ToString(), cmd.TenantId, UserId = cmd.UserId.ToString(),
                Position = cmd.Request.Position, SoundEnabled = cmd.Request.SoundEnabled,
                SoundName = cmd.Request.SoundName, BrowserPush = cmd.Request.BrowserPushEnabled,
                TypesJson = typesJson
            });

        return new NotificationPreferenceResponse(
            cmd.Request.Position, cmd.Request.SoundEnabled, cmd.Request.SoundName,
            cmd.Request.BrowserPushEnabled, cmd.Request.TypesDisabled ?? new(), DateTime.UtcNow);
    }
}

// ============================================================
// Logs: List (admin view all notifications of tenant)
// ============================================================
public record ListNotificationLogsQuery(int TenantId, int Page, int PageSize)
    : IRequest<(List<NotificationResponse> Items, int Total)>;

public class ListNotificationLogsHandler : IRequestHandler<ListNotificationLogsQuery, (List<NotificationResponse>, int)>
{
    private readonly IDapperConnectionFactory _db;
    public ListNotificationLogsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<(List<NotificationResponse>, int)> Handle(ListNotificationLogsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        int offset = (q.Page - 1) * q.PageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_nti_notifications WHERE tenant_id = @TenantId",
            new { q.TenantId });

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT id, type, title, body, data_json, read_at, created_at
              FROM diab_his_nti_notifications
              WHERE tenant_id = @TenantId
              ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset",
            new { q.TenantId, q.PageSize, Offset = offset });

        var items = rows.Select(r =>
        {
            Guid.TryParse(r.id?.ToString(), out Guid notifId);
            return new NotificationResponse(
                notifId, (string)r.type, (string)r.title, (string)r.body,
                r.data_json != null ? JsonSerializer.Deserialize<object>((string)r.data_json) : null,
                (DateTime?)r.read_at, (DateTime)r.created_at);
        }).ToList();

        return (items, total);
    }
}

// ============================================================
// Test Send: Insert notification record (no real push)
// ============================================================
public record TestSendNotificationCommand(int TenantId, Guid SenderId, TestSendNotificationRequest Request)
    : IRequest<Guid>;

public class TestSendNotificationHandler : IRequestHandler<TestSendNotificationCommand, Guid>
{
    private readonly IDapperConnectionFactory _db;
    public TestSendNotificationHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Guid> Handle(TestSendNotificationCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var id = Guid.NewGuid();
        var targetUserId = string.IsNullOrWhiteSpace(cmd.Request.UserId)
            ? cmd.SenderId.ToString()
            : cmd.Request.UserId;

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_nti_notifications
                (id, tenant_id, user_id, type, title, body, data_json, created_at)
              VALUES (@Id, @TenantId, @UserId, 'TEST', @Title, @Body, NULL, UTC_TIMESTAMP())",
            new { Id = id.ToString(), cmd.TenantId, UserId = targetUserId, cmd.Request.Title, cmd.Request.Body });

        return id;
    }
}
