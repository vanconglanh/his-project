using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.LabIntegration;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class LabIntegrationController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabIntegrationController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/lab-integration/outbound/send/{lab_order_id}
    [HttpPost("api/v1/lab-integration/outbound/send/{labOrderId:guid}")]
    [RequirePermission("lab_integration.send")]
    public async Task<IActionResult> Send(
        Guid labOrderId,
        [FromBody] SendToPartnerRequest body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SendToPartnerCommand(labOrderId, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_ORDER_NOT_FOUND" ? 404 :
                       result.ErrorCode == "LAB_PARTNER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return StatusCode(202, new { data = result.Value });
    }

    // GET /api/v1/lab-integration/outbound
    [HttpGet("api/v1/lab-integration/outbound")]
    [RequirePermission("lab_integration.send")]
    public async Task<IActionResult> ListOutbound(
        [FromQuery] string? status,
        [FromQuery] Guid? lab_partner_id,
        [FromQuery] DateTime? from_date,
        [FromQuery] DateTime? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListOutboundQuery(
            status, lab_partner_id, from_date, to_date, page, page_size), ct);
        var (items, total) = result.Value!;
        return Ok(new { data = items, meta = new { page, page_size, total } });
    }

    // POST /api/v1/lab-integration/outbound/{id}/retry
    [HttpPost("api/v1/lab-integration/outbound/{id:guid}/retry")]
    [RequirePermission("lab_integration.retry")]
    public async Task<IActionResult> RetryOutbound(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RetryOutboundCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_INTEGRATION_RETRY_EXCEEDED" ? 409 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return StatusCode(202);
    }

    // GET /api/v1/lab-integration/inbound
    [HttpGet("api/v1/lab-integration/inbound")]
    [RequirePermission("lab_integration.send")]
    public async Task<IActionResult> ListInbound(
        [FromQuery] string? status,
        [FromQuery] Guid? lab_partner_id,
        [FromQuery] DateTime? from_date,
        [FromQuery] DateTime? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListInboundQuery(
            status, lab_partner_id, from_date, to_date, page, page_size), ct);
        var (items, total) = result.Value!;
        return Ok(new { data = items, meta = new { page, page_size, total } });
    }

    // POST /api/v1/lab-integration/inbound/{id}/reprocess
    [HttpPost("api/v1/lab-integration/inbound/{id:guid}/reprocess")]
    [RequirePermission("lab_integration.retry")]
    public async Task<IActionResult> ReprocessInbound(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReprocessInboundCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_INBOUND_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return StatusCode(202);
    }

    // GET /api/v1/lab-integration/inbound/{id}/raw
    [HttpGet("api/v1/lab-integration/inbound/{id:guid}/raw")]
    [RequirePermission("lab_integration.send")]
    public async Task<IActionResult> GetRaw(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInboundRawQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));

        var (payload, hl7, headers) = result.Value!;
        return Ok(new { data = new { payload_json = payload, raw_hl7_message = hl7, headers } });
    }

    // GET /api/v1/lab-integration/stats
    [HttpGet("api/v1/lab-integration/stats")]
    [RequirePermission("lab_integration.send")]
    public async Task<IActionResult> Stats(
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetIntegrationStatsQuery(days), ct);
        return Ok(new { data = result.Value });
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message } };
}

// ─────────────────────────────────────────────────
// Webhook controller — PUBLIC (khong Authorize)
// ─────────────────────────────────────────────────
[ApiController]
public class LabWebhookController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabWebhookController(IMediator mediator) => _mediator = mediator;

    // POST /api/public/v1/lab-results/webhook/{partnerCode}
    [HttpPost("api/public/v1/lab-results/webhook/{partnerCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> Inbound(
        string partnerCode,
        [FromHeader(Name = "X-Partner-Api-Key")] string? apiKey,
        [FromHeader(Name = "X-Partner-Signature")] string? signature,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(apiKey))
            return Unauthorized(new { error = new { code = "LAB_WEBHOOK_INVALID_SIGNATURE", message = "X-Partner-Api-Key thiếu" } });
        if (string.IsNullOrEmpty(signature))
            return Unauthorized(new { error = new { code = "LAB_WEBHOOK_INVALID_SIGNATURE", message = "X-Partner-Signature thiếu" } });

        // Doc raw body
        byte[] rawBody;
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        rawBody = ms.ToArray();

        object? parsedPayload = null;
        string? rawHl7         = null;
        var contentType        = Request.ContentType ?? "";

        if (contentType.Contains("hl7-v2"))
        {
            rawHl7 = System.Text.Encoding.UTF8.GetString(rawBody);
        }
        else
        {
            try
            {
                parsedPayload = System.Text.Json.JsonSerializer.Deserialize<object>(rawBody);
            }
            catch
            {
                return BadRequest(new { error = new { code = "LAB_IMPORT_PARSE_ERROR", message = "Không thể parse JSON body" } });
            }
        }

        var result = await _mediator.Send(new WebhookInboundCommand(
            partnerCode, apiKey, signature, rawBody, contentType, parsedPayload, rawHl7), ct);

        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_WEBHOOK_INVALID_SIGNATURE" ? 401 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        var (inboundId, receivedAt) = result.Value!;
        return StatusCode(202, new { data = new { inbound_id = inboundId, received_at = receivedAt } });
    }
}
