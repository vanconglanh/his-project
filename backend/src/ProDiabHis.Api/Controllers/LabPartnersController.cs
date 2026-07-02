using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.LabPartners;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class LabPartnersController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabPartnersController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/lab-partners
    [HttpGet("api/v1/lab-partners")]
    [RequirePermission("lab_partner.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListLabPartnersQuery(status, q), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/lab-partners
    [HttpPost("api/v1/lab-partners")]
    [RequirePermission("lab_partner.write")]
    public async Task<IActionResult> Create([FromBody] LabPartnerCreateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLabPartnerCommand(body), ct);
        if (!result.IsSuccess)
            return BadRequest(Error(result.ErrorCode!, result.ErrorMessage!));
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/lab-partners/{id}
    [HttpGet("api/v1/lab-partners/{id:guid}")]
    [RequirePermission("lab_partner.read")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLabPartnerQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/lab-partners/{id}
    [HttpPut("api/v1/lab-partners/{id:guid}")]
    [RequirePermission("lab_partner.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LabPartnerUpdateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabPartnerCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // DELETE /api/v1/lab-partners/{id}
    [HttpDelete("api/v1/lab-partners/{id:guid}")]
    [RequirePermission("lab_partner.admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteLabPartnerCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return NoContent();
    }

    // POST /api/v1/lab-partners/{id}/test-connection
    [HttpPost("api/v1/lab-partners/{id:guid}/test-connection")]
    [RequirePermission("lab_partner.write")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new TestLabPartnerConnectionCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/lab-partners/{id}/credentials
    [HttpPut("api/v1/lab-partners/{id:guid}/credentials")]
    [RequirePermission("lab_partner.admin")]
    public async Task<IActionResult> UpdateCredentials(Guid id, [FromBody] LabPartnerCredentialsRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabPartnerCredentialsCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // POST /api/v1/lab-partners/{id}/credentials/rotate
    [HttpPost("api/v1/lab-partners/{id:guid}/credentials/rotate")]
    [RequirePermission("lab_partner.admin")]
    public async Task<IActionResult> RotateKey(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RotateLabPartnerApiKeyCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok(new { data = result.Value });
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message } };
}
