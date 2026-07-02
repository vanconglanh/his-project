using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Bhyt;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/bhyt/exports")]
[Authorize]
public class BhytExportController : ControllerBase
{
    private readonly IMediator _mediator;
    public BhytExportController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/bhyt/exports
    [HttpPost]
    [RequirePermission("bhyt.export")]
    public async Task<IActionResult> Create([FromBody] CreateBhytExportRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBhytExportCommand(request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "BHYT_EXPORT_INVALID_PERIOD")
                return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
            if (result.ErrorCode == "BHYT_EXPORT_CONFLICT")
                return Conflict(Error(result.ErrorCode!, result.ErrorMessage!));
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return CreatedAtAction(nameof(GetDetail), new { id = result.Value!.Id }, new { data = result.Value });
    }

    // GET /api/v1/bhyt/exports
    [HttpGet]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? period_month,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListBhytExportsQuery(period_month, status, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // GET /api/v1/bhyt/exports/{id}
    [HttpGet("{id:int}")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBhytExportQuery(id), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/bhyt/exports/{id}
    [HttpDelete("{id:int}")]
    [RequirePermission("bhyt.export")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteBhytExportCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "BHYT_EXPORT_NOT_FOUND") return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
            return Conflict(Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return NoContent();
    }

    // POST /api/v1/bhyt/exports/{id}/generate
    [HttpPost("{id:int}/generate")]
    [RequirePermission("bhyt.generate")]
    public async Task<IActionResult> Generate(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateBhytXmlCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BHYT_PERIOD_LOCKED"
                ? Conflict(Error(result.ErrorCode!, result.ErrorMessage!))
                : BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bhyt/exports/{id}/regenerate
    [HttpPost("{id:int}/regenerate")]
    [RequirePermission("bhyt.generate")]
    public async Task<IActionResult> Regenerate(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegenerateBhytXmlCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BHYT_PERIOD_LOCKED"
                ? Conflict(Error(result.ErrorCode!, result.ErrorMessage!))
                : BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bhyt/exports/{id}/validate
    [HttpPost("{id:int}/validate")]
    [RequirePermission("bhyt.validate")]
    public async Task<IActionResult> Validate(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ValidateBhytXmlCommand(id), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(Error(result.ErrorCode!, result.ErrorMessage!, result.ErrorDetails));
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bhyt/exports/{id}/sign
    [HttpPost("{id:int}/sign")]
    [RequirePermission("bhyt.sign")]
    public async Task<IActionResult> Sign(int id, [FromBody] SignBhytExportRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SignBhytXmlCommand(id, request), ct);
        if (!result.IsSuccess) return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bhyt/exports/{id}/submit
    [HttpPost("{id:int}/submit")]
    [RequirePermission("bhyt.submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitBhytExportCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return Accepted(new { data = result.Value });
    }

    // GET /api/v1/bhyt/exports/{id}/xml/{tableNo}
    [HttpGet("{id:int}/xml/{tableNo:int}")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> DownloadTableXml(int id, int tableNo, CancellationToken ct)
    {
        if (tableNo < 1 || tableNo > 5)
            return BadRequest(Error("BHYT_EXPORT_INVALID_PERIOD", "tableNo phai tu 1 den 5"));
        var result = await _mediator.Send(new DownloadBhytTableXmlQuery(id, tableNo), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return File(result.Value!, "application/xml", $"bhyt_bang{tableNo}.xml");
    }

    // GET /api/v1/bhyt/exports/{id}/xml/all
    [HttpGet("{id:int}/xml/all")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> DownloadAllXml(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DownloadBhytAllXmlQuery(id), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return File(result.Value!, "application/zip", $"bhyt_export_{id}_all.zip");
    }

    // GET /api/v1/bhyt/exports/{id}/items/table/{tableNo}
    [HttpGet("{id:int}/items/table/{tableNo:int}")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> ListItems(
        int id, int tableNo,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListBhytExportItemsQuery(id, tableNo, page, Math.Min(page_size, 200)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // GET /api/v1/bhyt/exports/{id}/items/table/{tableNo}/{rowId}
    [HttpGet("{id:int}/items/table/{tableNo:int}/{rowId:guid}")]
    [RequirePermission("bhyt.read")]
    public async Task<IActionResult> GetItem(int id, int tableNo, Guid rowId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBhytExportItemQuery(id, tableNo, rowId), ct);
        if (!result.IsSuccess) return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    private static object Error(string code, string message, object? details = null) =>
        new { error = new { code, message, details } };
}
