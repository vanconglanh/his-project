using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Encounters;
using ProDiabHis.Application.Encounters.Addenda;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/encounters")]
[Authorize]
public class EncountersController : ControllerBase
{
    private readonly IMediator _mediator;

    public EncountersController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/encounters
    [HttpGet]
    [RequirePermission("encounter.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? patient_id,
        [FromQuery] string? doctor_id,
        [FromQuery] string? room_id,
        [FromQuery] string? status,
        [FromQuery] string? encounter_type,
        [FromQuery] DateOnly? date_from,
        [FromQuery] DateOnly? date_to,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEncountersQuery(
            patient_id, doctor_id, room_id, status, encounter_type,
            date_from, date_to, page, Math.Min(page_size, 100)), ct);

        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // POST /api/v1/encounters
    [HttpPost]
    [RequirePermission("encounter.create")]
    public async Task<IActionResult> Create([FromBody] CreateEncounterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEncounterCommand(request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = "ENCOUNTER_CREATE_FAILED", message = result.ErrorMessage } });
        return CreatedAtAction(nameof(GetDetail), new { id = result.Value!.Id }, new { data = result.Value });
    }

    // GET /api/v1/encounters/alerts/over-12h   (must be before /{id})
    [HttpGet("alerts/over-12h")]
    [RequirePermission("encounter.read")]
    public async Task<IActionResult> Over12hAlerts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOver12hAlertsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/encounters/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("encounter.read")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEncounterDetailQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = "ENCOUNTER_NOT_FOUND", message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/encounters/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("encounter.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEncounterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEncounterCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails, "ENCOUNTER_NOT_FOUND", 404);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/encounters/{id}/start
    [HttpPost("{id:guid}/start")]
    [RequirePermission("encounter.start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartEncounterCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode ?? "ENCOUNTER_INVALID_TRANSITION";
            return Conflict(new { error = new { code, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/encounters/{id}/close
    [HttpPost("{id:guid}/close")]
    [RequirePermission("encounter.close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseEncounterCommand(id), ct);
        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode is "ENCOUNTER_NOT_FOUND" ? 404 : 422;
            return StatusCode(statusCode, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = new { closed = true } });
    }

    // PUT /api/v1/encounters/{id}/chief-complaint
    [HttpPut("{id:guid}/chief-complaint")]
    [RequirePermission("encounter.update")]
    public async Task<IActionResult> UpdateChiefComplaint(Guid id, [FromBody] UpdateChiefComplaintBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateChiefComplaintCommand(id, body.ChiefComplaint), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails, "ENCOUNTER_NOT_FOUND", 404);
        return Ok();
    }

    // POST /api/v1/encounters/{id}/diagnoses
    [HttpPost("{id:guid}/diagnoses")]
    [RequirePermission("encounter.update")]
    public async Task<IActionResult> AddDiagnosis(Guid id, [FromBody] DiagnosisRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddDiagnosisCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails, result.ErrorCode, 400);
        return StatusCode(201, new { data = result.Value });
    }

    // DELETE /api/v1/encounters/{id}/diagnoses/{diagnosisId}
    [HttpDelete("{id:guid}/diagnoses/{diagnosisId:guid}")]
    [RequirePermission("encounter.update")]
    public async Task<IActionResult> DeleteDiagnosis(Guid id, Guid diagnosisId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveDiagnosisCommand(id, diagnosisId), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails, "DIAGNOSIS_NOT_FOUND", 404);
        return NoContent();
    }

    // GET /api/v1/encounters/{id}/timeline
    [HttpGet("{id:guid}/timeline")]
    [RequirePermission("encounter.read")]
    public async Task<IActionResult> Timeline(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEncounterTimelineQuery(id), ct);
        return Ok(new { data = result.Value });
    }

    // ────────────────────────────────────────────────
    // [G03] Khoa benh an + ban dinh chinh (addendum)
    // ────────────────────────────────────────────────

    /// <summary>Trang thai khoa cua benh an — FE dung de disable form + hien banner.</summary>
    // GET /api/v1/encounters/{id}/lock-state
    [HttpGet("{id:guid}/lock-state")]
    [RequirePermission("encounter.read")]
    public async Task<IActionResult> LockState(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEncounterLockStateQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Tao ban dinh chinh cho benh an da khoa (khong ghi de ban goc).</summary>
    // POST /api/v1/encounters/{id}/addenda
    [HttpPost("{id:guid}/addenda")]
    [RequirePermission("encounter.amend")]
    public async Task<IActionResult> CreateAddendum(Guid id, [FromBody] CreateAddendumRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEncounterAddendumCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "ENCOUNTER_NOT_FOUND"          => 404,
                "ADDENDUM_TARGET_NOT_FOUND"    => 404,
                "FORBIDDEN"                    => 403,
                "BHYT_RESUBMIT_ACK_REQUIRED"   => 409,
                _                              => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = result.ErrorDetails } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Lich su dinh chinh cua benh an.</summary>
    // GET /api/v1/encounters/{id}/addenda
    [HttpGet("{id:guid}/addenda")]
    [RequirePermission("encounter.amend.read")]
    public async Task<IActionResult> ListAddenda(Guid id, [FromQuery] string? section,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEncounterAddendaQuery(id, section, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    /// <summary>Benh an da khoa -> 409 ENCOUNTER_LOCKED; con lai giu nguyen ma loi/HTTP cu.</summary>
    private IActionResult MapError(string? code, string? message, object? details, string? fallbackCode, int fallbackStatus)
    {
        if (code == EncounterLockErrors.EncounterLocked)
            return Conflict(new { error = new { code, message, details } });

        return StatusCode(fallbackStatus, new { error = new { code = code ?? fallbackCode, message } });
    }
}

public record UpdateChiefComplaintBody(string ChiefComplaint);
