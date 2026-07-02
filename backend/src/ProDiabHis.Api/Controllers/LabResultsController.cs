using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.LabResults;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class LabResultsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabResultsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/lab-results
    [HttpGet("api/v1/lab-results")]
    [RequirePermission("lab_result.read")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patient_id,
        [FromQuery] Guid? encounter_id,
        [FromQuery] Guid? lab_order_id,
        [FromQuery] string? status,
        [FromQuery] string? flag,
        [FromQuery] DateTime? from_date,
        [FromQuery] DateTime? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        page_size = Math.Min(page_size, 100);
        var result = await _mediator.Send(new ListLabResultsQuery(
            patient_id, encounter_id, lab_order_id, status, flag,
            from_date, to_date, page, page_size), ct);

        if (!result.IsSuccess)
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));

        var (items, total) = result.Value!;
        return Ok(new { data = items, meta = new { page, page_size, total } });
    }

    // POST /api/v1/lab-results
    [HttpPost("api/v1/lab-results")]
    [RequirePermission("lab_result.write")]
    public async Task<IActionResult> Create([FromBody] LabResultCreateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLabResultCommand(body), ct);
        if (!result.IsSuccess)
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/lab-results/{id}
    [HttpPut("api/v1/lab-results/{id:guid}")]
    [RequirePermission("lab_result.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LabResultUpdateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabResultCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_RESULT_NOT_FOUND" ? 404 :
                       result.ErrorCode == "LAB_RESULT_EDIT_TIMEOUT" ? 409 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/lab-results/{id}/verify
    [HttpPost("api/v1/lab-results/{id:guid}/verify")]
    [RequirePermission("lab_result.verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyLabResultCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_RESULT_ALREADY_VERIFIED" ? 409 :
                       result.ErrorCode == "LAB_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // POST /api/v1/lab-results/{id}/unverify
    [HttpPost("api/v1/lab-results/{id:guid}/unverify")]
    [RequirePermission("lab_result.verify")]
    public async Task<IActionResult> Unverify(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnverifyLabResultCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_RESULT_NOT_FOUND" ? 404 :
                       result.ErrorCode == "LAB_RESULT_EDIT_TIMEOUT" ? 409 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // POST /api/v1/lab-results/import
    [HttpPost("api/v1/lab-results/import")]
    [RequirePermission("lab_result.import")]
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromForm] string format = "CSV",
        [FromForm] bool auto_verify = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Error("LAB_IMPORT_INVALID_FORMAT", "File không được để trống"));

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportLabResultsCommand(stream, format, auto_verify), ct);

        if (!result.IsSuccess)
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(new { data = result.Value });
    }

    // GET /api/v1/lab-results/abnormal
    [HttpGet("api/v1/lab-results/abnormal")]
    [RequirePermission("lab_result.read")]
    public async Task<IActionResult> Abnormal(
        [FromQuery] string? severity,
        [FromQuery] DateTime? from_date,
        [FromQuery] DateTime? to_date,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAbnormalLabResultsQuery(severity ?? "ALL", from_date, to_date), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/lab-results/history-trend
    [HttpGet("api/v1/lab-results/history-trend")]
    [RequirePermission("lab_result.read")]
    public async Task<IActionResult> HistoryTrend(
        [FromQuery] Guid patient_id,
        [FromQuery] string test_code,
        [FromQuery] DateTime? from_date,
        [FromQuery] DateTime? to_date,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLabResultHistoryTrendQuery(patient_id, test_code, from_date, to_date), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/lab-results/{id}/pdf
    [HttpGet("api/v1/lab-results/{id:guid}/pdf")]
    [RequirePermission("lab_result.read")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportLabResultPdfQuery(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_RESULT_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return File(result.Value!, "application/pdf", $"lab-result-{id}.pdf");
    }

    // POST /api/v1/lab-results/batch-verify
    [HttpPost("api/v1/lab-results/batch-verify")]
    [RequirePermission("lab_result.verify")]
    public async Task<IActionResult> BatchVerify([FromBody] BatchVerifyBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new BatchVerifyLabResultsCommand(body.ResultIds), ct);
        return Ok(new { data = result.Value });
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message } };
}

public record BatchVerifyBody(IReadOnlyList<Guid> ResultIds);
