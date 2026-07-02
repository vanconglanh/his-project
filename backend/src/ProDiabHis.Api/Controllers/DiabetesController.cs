using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Diabetes;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class DiabetesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiabetesController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/encounters/{encounterId}/diabetes-assessment
    [HttpPost("api/v1/encounters/{encounterId:guid}/diabetes-assessment")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Create(Guid encounterId, [FromBody] DiabetesAssessmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDiabetesAssessmentCommand(encounterId, request), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "DIABETES_ASSESSMENT_EXISTS" ? 409 : 422;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/diabetes-assessment
    [HttpGet("api/v1/encounters/{encounterId:guid}/diabetes-assessment")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Get(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDiabetesAssessmentQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/encounters/{encounterId}/diabetes-assessment
    [HttpPut("api/v1/encounters/{encounterId:guid}/diabetes-assessment")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Update(Guid encounterId, [FromBody] DiabetesAssessmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDiabetesAssessmentCommand(encounterId, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    // GET /api/v1/patients/{patientId}/diabetes-assessments/history
    // Also matches /api/v1/diabetes-assessments/patient/{patientId}/history (legacy)
    [HttpGet("api/v1/patients/{patientId:guid}/diabetes-assessments/history")]
    [HttpGet("api/v1/diabetes-assessments/patient/{patientId:guid}/history")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> History(
        Guid patientId,
        [FromQuery] DateOnly? date_from,
        [FromQuery] DateOnly? date_to,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDiabetesHistoryQuery(patientId, date_from, date_to), ct);
        return Ok(new { data = result.Value });
    }
}

[ApiController]
[Route("api/v1/diabetes-templates")]
[Authorize]
public class DiabetesTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiabetesTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDiabetesTemplatesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Create([FromBody] DiabetesTemplateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDiabetesTemplateCommand(request), ct);
        return StatusCode(201);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DiabetesTemplateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDiabetesTemplateCommand(id, request), ct);
        return Ok();
    }
}
