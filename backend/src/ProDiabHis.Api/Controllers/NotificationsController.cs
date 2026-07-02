using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    private Guid UserId => Guid.Parse(User.FindFirst("user_id")!.Value);
    private int TenantId => int.Parse(User.FindFirst("tenant_id")!.Value);

    // -------- Inbox --------
    [HttpGet("inbox")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> ListInbox(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] bool unread_only = false,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _mediator.Send(
            new ListNotificationsQuery(UserId, TenantId, page, page_size, unread_only), cancellationToken);
        return Ok(new { data = items, meta = new { page, total } });
    }

    [HttpGet("unread-count")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new GetUnreadCountQuery(UserId, TenantId), cancellationToken);
        return Ok(new { count });
    }

    [HttpPost("{id:guid}/mark-read")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id, UserId, TenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(UserId, TenantId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteNotificationCommand(id, UserId, TenantId), cancellationToken);
        return NoContent();
    }

    // -------- Web Push --------
    [HttpPost("web-push/subscribe")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> Subscribe(
        [FromBody] WebPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.P256dhKey) ||
            string.IsNullOrWhiteSpace(request.AuthKey))
        {
            return BadRequest(new { error = new { code = "WEB_PUSH_INVALID_SUBSCRIPTION", message = "Subscription khong hop le" } });
        }

        await _mediator.Send(new WebPushSubscribeCommand(UserId, TenantId, request), cancellationToken);
        return StatusCode(201);
    }

    [HttpDelete("web-push/unsubscribe")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string endpoint, CancellationToken cancellationToken)
    {
        await _mediator.Send(new WebPushUnsubscribeCommand(UserId, TenantId, endpoint), cancellationToken);
        return NoContent();
    }

    [HttpGet("vapid/status")]
    [RequirePermission("notification.config")]
    public async Task<IActionResult> GetVapidStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVapidStatusQuery(TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPost("vapid/generate")]
    [RequirePermission("notification.config")]
    public async Task<IActionResult> GenerateVapidKey(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateVapidKeyCommand(TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("web-push/vapid-public-key")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVapidPublicKey([FromQuery] int tenant_id, CancellationToken cancellationToken)
    {
        var key = await _mediator.Send(new GetVapidPublicKeyQuery(tenant_id), cancellationToken);
        if (key == null)
            return NotFound(new { error = new { code = "VAPID_KEY_NOT_CONFIGURED", message = "Tenant chua cau hinh VAPID key" } });
        return Ok(new { public_key = key });
    }

    // -------- Logs (admin) --------
    [HttpGet("logs")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> ListLogs(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _mediator.Send(
            new ListNotificationLogsQuery(TenantId, page, page_size), cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)page_size);
        return Ok(new { data = items, meta = new { page, page_size, total, total_pages = totalPages } });
    }

    [HttpPost("test-send")]
    [RequirePermission("notification.send")]
    public async Task<IActionResult> TestSend(
        [FromBody] TestSendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new TestSendNotificationCommand(TenantId, UserId, request), cancellationToken);
        return StatusCode(201, new { data = new { id } });
    }

    // -------- Preferences --------
    [HttpGet("preferences")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPreferencesQuery(UserId, TenantId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("preferences")]
    [RequirePermission("notification.read")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] NotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdatePreferencesCommand(UserId, TenantId, request), cancellationToken);
        return Ok(result);
    }
}
