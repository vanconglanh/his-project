using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Cdss;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cdss")]
public class CdssController : ControllerBase
{
    private readonly IMediator _mediator;

    public CdssController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/cdss/check
    [HttpPost("check")]
    [RequirePermission("cdss.read")]
    public async Task<IActionResult> Check([FromBody] CdssCheckRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new EvaluatePrescriptionCdssQuery(request), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/cdss/override
    [HttpPost("override")]
    [RequirePermission("cdss.override")]
    public async Task<IActionResult> Override([FromBody] CdssOverrideRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RecordCdssOverrideCommand(request), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = new { id = result.Value } });
    }

    // GET /api/v1/cdss/rules
    [HttpGet("rules")]
    [RequirePermission("cdss.admin")]
    public async Task<IActionResult> ListRules(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListCdssRulesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/cdss/rules
    [HttpPost("rules")]
    [RequirePermission("cdss.admin")]
    public async Task<IActionResult> UpsertRule([FromBody] CdssRuleUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertCdssRuleCommand(request), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = new { id = result.Value } });
    }
}
