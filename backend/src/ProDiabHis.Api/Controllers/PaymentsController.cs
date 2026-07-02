using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;
using System.IO;
using System.Text;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/payments
    [HttpGet]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? billing_id,
        [FromQuery] string? method,
        [FromQuery] string? status,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPaymentsQuery(
            Guid.TryParse(billing_id, out var bid) ? bid : null,
            method, status, from_date, to_date,
            page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/payments
    [HttpPost]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePaymentCommand(request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PAYMENT_AMOUNT_INVALID")
                return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/payments/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPaymentQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/payments/{id}/refund
    [HttpPost("{id:guid}/refund")]
    [RequirePermission("payment.refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id, request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/payments/{id}/void
    [HttpPost("{id:guid}/void")]
    [RequirePermission("payment.void")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidPaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VoidPaymentCommand(id, request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/payments/methods
    [HttpGet("methods")]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> ListMethods(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListPaymentMethodsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/payments/qr/generate
    [HttpPost("qr/generate")]
    [RequirePermission("payment_qr.generate")]
    public async Task<IActionResult> GenerateQr([FromBody] QrGenerateApiRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateQrCommand(request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/payments/qr/{qrId}/status
    [HttpGet("qr/{qrId:guid}/status")]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> GetQrStatus(Guid qrId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetQrStatusQuery(qrId), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        if (result.Value!.Status == "EXPIRED") return StatusCode(410, new { error = new { code = "PAYMENT_QR_EXPIRED", message = "QR da het han" } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/payments/qr/webhook/{provider} (PUBLIC)
    [HttpPost("qr/webhook/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> QrWebhook(string provider, CancellationToken ct)
    {
        string payload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? string.Empty;
        var result = await _mediator.Send(new ProcessQrWebhookCommand(provider, payload, signature), ct);
        if (!result.IsSuccess) return Unauthorized(new { error = new { code = "WEBHOOK_INVALID", message = result.ErrorMessage } });
        return Ok(new { message = "ACK" });
    }

    // POST /api/v1/payments/card/charge
    [HttpPost("card/charge")]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> CardCharge([FromBody] CardChargeApiRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CardChargeCommand(request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PAYMENT_GATEWAY_ERROR")
                return StatusCode(402, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return Ok(new { data = result.Value });
    }
}
