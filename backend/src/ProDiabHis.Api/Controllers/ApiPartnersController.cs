using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/api-partners")]
public class ApiPartnersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApiPartnersController(IMediator mediator) => _mediator = mediator;

    private int TenantId => int.Parse(User.FindFirst("tenant_id")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirst("user_id")!.Value);

    [HttpGet]
    [RequirePermission("api_partner.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListApiPartnersQuery(TenantId, q, status), cancellationToken);
        return Ok(new { data = result, meta = new { total = result.Count } });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("api_partner.read")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApiPartnerQuery(id, TenantId), cancellationToken);
        if (result == null)
            return NotFound(new { error = new { code = "PARTNER_NOT_FOUND", message = "Khong tim thay doi tac" } });
        return Ok(new { data = result });
    }

    [HttpPost]
    [RequirePermission("api_partner.write")]
    public async Task<IActionResult> Create(
        [FromBody] ApiPartnerCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateApiPartnerCommand(TenantId, request, UserId), cancellationToken);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("api_partner.write")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ApiPartnerUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateApiPartnerCommand(id, TenantId, request), cancellationToken);
        if (result == null)
            return NotFound(new { error = new { code = "PARTNER_NOT_FOUND", message = "Khong tim thay doi tac" } });
        return Ok(new { data = result });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("api_partner.write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteApiPartnerCommand(id, TenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-key")]
    [RequirePermission("api_partner.admin")]
    public async Task<IActionResult> RegenerateKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new RegenerateApiKeyCommand(id, TenantId), cancellationToken);
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "PARTNER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "PARTNER_NOT_FOUND", message = "Khong tim thay doi tac" } });
        }
    }

    [HttpPost("{id:guid}/test-call")]
    [RequirePermission("api_partner.admin")]
    public IActionResult TestCall(Guid id)
        => Ok(new { status = "ok", message = "Test call simulated successfully" });

    [HttpGet("{id:guid}/usage-stats")]
    [RequirePermission("api_partner.read")]
    public async Task<IActionResult> UsageStats(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsageStatsQuery(id, TenantId, from, to), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpGet("{id:guid}/request-logs")]
    [RequirePermission("api_partner.read")]
    public async Task<IActionResult> RequestLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int? status_code = null,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _mediator.Send(new GetRequestLogsQuery(id, TenantId, page, status_code), cancellationToken);
        return Ok(new { data = items, meta = new { page, total } });
    }
}
