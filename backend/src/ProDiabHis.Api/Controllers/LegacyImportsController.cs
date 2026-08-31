using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.LegacyImport;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Nhap lieu hang loat ho so giay cu dang anh scan: admin upload 1 file ZIP -> giai nen an toan
/// -> OCR tung anh (Tesseract) chay nen (Hangfire) -> tao item cho admin review/match benh nhan
/// -> confirm -> luu thanh tai lieu dinh kem ho so benh nhan (KHONG tu tao benh an/luot kham).
/// Tinh nang migration du lieu 1 lan, chi danh cho admin (permission legacy_import.write).
/// </summary>
[ApiController]
[Authorize]
public class LegacyImportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LegacyImportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/v1/legacy-imports
    [HttpPost("api/v1/legacy-imports")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> Create(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { code = "LEGACY_IMPORT_INVALID_ZIP", message = "Vui lòng chọn file ZIP" } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new CreateLegacyImportBatchCommand(stream, file.FileName, file.ContentType, file.Length), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "LEGACY_IMPORT_TOO_LARGE" => 413,
                "LEGACY_IMPORT_INVALID_ZIP" => 415,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/legacy-imports
    [HttpGet("api/v1/legacy-imports")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListLegacyImportBatchesQuery(page, page_size), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new
        {
            data = result.Value!.Items,
            meta = new { page = result.Value.Page, page_size = result.Value.PageSize, total = result.Value.Total, total_pages = result.Value.TotalPages }
        });
    }

    // GET /api/v1/legacy-imports/{id}
    [HttpGet("api/v1/legacy-imports/{id:guid}")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLegacyImportBatchQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/legacy-imports/{id}/items?status=pending_match
    [HttpGet("api/v1/legacy-imports/{id:guid}/items")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> ListItems(
        Guid id,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListLegacyImportItemsQuery(id, status, page, page_size), ct);
        if (!result.IsSuccess)
        {
            var httpStatus = result.ErrorCode == "LEGACY_IMPORT_BATCH_NOT_FOUND" ? 404 : 422;
            return StatusCode(httpStatus, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return Ok(new
        {
            data = result.Value!.Items,
            meta = new { page = result.Value.Page, page_size = result.Value.PageSize, total = result.Value.Total, total_pages = result.Value.TotalPages }
        });
    }

    // PUT /api/v1/legacy-imports/items/{itemId}/match
    [HttpPut("api/v1/legacy-imports/items/{itemId:guid}/match")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> Match(Guid itemId, [FromBody] MatchLegacyImportItemBody body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MatchLegacyImportItemCommand(itemId, body.patient_id), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "LEGACY_IMPORT_ITEM_NOT_FOUND" or "PATIENT_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/legacy-imports/items/{itemId}/confirm
    [HttpPost("api/v1/legacy-imports/items/{itemId:guid}/confirm")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> Confirm(Guid itemId, [FromBody] ConfirmLegacyImportItemBody? body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ConfirmLegacyImportItemCommand(itemId, body?.ocr_text, body?.patient_id, body?.doc_type), ct);

        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                "LEGACY_IMPORT_ITEM_NOT_FOUND" or "PATIENT_NOT_FOUND" => 404,
                "ITEM_NOT_MATCHED" or "LEGACY_IMPORT_ITEM_ALREADY_CONFIRMED" or "LEGACY_IMPORT_ITEM_NO_IMAGE" => 422,
                _ => 422
            };
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/legacy-imports/items/{itemId}/reject
    [HttpPost("api/v1/legacy-imports/items/{itemId:guid}/reject")]
    [RequirePermission("legacy_import.write")]
    public async Task<IActionResult> Reject(Guid itemId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new RejectLegacyImportItemCommand(itemId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}

public record MatchLegacyImportItemBody(Guid patient_id);
public record ConfirmLegacyImportItemBody(string? ocr_text, Guid? patient_id, string? doc_type);
