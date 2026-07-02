using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Prescriptions;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PrescriptionsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/prescriptions
    [HttpGet]
    [RequirePermission("prescription.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? patient_id,
        [FromQuery] string? encounter_id,
        [FromQuery] string? doctor_id,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPrescriptionsQuery(
            status,
            Guid.TryParse(patient_id, out var pid) ? pid : null,
            Guid.TryParse(encounter_id, out var eid) ? eid : null,
            Guid.TryParse(doctor_id, out var did) ? did : null,
            from_date, to_date, q, page, Math.Min(page_size, 100)), ct);

        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/prescriptions
    [HttpPost]
    [RequirePermission("prescription.create")]
    public async Task<IActionResult> Create([FromBody] PrescriptionCreateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePrescriptionCommand(request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return CreatedAtAction(nameof(GetDetail), new { id = result.Value!.Id }, new { data = result.Value });
    }

    // GET /api/v1/prescriptions/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("prescription.read")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPrescriptionQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/prescriptions/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("prescription.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PrescriptionUpdateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePrescriptionCommand(id, request), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/prescriptions/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("prescription.update")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePrescriptionCommand(id), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // POST /api/v1/prescriptions/{id}/items
    [HttpPost("{id:guid}/items")]
    [RequirePermission("prescription.update")]
    public async Task<IActionResult> AddItems(Guid id, [FromBody] AddPrescriptionItemsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddPrescriptionItemsCommand(id, request.Items), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Created("", new { data = result.Value });
    }

    // DELETE /api/v1/prescriptions/{id}/items/{itemId}
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [RequirePermission("prescription.update")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemovePrescriptionItemCommand(id, itemId), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // POST /api/v1/prescriptions/{id}/sign
    [HttpPost("{id:guid}/sign")]
    [RequirePermission("prescription.sign")]
    public async Task<IActionResult> Sign(Guid id, [FromBody] SignPrescriptionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SignPrescriptionCommand(id, request), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/prescriptions/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("prescription.cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPrescriptionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelPrescriptionCommand(id, request.Reason), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/prescriptions/{id}/ddi-check
    [HttpGet("{id:guid}/ddi-check")]
    [RequirePermission("ddi.check")]
    public async Task<IActionResult> DdiCheck(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckDdiQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/prescriptions/{id}/qr
    [HttpGet("{id:guid}/qr")]
    [RequirePermission("prescription.read")]
    public async Task<IActionResult> GetQr(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPrescriptionQrQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "image/png");
    }

    // GET /api/v1/prescriptions/{id}/pdf
    [HttpGet("{id:guid}/pdf")]
    [RequirePermission("prescription.read")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPrescriptionPdfQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"prescription-{id}.pdf");
    }

    // GET /api/v1/prescriptions/{id}/print-history
    [HttpGet("{id:guid}/print-history")]
    [RequirePermission("prescription.read")]
    public async Task<IActionResult> GetPrintHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPrintHistoryQuery(id), ct);
        return Ok(new { data = result.Value });
    }

    /// <summary>Day don thuoc len Cong DTQG, nhan ma_don_thuoc va QR URL (ADR-0002)</summary>
    // POST /api/v1/prescriptions/{id}/submit-dtqg
    [HttpPost("{id:guid}/submit-dtqg")]
    [RequirePermission("dtqg.submit")]
    public async Task<IActionResult> SubmitDtqg(
        Guid id,
        [FromBody] Application.Pharmacy.Dtqg.SubmitDtqgFromPrescriptionRequest? request,
        CancellationToken ct)
    {
        var req = request ?? new Application.Pharmacy.Dtqg.SubmitDtqgFromPrescriptionRequest();
        var result = await _mediator.Send(
            new Application.Pharmacy.Dtqg.SubmitDtqgFromPrescriptionCommand(id, req), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "PRESCRIPTION_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "PRESCRIPTION_NO_ITEMS" or "PRESCRIPTION_NO_DIAGNOSIS"
                    => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "PRESCRIPTION_ALREADY_SUBMITTED"
                    => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "DTQG_PORTAL_UNAVAILABLE"
                    => StatusCode(502, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }

        var value = result.Value!;
        Response.Headers["X-Dtqg-Mode"] = value.Mode;

        return Ok(new
        {
            data = new
            {
                prescription_id = value.Data.PrescriptionId,
                ma_don_thuoc = value.Data.MaDonThuoc,
                qr_url = value.Data.QrUrl,
                submitted_at = value.Data.SubmittedAt,
                status = value.Data.Status,
                portal_status = value.Data.PortalStatus
            },
            meta = new { mode = value.Mode }
        });
    }

    // POST /api/v1/prescriptions/{id}/dtqg/submit
    [HttpPost("{id:guid}/dtqg/submit")]
    [RequirePermission("dtqg.submit")]
    public async Task<IActionResult> DtqgSubmit(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new Application.Pharmacy.Dtqg.SubmitDtqgCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "DTQG_PRESCRIPTION_ALREADY_SUBMITTED")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode == "PRESCRIPTION_INVALID_STATE")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return StatusCode(424, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/prescriptions/{id}/dtqg/status
    [HttpGet("{id:guid}/dtqg/status")]
    [RequirePermission("dtqg.submit")]
    public async Task<IActionResult> DtqgStatus(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new Application.Pharmacy.Dtqg.GetDtqgStatusQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/prescriptions/{id}/dtqg/retry
    [HttpPost("{id:guid}/dtqg/retry")]
    [RequirePermission("dtqg.retry")]
    public async Task<IActionResult> DtqgRetry(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new Application.Pharmacy.Dtqg.RetryDtqgCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "DTQG_RETRY_EXCEEDED")
                return StatusCode(429, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }
}
