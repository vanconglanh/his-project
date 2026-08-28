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

    // ═══════════════════════════════════════════════
    // FR-512 [P1]: Chi phi/hoa hong tung LabOrder tra doi tac
    // ═══════════════════════════════════════════════

    // GET /api/v1/lab-partners/{id}/costs
    [HttpGet("api/v1/lab-partners/{id:guid}/costs")]
    [RequirePermission("lab_partner.finance_read")]
    public async Task<IActionResult> ListCosts(Guid id, [FromQuery] string? periodMonth,
        [FromQuery] bool? unreconciled, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListLabPartnerCostsQuery(id, periodMonth, unreconciled), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/lab-partner-costs
    [HttpPost("api/v1/lab-partner-costs")]
    [RequirePermission("lab_partner.finance_write")]
    public async Task<IActionResult> CreateCost([FromBody] CreateLabPartnerCostRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLabPartnerCostCommand(body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_ORDER_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/lab-partner-costs/{id}
    [HttpPut("api/v1/lab-partner-costs/{id:guid}")]
    [RequirePermission("lab_partner.finance_write")]
    public async Task<IActionResult> UpdateCost(Guid id, [FromBody] UpdateLabPartnerCostRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabPartnerCostCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_COST_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    // ═══════════════════════════════════════════════
    // FR-512 [P1]: Ky doi soat cong no theo thang
    // ═══════════════════════════════════════════════

    // GET /api/v1/lab-partners/{id}/reconciliations
    [HttpGet("api/v1/lab-partners/{id:guid}/reconciliations")]
    [RequirePermission("lab_partner.finance_read")]
    public async Task<IActionResult> ListReconciliations(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListLabPartnerReconciliationsQuery(id), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/lab-partners/{id}/reconciliations
    [HttpPost("api/v1/lab-partners/{id:guid}/reconciliations")]
    [RequirePermission("lab_partner.finance_write")]
    public async Task<IActionResult> CreateReconciliation(Guid id,
        [FromBody] CreateLabPartnerReconciliationRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLabPartnerReconciliationCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode switch
            {
                "LAB_PARTNER_NOT_FOUND" => 404,
                "LAB_PARTNER_RECONCILIATION_EXISTS" => 409,
                "LAB_PARTNER_COST_EMPTY" => 409,
                _ => 400
            };
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/lab-partner-reconciliations/{id}/status
    [HttpPut("api/v1/lab-partner-reconciliations/{id:guid}/status")]
    [RequirePermission("lab_partner.finance_write")]
    public async Task<IActionResult> UpdateReconciliationStatus(Guid id,
        [FromBody] UpdateLabPartnerReconciliationStatusRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabPartnerReconciliationStatusCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_PARTNER_RECONCILIATION_NOT_FOUND" ? 404 : 400;
            return StatusCode(code, Error(result.ErrorCode!, result.ErrorMessage!));
        }
        return Ok();
    }

    private static object Error(string code, string message) =>
        new { error = new { code, message } };
}
