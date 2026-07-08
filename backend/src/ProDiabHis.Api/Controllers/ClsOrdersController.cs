using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.CLS;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class ClsOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClsOrdersController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/encounters/{encounterId}/lab-orders
    [HttpPost("api/v1/encounters/{encounterId:guid}/lab-orders")]
    [RequirePermission("lab_order.create")]
    public async Task<IActionResult> CreateLab(Guid encounterId, [FromBody] CreateLabOrdersBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLabOrdersCommand(encounterId, body.Tests), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/lab-orders
    [HttpGet("api/v1/encounters/{encounterId:guid}/lab-orders")]
    [RequirePermission("lab_order.read")]
    public async Task<IActionResult> ListLab(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListLabOrdersQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/lab-orders/{id}
    [HttpPut("api/v1/lab-orders/{id:guid}")]
    [RequirePermission("lab_order.update")]
    public async Task<IActionResult> UpdateLab(Guid id, [FromBody] UpdateOrderStatusBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLabOrderStatusCommand(id, body.Status, body.Note), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    // DELETE /api/v1/lab-orders/{id}
    [HttpDelete("api/v1/lab-orders/{id:guid}")]
    [RequirePermission("lab_order.delete")]
    public async Task<IActionResult> DeleteLab(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteLabOrderCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "LAB_ORDER_CANNOT_DELETE" ? 409 : 404;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }

    // POST /api/v1/encounters/{encounterId}/rad-orders
    [HttpPost("api/v1/encounters/{encounterId:guid}/rad-orders")]
    [RequirePermission("rad_order.create")]
    public async Task<IActionResult> CreateRad(Guid encounterId, [FromBody] CreateRadOrdersBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRadOrdersCommand(encounterId, body.Orders), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/rad-orders
    [HttpGet("api/v1/encounters/{encounterId:guid}/rad-orders")]
    [RequirePermission("rad_order.read")]
    public async Task<IActionResult> ListRad(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListRadOrdersQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/rad-orders/{id}
    [HttpPut("api/v1/rad-orders/{id:guid}")]
    [RequirePermission("rad_order.update")]
    public async Task<IActionResult> UpdateRad(Guid id, [FromBody] UpdateOrderStatusBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRadOrderStatusCommand(id, body.Status, body.Note), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    // DELETE /api/v1/rad-orders/{id}
    [HttpDelete("api/v1/rad-orders/{id:guid}")]
    [RequirePermission("rad_order.delete")]
    public async Task<IActionResult> DeleteRad(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteRadOrderCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // GET /api/v1/encounters/{encounterId}/lab-orders/pdf
    [HttpGet("api/v1/encounters/{encounterId:guid}/lab-orders/pdf")]
    [RequirePermission("lab_order.read")]
    public async Task<IActionResult> LabOrdersPdf(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLabOrdersPdfQuery(encounterId), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "ENCOUNTER_NOT_FOUND" || result.ErrorCode == "LAB_ORDER_EMPTY" ? 404 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return File(result.Value!, "application/pdf", $"phieu-chi-dinh-xn-{encounterId:N}.pdf");
    }

    // GET /api/v1/encounters/{encounterId}/rad-orders/pdf
    [HttpGet("api/v1/encounters/{encounterId:guid}/rad-orders/pdf")]
    [RequirePermission("rad_order.read")]
    public async Task<IActionResult> RadOrdersPdf(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRadOrdersPdfQuery(encounterId), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "ENCOUNTER_NOT_FOUND" || result.ErrorCode == "RAD_ORDER_EMPTY" ? 404 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return File(result.Value!, "application/pdf", $"phieu-chi-dinh-cdha-{encounterId:N}.pdf");
    }

    // GET /api/v1/cls-catalog/tests
    [HttpGet("api/v1/cls-catalog/tests")]
    [RequirePermission("lab_order.read")]
    public async Task<IActionResult> Catalog(
        [FromQuery] string? q,
        [FromQuery] string? kind,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchClsCatalogQuery(q, kind, limit), ct);
        return Ok(new { data = result.Value });
    }
}

public record CreateLabOrdersBody(IReadOnlyList<LabOrderRequest> Tests);
public record CreateRadOrdersBody(IReadOnlyList<RadOrderRequest> Orders);
public record UpdateOrderStatusBody(string Status, string? Note);
