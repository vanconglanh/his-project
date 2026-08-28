using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Files;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/files/upload
    [HttpPost("upload")]
    [RequirePermission("file.upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? category = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return UnprocessableEntity(new { error = new { code = "FILE_UPLOAD_FAILED", message = "Tải tệp thất bại, vui lòng thử lại" } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadFileCommand(stream, file.FileName, file.ContentType, file.Length, category), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "FILE_UPLOAD_FAILED" && result.ErrorMessage!.Contains("20MB") ? 413 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/files/{id}/signed-url
    [HttpGet("{id:guid}/signed-url")]
    [Authorize]
    public async Task<IActionResult> GetSignedUrl(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSignedUrlQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/files/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("file.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteFileCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/files/{fileId}/annotations
    // Đính kèm hình ảnh lâm sàng + annotation (FR-311): xem danh sách annotation (layer JSON,
    // không sửa ảnh gốc) gắn với 1 file ảnh.
    [HttpGet("{fileId:guid}/annotations")]
    [RequirePermission("file_annotation.read")]
    public async Task<IActionResult> ListAnnotations(Guid fileId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListFileAnnotationsQuery(fileId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/files/{fileId}/annotations
    // Chỉ Bác sĩ/Điều dưỡng (role bac_si/ky_thuat_vien) được tạo annotation.
    [HttpPost("{fileId:guid}/annotations")]
    [RequirePermission("file_annotation.write")]
    public async Task<IActionResult> CreateAnnotation(
        Guid fileId,
        [FromBody] CreateFileAnnotationRequest request,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new CreateFileAnnotationCommand(fileId, request.PatientId, request.EncounterId, request.AnnotationData), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "FILE_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/files/{fileId}/annotations/{id}
    [HttpPut("{fileId:guid}/annotations/{id:guid}")]
    [RequirePermission("file_annotation.write")]
    public async Task<IActionResult> UpdateAnnotation(
        Guid fileId,
        Guid id,
        [FromBody] UpdateFileAnnotationRequest request,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateFileAnnotationCommand(fileId, id, request.AnnotationData), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "FILE_ANNOTATION_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/files/{fileId}/annotations/{id}
    [HttpDelete("{fileId:guid}/annotations/{id:guid}")]
    [RequirePermission("file_annotation.delete")]
    public async Task<IActionResult> DeleteAnnotation(Guid fileId, Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteFileAnnotationCommand(fileId, id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}

public record CreateFileAnnotationRequest(Guid? PatientId, Guid? EncounterId, string AnnotationData);

public record UpdateFileAnnotationRequest(string AnnotationData);
