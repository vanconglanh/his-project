using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Diabetes;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class DiabetesDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiabetesDashboardController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/patients/{id}/diabetes/trajectory
    [HttpGet("api/v1/patients/{id:guid}/diabetes/trajectory")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Trajectory(Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDiabetesTrajectoryQuery(id, from, to), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/patients/{id}/diabetes/deterioration-flags
    [HttpGet("api/v1/patients/{id:guid}/diabetes/deterioration-flags")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> DeteriorationFlags(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeteriorationFlagsQuery(id), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/diabetes/risk-list
    [HttpGet("api/v1/diabetes/risk-list")]
    [RequirePermission("risk.read")]
    public async Task<IActionResult> RiskList(
        [FromQuery] string? level, [FromQuery] string? sort,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRiskListQuery(level, sort, page, pageSize), ct);
        return Ok(new { data = result.Value!.Items, meta = new { page = result.Value!.Page, pageSize = result.Value!.PageSize, total = result.Value!.Total } });
    }
}
