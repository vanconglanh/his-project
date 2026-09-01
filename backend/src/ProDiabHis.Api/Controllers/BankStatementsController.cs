using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing.BankReconciliation;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Doi soat sao ke ngan hang/POS: import file sao ke (Excel/CSV) va auto-matching
/// voi khoan thu trong diab_his_bil_payments. Phan doi soat noi bo theo phuong thuc
/// nam o PaymentsController/report payment-method-reconcile (khong lien quan file nay).
/// </summary>
[ApiController]
[Route("api/v1/bil/bank-statements")]
[Authorize]
public class BankStatementsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BankStatementsController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/bil/bank-statements/import
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromForm] string? bank_code,
        [FromForm] DateOnly? statement_date,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "BANK_STATEMENT_INVALID_FORMAT", message = "Vui lòng upload file sao kê." } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new ImportBankStatementCommand(stream, file.FileName, file.ContentType, bank_code, statement_date), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/bil/bank-statements
    [HttpGet]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListBankStatementsQuery(from_date, to_date, page, Math.Min(page_size, 100)), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // GET /api/v1/bil/bank-statements/{id}/lines
    [HttpGet("{id:guid}/lines")]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> GetLines(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBankStatementLinesQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/bil/bank-statements/lines/{lineId}/candidates
    [HttpGet("lines/{lineId:guid}/candidates")]
    [RequirePermission("payment.read")]
    public async Task<IActionResult> GetCandidates(Guid lineId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMatchCandidatesQuery(lineId), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bil/bank-statements/lines/{lineId}/manual-match
    [HttpPost("lines/{lineId:guid}/manual-match")]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> ManualMatch(Guid lineId, [FromBody] ManualMatchLineRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ManualMatchLineCommand(lineId, request.payment_id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bil/bank-statements/lines/{lineId}/ignore
    [HttpPost("lines/{lineId:guid}/ignore")]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> Ignore(Guid lineId, CancellationToken ct)
    {
        var result = await _mediator.Send(new IgnoreLineCommand(lineId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/bil/bank-statements/lines/{lineId}/unmatch
    [HttpPost("lines/{lineId:guid}/unmatch")]
    [RequirePermission("payment.collect")]
    public async Task<IActionResult> Unmatch(Guid lineId, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnmatchLineCommand(lineId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}

public record ManualMatchLineRequest(Guid payment_id);
