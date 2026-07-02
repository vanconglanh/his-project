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
}
