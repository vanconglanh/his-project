using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// CRUD gia override dich vu theo chi nhanh/nhom (BR-70..BR-76, E/Dot3 da chi nhanh).
/// Chi admin / quan_ly_vung (quyen service.price_override) duoc tao/sua/xoa (BR-74).
/// </summary>
[ApiController]
[Route("api/v1/service-price-overrides")]
[Authorize]
public class ServicePriceOverridesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicePriceOverridesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/service-price-overrides
    [HttpGet]
    [RequirePermission("service.price_override")]
    public async Task<IActionResult> List(
        [FromQuery] string? service_id,
        [FromQuery] int? branch_id,
        [FromQuery] int? group_id,
        [FromQuery] string? scope,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListServicePriceOverridesQuery(
            Guid.TryParse(service_id, out var sid) ? sid : null,
            branch_id, group_id, scope, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // GET /api/v1/service-price-overrides/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("service.price_override")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetServicePriceOverrideQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/service-price-overrides
    [HttpPost]
    [RequirePermission("service.price_override")]
    public async Task<IActionResult> Create([FromBody] CreateServicePriceOverrideRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateServicePriceOverrideCommand(request), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "PRICE_OVERLAP" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "FORBIDDEN" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "SERVICE_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/service-price-overrides/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("service.price_override")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServicePriceOverrideRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateServicePriceOverrideCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "PRICE_OVERLAP" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "FORBIDDEN" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "PRICE_OVERRIDE_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/service-price-overrides/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("service.price_override")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteServicePriceOverrideCommand(id), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "FORBIDDEN" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "PRICE_OVERRIDE_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return NoContent();
    }
}
