using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Files;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class ClsUploadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClsUploadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/v1/patients/{patientId}/cls-uploads
    [HttpGet("api/v1/patients/{patientId:guid}/cls-uploads")]
    [RequirePermission("cls_upload.read")]
    public async Task<IActionResult> List(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] string? doc_type = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListClsUploadsQuery(patientId, page, page_size, doc_type), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total }
        });
    }

    // POST /api/v1/patients/{patientId}/cls-uploads
    [HttpPost("api/v1/patients/{patientId:guid}/cls-uploads")]
    [RequirePermission("cls_upload.create")]
    public async Task<IActionResult> Upload(
        Guid patientId,
        IFormFile file,
        [FromForm] string doc_type,
        [FromForm] Guid? encounter_id = null,
        [FromForm] string? note = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { code = "CLS_UPLOAD_MISSING", message = "Vui lòng chọn file" } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadClsCommand(
            patientId, stream, file.FileName, file.ContentType, file.Length,
            doc_type, encounter_id, note), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "CLS_UPLOAD_TOO_LARGE" => 413,
                "CLS_UPLOAD_INVALID_FORMAT" => 415,
                "PATIENT_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/patients/{patientId}/cls-uploads/{id}
    [HttpGet("api/v1/patients/{patientId:guid}/cls-uploads/{id:guid}")]
    [RequirePermission("cls_upload.read")]
    public async Task<IActionResult> GetById(Guid patientId, Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetClsUploadQuery(patientId, id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/patients/{patientId}/cls-uploads/{id}
    [HttpDelete("api/v1/patients/{patientId:guid}/cls-uploads/{id:guid}")]
    [RequirePermission("cls_upload.delete")]
    public async Task<IActionResult> Delete(Guid patientId, Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteClsUploadCommand(patientId, id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/encounters/{encounterId}/cls-uploads
    [HttpGet("api/v1/encounters/{encounterId:guid}/cls-uploads")]
    [RequirePermission("cls_upload.read")]
    public async Task<IActionResult> ListByEncounter(Guid encounterId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEncounterClsUploadsQuery(encounterId), ct);
        return Ok(new { data = result });
    }
}
