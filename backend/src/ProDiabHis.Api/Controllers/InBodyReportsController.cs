using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.InBody;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Upload + xac nhan ket qua may InBody (thanh phan co the). Xem PRD:
/// docs/prd/inbody-ocr-20260830.md — CHI doc text layer PDF (khong OCR anh),
/// KHONG tu dong ghi vao ho so — luon qua buoc confirm rieng.
/// </summary>
[ApiController]
[Authorize]
public class InBodyReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InBodyReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/patients/{patientId}/inbody-reports
    [HttpPost("api/v1/patients/{patientId:guid}/inbody-reports")]
    [RequirePermission("patient.clinical.write")]
    public async Task<IActionResult> Upload(
        Guid patientId,
        IFormFile file,
        [FromForm] Guid? encounter_id = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return UnprocessableEntity(new { error = new { code = "INBODY_UPLOAD_FAILED", message = "Tải tệp thất bại, vui lòng thử lại" } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadInBodyReportCommand(patientId, encounter_id, stream, file.FileName, file.ContentType), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "PATIENT_NOT_FOUND" => 404,
                "INBODY_TOO_LARGE" => 413,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/patients/{patientId}/inbody-reports
    [HttpGet("api/v1/patients/{patientId:guid}/inbody-reports")]
    [RequirePermission("patient.read")]
    public async Task<IActionResult> List(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListInBodyReportsQuery(patientId, page, page_size), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new
        {
            data = result.Value!.Items,
            meta = new { page = result.Value.Page, page_size = result.Value.PageSize, total = result.Value.Total, total_pages = result.Value.TotalPages }
        });
    }

    // POST /api/v1/inbody-reports/{id}/confirm
    [HttpPost("api/v1/inbody-reports/{id:guid}/confirm")]
    [RequirePermission("patient.clinical.write")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmInBodyReportBody body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ConfirmInBodyReportCommand(id, body.encounter_id, body.fields ?? new List<ConfirmInBodyFieldItem>()), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "INBODY_REPORT_NOT_FOUND" or "ENCOUNTER_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return Ok(new { data = result.Value });
    }
}

public record ConfirmInBodyReportBody(Guid? encounter_id, List<ConfirmInBodyFieldItem>? fields);
