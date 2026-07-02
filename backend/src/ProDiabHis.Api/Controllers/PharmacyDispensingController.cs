using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Dispensing;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/pharmacy/dispense")]
[Authorize]
public class PharmacyDispensingController : ControllerBase
{
    private readonly IMediator _mediator;
    public PharmacyDispensingController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/pharmacy/dispense/queue
    [HttpGet("queue")]
    [RequirePermission("dispense.queue")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] int? warehouse_id,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDispenseQueueQuery(warehouse_id, q, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // GET /api/v1/pharmacy/dispense/history
    [HttpGet("history")]
    [RequirePermission("dispense.queue")]
    public async Task<IActionResult> History(
        [FromQuery] int? patient_id,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDispenseHistoryQuery(patient_id, from_date, to_date, status, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/pharmacy/dispense/{prescriptionId}
    [HttpPost("{prescriptionId:int}")]
    [RequirePermission("dispense.perform")]
    public async Task<IActionResult> Dispense(int prescriptionId, [FromBody] DispenseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new DispenseCommand(prescriptionId, request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PHARMACY_DISPENSE_DUPLICATE")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode is "PHARMACY_STOCK_INSUFFICIENT" or "PHARMACY_INVALID_FEFO_PICK" or "PHARMACY_BATCH_NOT_FOUND")
                return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Created("", new { data = result.Value });
    }

    // POST /api/v1/pharmacy/dispense/{id}/reject
    [HttpPost("{id}/reject")]
    [RequirePermission("dispense.reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectDispenseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectDispenseCommand(id, request.Reason), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/pharmacy/dispense/{id}/return
    [HttpPost("{id}/return")]
    [RequirePermission("dispense.return")]
    public async Task<IActionResult> Return(string id, [FromBody] ReturnDispenseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReturnDispenseCommand(id, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/pharmacy/dispense/{id}/receipt-pdf
    [HttpGet("{id}/receipt-pdf")]
    [RequirePermission("dispense.queue")]
    public async Task<IActionResult> ReceiptPdf(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDispenseReceiptPdfQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"receipt-{id}.pdf");
    }
}
