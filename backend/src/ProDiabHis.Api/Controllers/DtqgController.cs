using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Dtqg;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/dtqg")]
[Authorize]
public class DtqgController : ControllerBase
{
    private readonly IMediator _mediator;
    public DtqgController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/dtqg/submissions
    [HttpGet("submissions")]
    [RequirePermission("dtqg.submit")]
    public async Task<IActionResult> ListSubmissions(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListDtqgSubmissionsQuery(status, from_date, to_date, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/dtqg/submissions/{id}/cancel-on-portal
    [HttpPost("submissions/{id:guid}/cancel-on-portal")]
    [RequirePermission("dtqg.admin")]
    public async Task<IActionResult> CancelOnPortal(Guid id, [FromBody] CancelOnPortalRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelOnPortalCommand(id, request.Reason), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/dtqg/credentials
    [HttpGet("credentials")]
    [RequirePermission("dtqg.admin")]
    public async Task<IActionResult> GetCredentials(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDtqgCredentialsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/dtqg/credentials
    [HttpPut("credentials")]
    [RequirePermission("dtqg.admin")]
    public async Task<IActionResult> UpsertCredentials([FromBody] DtqgCredentialsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertDtqgCredentialsCommand(request), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/dtqg/credentials/test
    [HttpPost("credentials/test")]
    [RequirePermission("dtqg.admin")]
    public async Task<IActionResult> TestCredentials(CancellationToken ct)
    {
        var result = await _mediator.Send(new TestDtqgCredentialsCommand(), ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}

public record CancelOnPortalRequest(string Reason);
