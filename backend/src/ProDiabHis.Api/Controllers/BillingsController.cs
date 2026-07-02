using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;
using Microsoft.Extensions.Configuration;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/billings")]
[Authorize]
public class BillingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BillingsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/billings
    [HttpGet]
    [RequirePermission("billing.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? patient_id,
        [FromQuery] string? encounter_id,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] string? payer,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListBillingsQuery(
            status,
            Guid.TryParse(patient_id, out var pid) ? pid : null,
            Guid.TryParse(encounter_id, out var eid) ? eid : null,
            from_date, to_date, payer, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/billings
    [HttpPost]
    [RequirePermission("billing.create")]
    public async Task<IActionResult> Create([FromBody] CreateBillingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBillingCommand(request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/billings/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("billing.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBillingQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/billings/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("billing.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBillingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateBillingCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "BILLING_ALREADY_FINALIZED")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/billings/{id}/items
    [HttpPost("{id:guid}/items")]
    [RequirePermission("billing.update")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] BillingItemUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddBillingItemCommand(id, request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return StatusCode(201, new { data = result.Value });
    }

    // DELETE /api/v1/billings/items/{itemId}
    [HttpDelete("items/{itemId:guid}")]
    [RequirePermission("billing.update")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteBillingItemCommand(itemId), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return NoContent();
    }

    // POST /api/v1/billings/{id}/finalize
    [HttpPost("{id:guid}/finalize")]
    [RequirePermission("billing.finalize")]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new FinalizeBillingCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "BILLING_ALREADY_FINALIZED" or "BILLING_VOID")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/billings/{id}/void
    [HttpPost("{id:guid}/void")]
    [RequirePermission("billing.void")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidBillingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VoidBillingCommand(id, request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/billings/{id}/preview
    [HttpGet("{id:guid}/preview")]
    [RequirePermission("billing.read")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBillingQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/billings/{id}/pdf
    [HttpGet("{id:guid}/pdf")]
    [RequirePermission("billing.read")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportBillingPdfQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"billing-{id}.pdf");
    }

    // POST /api/v1/billings/{id}/apply-bhyt
    [HttpPost("{id:guid}/apply-bhyt")]
    [RequirePermission("billing.apply_bhyt")]
    public async Task<IActionResult> ApplyBhyt(Guid id, [FromBody] ApplyBhytRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApplyBhytCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "BILLING_INVALID_BHYT")
                return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/billings/encounter/{encounterId}
    [HttpGet("encounter/{encounterId:guid}")]
    [RequirePermission("billing.read")]
    public async Task<IActionResult> GetByEncounter(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBillingByEncounterQuery(encounterId), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    /// <summary>In hoa don A5 (PDF) + archive vao MinIO + ghi audit log</summary>
    // POST /api/v1/billings/{id}/print
    [HttpPost("{id:guid}/print")]
    [RequirePermission("billing.print")]
    public async Task<IActionResult> Print(Guid id, [FromBody] PrintBillingRequest? request, CancellationToken ct)
    {
        var req = request ?? new PrintBillingRequest();
        var result = await _mediator.Send(new PrintBillingCommand(id, req), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "BILLING_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "BILLING_NOT_FINALIZED" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }

        var value = result.Value!;
        Response.Headers["Content-Disposition"] = $"inline; filename=\"invoice-{value.InvoiceNo}.pdf\"";
        Response.Headers["X-Invoice-Archived-Url"] = value.ArchivedUrl;
        return File(value.PdfBytes, "application/pdf");
    }
}
