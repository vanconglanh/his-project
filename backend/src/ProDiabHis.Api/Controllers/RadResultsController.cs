using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.RadResults;
using ProDiabHis.Application.RadResults.Ocr;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class RadResultsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RadResultsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/rad-results
    [HttpGet("api/v1/rad-results")]
    [RequirePermission("rad_result.read")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patient_id,
        [FromQuery] Guid? encounter_id,
        [FromQuery] Guid? rad_order_id,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListRadResultsQuery(
            patient_id, encounter_id, rad_order_id, status, page, page_size), ct);
        var (items, total) = result.Value!;
        return Ok(new { data = items, meta = new { page, page_size, total } });
    }

    // POST /api/v1/rad-results
    [HttpPost("api/v1/rad-results")]
    [RequirePermission("rad_result.write")]
    public async Task<IActionResult> Create([FromBody] RadResultCreateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRadResultCommand(body), ct);
        if (!result.IsSuccess)
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/rad-results/{id}
    [HttpPut("api/v1/rad-results/{id:guid}")]
    [RequirePermission("rad_result.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RadResultUpdateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRadResultCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "RAD_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // POST /api/v1/rad-results/{id}/verify
    [HttpPost("api/v1/rad-results/{id:guid}/verify")]
    [RequirePermission("rad_result.verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyRadResultCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "RAD_RESULT_NOT_FOUND" ? 404 :
                       result.ErrorCode == "VERIFY_SELF_FORBIDDEN" ? 403 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { data = new { signed_pdf_url = result.Value } });
    }

    // POST /api/v1/rad-results/{id}/dicom-upload
    [HttpPost("api/v1/rad-results/{id:guid}/dicom-upload")]
    [RequirePermission("rad_result.write")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB
    public async Task<IActionResult> DicomUpload(Guid id, IFormFileCollection files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
            return BadRequest(Error("RAD_DICOM_UPLOAD_FAILED", "Không có file DICOM được gửi lên"));

        var fileList = files.Select(f =>
            (f.OpenReadStream(), f.FileName, f.Length)).ToList();

        var result = await _mediator.Send(new UploadDicomCommand(id, fileList), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "RAD_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }

        var (count, size) = result.Value!;
        return Ok(new { data = new { uploaded_count = count, total_size_bytes = size } });
    }

    // GET /api/v1/rad-results/{id}/pdf
    [HttpGet("api/v1/rad-results/{id:guid}/pdf")]
    [RequirePermission("rad_result.read")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportRadResultPdfQuery(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "RAD_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return File(result.Value!, "application/pdf", $"rad-result-{id}.pdf");
    }

    // POST /api/v1/rad-results/ocr-extract
    // Upload file phieu KQ CDHA (PDF/anh) -> OCR doc + tach 2 doan Mo ta/Ket luan.
    // KHONG luu — chi tra ve text doc duoc de nguoi dung xac nhan/sua truoc khi luu (buoc ocr-confirm).
    [HttpPost("api/v1/rad-results/ocr-extract")]
    [RequirePermission("rad_result.write")]
    public async Task<IActionResult> OcrExtract(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return UnprocessableEntity(Error("RAD_OCR_UPLOAD_FAILED", "Tải tệp thất bại, vui lòng thử lại"));

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new ExtractRadResultOcrCommand(stream, file.FileName, file.ContentType), ct);

        if (!result.IsSuccess)
        {
            var code = result.ErrorCode switch
            {
                "RAD_OCR_TOO_LARGE" => 413,
                _ => 422
            };
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }

        return Ok(new { data = result.Value });
    }

    // POST /api/v1/rad-results/ocr-confirm
    // Xac nhan (da sua tay neu can) 2 o Mo ta/Ket luan -> tao RadResult qua luong nhap KQ san co.
    [HttpPost("api/v1/rad-results/ocr-confirm")]
    [RequirePermission("rad_result.write")]
    public async Task<IActionResult> OcrConfirm([FromBody] RadOcrConfirmBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmRadResultOcrCommand(
            body.rad_order_id,
            body.findings ?? string.Empty,
            body.impression,
            body.conclusion ?? string.Empty,
            body.recommendations,
            body.performed_at ?? DateTime.UtcNow), ct);

        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "RAD_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }

        return StatusCode(201, new { data = result.Value });
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message } };
}

public record RadOcrConfirmBody(
    Guid      rad_order_id,
    string?   findings,
    string?   impression,
    string?   conclusion,
    string?   recommendations,
    DateTime? performed_at);
