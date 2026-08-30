using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Documents;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Upload tai lieu thong minh (smart upload) — 1 diem upload chung dat PHIA TRUOC 3 luong
/// OCR rieng (InBody, LabResult, LegacyImport). He thong tu OCR + phan loai + dieu phoi sang
/// dung luong xac nhan co san. KHONG thay the 3 luong, chi la lop dieu phoi.
/// </summary>
[ApiController]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    private const long MaxBytes = 20L * 1024 * 1024;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/documents/smart-upload
    [HttpPost("api/v1/documents/smart-upload")]
    [RequirePermission("patient.clinical.write")]
    public async Task<IActionResult> SmartUpload(
        IFormFile file,
        [FromForm] Guid patient_id,
        [FromForm] Guid? encounter_id = null,
        CancellationToken ct = default)
    {
        if (patient_id == Guid.Empty)
            return BadRequest(new { error = new { code = "DOC_PATIENT_REQUIRED", message = "Vui lòng chọn bệnh nhân" } });

        if (file is null || file.Length == 0)
            return UnprocessableEntity(new { error = new { code = "DOC_UPLOAD_FAILED", message = "Tải tệp thất bại, vui lòng thử lại" } });

        if (file.Length > MaxBytes)
            return StatusCode(413, new { error = new { code = "DOC_TOO_LARGE", message = "File vượt quá dung lượng tối đa 20MB" } });

        byte[] fileBytes;
        using (var buffer = new MemoryStream())
        {
            await using var stream = file.OpenReadStream();
            await stream.CopyToAsync(buffer, ct);
            fileBytes = buffer.ToArray();
        }

        var result = await _mediator.Send(
            new SmartUploadCommand(patient_id, encounter_id, fileBytes, file.FileName, file.ContentType), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "PATIENT_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return Ok(new { data = result.Value });
    }
}
