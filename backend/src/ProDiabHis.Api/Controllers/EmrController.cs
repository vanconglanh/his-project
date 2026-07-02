using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.EMR;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class EmrController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmrController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/encounters/{encounterId}/emr
    [HttpGet("api/v1/encounters/{encounterId:guid}/emr")]
    [RequirePermission("emr.read")]
    public async Task<IActionResult> Get(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmrQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/encounters/{encounterId}/emr
    [HttpPut("api/v1/encounters/{encounterId:guid}/emr")]
    [RequirePermission("emr.write")]
    public async Task<IActionResult> SaveDraft(Guid encounterId, [FromBody] EmrSaveRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SaveEmrDraftCommand(encounterId, request), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "EMR_ALREADY_SIGNED" ? 409 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/encounters/{encounterId}/emr/sign
    [HttpPost("api/v1/encounters/{encounterId:guid}/emr/sign")]
    [RequirePermission("emr.sign")]
    public async Task<IActionResult> Sign(Guid encounterId, [FromBody] SignEmrRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SignEmrCommand(encounterId, request), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/encounters/{encounterId}/emr/unsign
    [HttpPost("api/v1/encounters/{encounterId:guid}/emr/unsign")]
    [RequirePermission("emr.unsign")]
    public async Task<IActionResult> Unsign(Guid encounterId, [FromBody] UnsignBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnsignEmrCommand(encounterId, body.Reason), ct);
        if (!result.IsSuccess)
            return StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    // GET /api/v1/encounters/{encounterId}/emr/pdf
    [HttpGet("api/v1/encounters/{encounterId:guid}/emr/pdf")]
    [RequirePermission("emr.export")]
    public async Task<IActionResult> ExportPdf(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportEmrPdfCommand(encounterId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"emr_{encounterId:N}.pdf");
    }

    // GET /api/v1/encounters/{encounterId}/emr/versions
    [HttpGet("api/v1/encounters/{encounterId:guid}/emr/versions")]
    [RequirePermission("emr.read")]
    public async Task<IActionResult> Versions(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmrVersionsQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/emr/versions/{versionId}/diff
    [HttpGet("api/v1/encounters/{encounterId:guid}/emr/versions/{versionId:guid}/diff")]
    [RequirePermission("emr.read")]
    public async Task<IActionResult> VersionDiff(Guid encounterId, Guid versionId,
        [FromQuery] Guid? compare_to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmrVersionDiffQuery(encounterId, versionId, compare_to), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}

public record UnsignBody(string Reason);

[ApiController]
[Route("api/v1/emr-templates")]
[Route("api/v1/emr/templates")]
[Authorize]
public class EmrTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmrTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission("emr_template.read")]
    public async Task<IActionResult> List([FromQuery] string? speciality, [FromQuery] bool? is_system, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListEmrTemplatesQuery(speciality, is_system), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost]
    [RequirePermission("emr_template.write")]
    public async Task<IActionResult> Create([FromBody] EmrTemplateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEmrTemplateCommand(request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("emr_template.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EmrTemplateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEmrTemplateCommand(id, request), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("emr_template.write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteEmrTemplateCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "TEMPLATE_SYSTEM" ? 422 : 404;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }
}
