using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Bhyt;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/bhyt/reconcile")]
[Authorize]
public class BhytReconcileController : ControllerBase
{
    private readonly IMediator _mediator;
    public BhytReconcileController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/bhyt/reconcile/import
    [HttpPost("import")]
    [RequirePermission("bhyt.reconcile")]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> Import(
        [FromForm] int export_id,
        IFormFile? file,
        [FromForm] string? xml_file_path,
        CancellationToken ct)
    {
        if (file == null && string.IsNullOrEmpty(xml_file_path))
            return BadRequest(Error("BHYT_RECONCILE_FILE_INVALID", "Phai upload file hoac cung cap xml_file_path"));

        Stream? fileStream = file != null ? file.OpenReadStream() : null;
        var result = await _mediator.Send(
            new ImportReconcileFileCommand(export_id, fileStream, file?.FileName, file?.Length ?? 0, xml_file_path), ct);

        if (fileStream != null) await fileStream.DisposeAsync();

        if (!result.IsSuccess) return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return Accepted(new { data = result.Value });
    }

    // GET /api/v1/bhyt/reconcile/{exportId}
    [HttpGet("{exportId:int}")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> ListItems(
        int exportId,
        [FromQuery] string? status_filter,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListReconcileItemsQuery(exportId, status_filter, page, Math.Min(page_size, 200)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/bhyt/reconcile/{itemId}/dispute
    [HttpPost("{itemId:guid}/dispute")]
    [RequirePermission("bhyt.reconcile")]
    public async Task<IActionResult> Dispute(
        Guid itemId,
        [FromBody] DisputeReconcileItemRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new DisputeReconcileItemCommand(itemId, request), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bhyt/reconcile/{itemId}/accept
    [HttpPost("{itemId:guid}/accept")]
    [RequirePermission("bhyt.reconcile")]
    public async Task<IActionResult> Accept(
        Guid itemId,
        [FromBody] AcceptReconcileItemRequest? request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new AcceptReconcileItemCommand(itemId, request), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/bhyt/reconcile/{exportId}/summary
    [HttpGet("{exportId:int}/summary")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> GetSummary(int exportId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReconcileSummaryQuery(exportId), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    private static object Error(string code, string message, object? details = null) =>
        new { error = new { code, message, details } };
}
