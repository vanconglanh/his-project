using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Warehouse;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;
    public SuppliersController(IMediator mediator) => _mediator = mediator;

    private int TenantId => int.Parse(User.FindFirst("tenant_id")!.Value);

    // GET /api/v1/suppliers
    [HttpGet]
    [RequirePermission("supplier.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListSuppliersQuery(TenantId, q, status, page, page_size), ct);
        var totalPages = (int)Math.Ceiling(result.Total / (double)page_size);
        return Ok(new { data = result.Items, meta = new { page, page_size, total = result.Total, total_pages = totalPages } });
    }

    // GET /api/v1/suppliers/{id}
    [HttpGet("{id}")]
    [RequirePermission("supplier.read")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSupplierQuery(id, TenantId), ct);
        if (result == null)
            return NotFound(new { error = new { code = "SUPPLIER_NOT_FOUND", message = "Không tìm thấy nhà cung cấp" } });
        return Ok(new { data = result });
    }

    // POST /api/v1/suppliers
    [HttpPost]
    [RequirePermission("supplier.write")]
    public async Task<IActionResult> Create([FromBody] SupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierCommand(TenantId, request), ct);
        return StatusCode(201, new { data = result });
    }

    // PUT /api/v1/suppliers/{id}
    [HttpPut("{id}")]
    [RequirePermission("supplier.write")]
    public async Task<IActionResult> Update(string id, [FromBody] SupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierCommand(id, TenantId, request), ct);
        if (result == null)
            return NotFound(new { error = new { code = "SUPPLIER_NOT_FOUND", message = "Không tìm thấy nhà cung cấp" } });
        return Ok(new { data = result });
    }

    // DELETE /api/v1/suppliers/{id}
    [HttpDelete("{id}")]
    [RequirePermission("supplier.write")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var found = await _mediator.Send(new DeleteSupplierCommand(id, TenantId), ct);
        if (!found)
            return NotFound(new { error = new { code = "SUPPLIER_NOT_FOUND", message = "Không tìm thấy nhà cung cấp" } });
        return NoContent();
    }
}
