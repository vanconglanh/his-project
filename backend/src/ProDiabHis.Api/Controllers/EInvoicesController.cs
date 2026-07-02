using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/einvoices")]
[Authorize]
public class EInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public EInvoicesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/einvoices
    [HttpGet]
    [RequirePermission("einvoice.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] string? billing_id,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEInvoicesQuery(
            status, provider,
            Guid.TryParse(billing_id, out var bid) ? bid : null,
            from_date, to_date, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/einvoices/issue
    [HttpPost("issue")]
    [RequirePermission("einvoice.issue")]
    public async Task<IActionResult> Issue([FromBody] IssueEInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new IssueEInvoiceCommand(request), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "EINVOICE_ALREADY_ISSUED" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "EINVOICE_TAX_CODE_MISSING" => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "EINVOICE_PROVIDER_ERROR" => StatusCode(502, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/einvoices/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("einvoice.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEInvoiceQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/einvoices/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("einvoice.cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelEInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelEInvoiceCommand(id, request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { message = "Cancelled" });
    }

    // GET /api/v1/einvoices/{id}/xml-download
    [HttpGet("{id:guid}/xml-download")]
    [RequirePermission("einvoice.read")]
    public async Task<IActionResult> XmlDownload(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DownloadEInvoiceXmlQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/xml", $"einvoice-{id}.xml");
    }
}
