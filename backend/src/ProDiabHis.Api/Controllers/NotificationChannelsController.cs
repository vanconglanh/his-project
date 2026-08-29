using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// FR-112 (H-1): Quan ly kenh gui thong bao (SMS / Zalo ZNS) per-tenant/branch.
/// Credential luu ma hoa, doi/reset qua UI khong can deploy lai.
/// </summary>
[ApiController]
[Route("api/v1/notification-channels")]
[Authorize]
public class NotificationChannelsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationChannelsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/notification-channels
    [HttpGet]
    [RequirePermission("notification_channel.read")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListNotificationChannelsQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/notification-channels/{id}
    [HttpGet("{id}")]
    [RequirePermission("notification_channel.read")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNotificationChannelQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/notification-channels
    [HttpPost]
    [RequirePermission("notification_channel.write")]
    public async Task<IActionResult> Create([FromBody] NotificationChannelRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateNotificationChannelCommand(request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/notification-channels/{id}
    [HttpPut("{id}")]
    [RequirePermission("notification_channel.write")]
    public async Task<IActionResult> Update(string id, [FromBody] NotificationChannelRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateNotificationChannelCommand(id, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/notification-channels/{id} — xoa mem = "reset" cau hinh
    [HttpDelete("{id}")]
    [RequirePermission("notification_channel.write")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteNotificationChannelCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = new { deleted = true } });
    }

    // POST /api/v1/notification-channels/{id}/test — nut "Test ket noi"
    [HttpPost("{id}/test")]
    [RequirePermission("notification_channel.write")]
    public async Task<IActionResult> Test(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new TestNotificationChannelCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}
