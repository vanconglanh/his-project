using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Patients;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/v1/patients
    [HttpGet]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] string? sort = null,
        [FromQuery] string? status = null,
        [FromQuery] string? gender = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPatientsQuery(page, page_size, sort, status, gender), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    // GET /api/v1/patients/search
    [HttpGet("search")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchPatientsQuery(q, page, page_size), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    // POST /api/v1/patients
    [HttpPost]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreatePatientCommand(request), ct);
        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode == "PATIENT_CODE_EXISTS" ? 409 : 422;
            return StatusCode(statusCode, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, new { data = result.Value });
    }

    // GET /api/v1/patients/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPatientQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/patients/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdatePatientCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PATIENT_NOT_FOUND") return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/patients/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("patient.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeletePatientCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/patients/{id}/encounters
    [HttpGet("{id:guid}/encounters")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetEncounters(Guid id, [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPatientEncountersQuery(id, page, page_size), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    // POST /api/v1/patients/{id}/avatar
    [HttpPost("{id:guid}/avatar")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { code = "AVATAR_MISSING", message = "Vui lòng chọn file ảnh" } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadAvatarCommand(id, stream, file.FileName, file.ContentType, file.Length), ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "AVATAR_FILE_TOO_LARGE")
                return StatusCode(413, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode == "AVATAR_INVALID_FORMAT")
                return StatusCode(415, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode == "PATIENT_NOT_FOUND")
                return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return Ok(new { data = new { avatar_url = result.Value } });
    }

    // GET /api/v1/patients/{id}/allergies
    [HttpGet("{id:guid}/allergies")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetAllergies(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListAllergiesQuery(id), ct);
        return Ok(new { data = result });
    }

    // POST /api/v1/patients/{id}/allergies
    [HttpPost("{id:guid}/allergies")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> AddAllergy(Guid id, [FromBody] AddAllergyRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AddAllergyCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return CreatedAtAction(nameof(GetAllergies), new { id }, new { data = result.Value });
    }

    // DELETE /api/v1/patients/{id}/allergies/{allergyId}
    [HttpDelete("{id:guid}/allergies/{allergyId:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> DeleteAllergy(Guid id, Guid allergyId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteAllergyCommand(id, allergyId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/patients/{id}/insurance
    [HttpGet("{id:guid}/insurance")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetInsurance(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListInsuranceQuery(id), ct);
        return Ok(new { data = result });
    }

    // POST /api/v1/patients/{id}/insurance
    [HttpPost("{id:guid}/insurance")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> AddInsurance(Guid id, [FromBody] InsuranceRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AddInsuranceCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PATIENT_NOT_FOUND") return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return CreatedAtAction(nameof(GetInsurance), new { id }, new { data = result.Value });
    }

    // PUT /api/v1/patients/{id}/insurance/{insuranceId}
    [HttpPut("{id:guid}/insurance/{insuranceId:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> UpdateInsurance(Guid id, Guid insuranceId, [FromBody] InsuranceRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateInsuranceCommand(id, insuranceId, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/patients/{id}/insurance/{insuranceId}
    [HttpDelete("{id:guid}/insurance/{insuranceId:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> DeleteInsurance(Guid id, Guid insuranceId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteInsuranceCommand(id, insuranceId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/patients/{id}/emergency-contacts
    [HttpGet("{id:guid}/emergency-contacts")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetEmergencyContacts(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEmergencyContactsQuery(id), ct);
        return Ok(new { data = result });
    }

    // POST /api/v1/patients/{id}/emergency-contacts
    [HttpPost("{id:guid}/emergency-contacts")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> AddEmergencyContact(Guid id, [FromBody] EmergencyContactRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AddEmergencyContactCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return CreatedAtAction(nameof(GetEmergencyContacts), new { id }, new { data = result.Value });
    }

    // PUT /api/v1/patients/{id}/emergency-contacts/{contactId}
    [HttpPut("{id:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> UpdateEmergencyContact(Guid id, Guid contactId, [FromBody] EmergencyContactRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateEmergencyContactCommand(id, contactId, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/patients/{id}/emergency-contacts/{contactId}
    [HttpDelete("{id:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> DeleteEmergencyContact(Guid id, Guid contactId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteEmergencyContactCommand(id, contactId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/patients/{id}/consents
    [HttpGet("{id:guid}/consents")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> GetConsents(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListConsentsQuery(id), ct);
        return Ok(new { data = result });
    }

    // POST /api/v1/patients/{id}/consents
    [HttpPost("{id:guid}/consents")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> AddConsent(Guid id, [FromBody] AddConsentRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AddConsentCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return CreatedAtAction(nameof(GetConsents), new { id }, new { data = result.Value });
    }

    // PUT /api/v1/patients/{id}/reception-note
    [HttpPut("{id:guid}/reception-note")]
    [RequirePermission("patient.write")]
    public async Task<IActionResult> UpdateReceptionNote(Guid id, [FromBody] UpdateReceptionNoteBody body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateReceptionNoteCommand(id, body.ReceptionNote), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}

public record UpdateReceptionNoteBody(string ReceptionNote);
