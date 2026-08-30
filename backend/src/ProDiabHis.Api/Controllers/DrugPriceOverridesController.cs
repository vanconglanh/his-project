using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Drugs;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// CRUD gia override + an/hien THUOC theo chi nhanh/nhom (migration 9185).
/// Mirror ServicePriceOverridesController. Chi admin / quan_ly_vung (quyen drug.price_override).
/// </summary>
[ApiController]
[Route("api/v1/drug-price-overrides")]
[Authorize]
public class DrugPriceOverridesController : ControllerBase
{
    private readonly IMediator _mediator;
    public DrugPriceOverridesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/drug-price-overrides
    [HttpGet]
    [RequirePermission("drug.price_override")]
    public async Task<IActionResult> List(
        [FromQuery] string? drug_id,
        [FromQuery] int? branch_id,
        [FromQuery] int? group_id,
        [FromQuery] string? scope,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListDrugPriceOverridesQuery(
            drug_id, branch_id, group_id, scope, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // GET /api/v1/drug-price-overrides/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("drug.price_override")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDrugPriceOverrideQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/drug-price-overrides
    [HttpPost]
    [RequirePermission("drug.price_override")]
    public async Task<IActionResult> Create([FromBody] CreateDrugPriceOverrideRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDrugPriceOverrideCommand(request), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "PRICE_OVERLAP" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "FORBIDDEN" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "DRUG_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/drug-price-overrides/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("drug.price_override")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDrugPriceOverrideRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDrugPriceOverrideCommand(id, request), ct);
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

    // DELETE /api/v1/drug-price-overrides/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("drug.price_override")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteDrugPriceOverrideCommand(id), ct);
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
