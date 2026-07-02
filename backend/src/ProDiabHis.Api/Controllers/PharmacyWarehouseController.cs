using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Warehouse;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class PharmacyWarehouseController : ControllerBase
{
    private readonly IMediator _mediator;
    public PharmacyWarehouseController(IMediator mediator) => _mediator = mediator;

    // ─── Warehouses ───────────────────────────────────────────────────────────
    [HttpGet("api/v1/pharmacy/warehouses")]
    [RequirePermission("warehouse.read")]
    public async Task<IActionResult> ListWarehouses(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListWarehousesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost("api/v1/pharmacy/warehouses")]
    [RequirePermission("warehouse.write")]
    public async Task<IActionResult> CreateWarehouse([FromBody] WarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWarehouseCommand(request), ct);
        return Created("", new { data = result.Value });
    }

    [HttpGet("api/v1/pharmacy/warehouses/{id}")]
    [RequirePermission("warehouse.read")]
    public async Task<IActionResult> GetWarehouse(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWarehouseQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpPut("api/v1/pharmacy/warehouses/{id}")]
    [RequirePermission("warehouse.write")]
    public async Task<IActionResult> UpdateWarehouse(string id, [FromBody] WarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWarehouseCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpDelete("api/v1/pharmacy/warehouses/{id}")]
    [RequirePermission("warehouse.write")]
    public async Task<IActionResult> DeleteWarehouse(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteWarehouseCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // ─── Purchase Orders ──────────────────────────────────────────────────────
    [HttpGet("api/v1/pharmacy/purchase-orders")]
    [RequirePermission("warehouse.read")]
    public async Task<IActionResult> ListPurchaseOrders(
        [FromQuery] string? status, [FromQuery] string? supplier_id,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPurchaseOrdersQuery(status, supplier_id, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpPost("api/v1/pharmacy/purchase-orders")]
    [RequirePermission("warehouse.write")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePurchaseOrderCommand(request), ct);
        return Created("", new { data = result.Value });
    }

    [HttpPost("api/v1/pharmacy/purchase-orders/{id}/grn")]
    [RequirePermission("warehouse.write")]
    public async Task<IActionResult> CreateGrn(string id, [FromBody] GoodsReceivedRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateGrnCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Created("", new { data = result.Value });
    }

    // ─── Stocks (plural /stocks + singular /stock alias) ─────────────────────
    [HttpGet("api/v1/pharmacy/stocks")]
    [HttpGet("api/v1/pharmacy/stock")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> ListStocks(
        [FromQuery] string? drug_id, [FromQuery] string? warehouse_id, [FromQuery] string? batch_no,
        [FromQuery] bool? low_stock, [FromQuery] bool? near_expiry,
        [FromQuery] int? days,
        [FromQuery] int page = 1, [FromQuery] int page_size = 50, CancellationToken ct = default)
    {
        // near_expiry=true + days=30/60/90 tich hop vao query filter
        var nearExp = near_expiry ?? (days.HasValue ? true : (bool?)null);
        var result = await _mediator.Send(
            new ListStocksStringIdQuery(warehouse_id, drug_id, batch_no, low_stock, nearExp, days ?? 60, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpGet("api/v1/pharmacy/stock/{id}")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> GetStockById(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockByIdQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpGet("api/v1/pharmacy/stock/low")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> LowStockList([FromQuery] string? warehouse_id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLowStockStringIdQuery(warehouse_id), ct);
        return Ok(new { data = result.Value });
    }

    [HttpGet("api/v1/pharmacy/stock/near-expiry")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> NearExpiryList([FromQuery] int days = 30, [FromQuery] string? warehouse_id = null, CancellationToken ct = default)
    {
        if (days != 30 && days != 60 && days != 90) days = 30;
        var result = await _mediator.Send(new GetNearExpiryStringIdQuery(days, warehouse_id), ct);
        return Ok(new { data = result.Value });
    }

    // ─── Adjustments ──────────────────────────────────────────────────────────
    [HttpPost("api/v1/pharmacy/adjustments")]
    [RequirePermission("stock.adjust")]
    public async Task<IActionResult> CreateAdjustment([FromBody] StockAdjustmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAdjustmentCommand(request), ct);
        return Created("", new { data = result.Value });
    }

    // ─── Movements ────────────────────────────────────────────────────────────
    [HttpGet("api/v1/pharmacy/movements")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> ListMovements(
        [FromQuery] int? warehouse_id, [FromQuery] int? drug_id, [FromQuery] string? movement_type,
        [FromQuery] DateOnly? from_date, [FromQuery] DateOnly? to_date,
        [FromQuery] int page = 1, [FromQuery] int page_size = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListMovementsQuery(warehouse_id, drug_id, movement_type, from_date, to_date, page, page_size), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // ─── Transfers ────────────────────────────────────────────────────────────
    [HttpPost("api/v1/pharmacy/transfers")]
    [RequirePermission("stock.adjust")]
    public async Task<IActionResult> CreateTransfer([FromBody] TransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateTransferCommand(request), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Created("", new { data = result.Value });
    }

    // ─── Alerts ───────────────────────────────────────────────────────────────
    [HttpGet("api/v1/pharmacy/alerts/low-stock")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> LowStockAlerts([FromQuery] int? warehouse_id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLowStockAlertsQuery(warehouse_id), ct);
        return Ok(new { data = result.Value });
    }

    [HttpGet("api/v1/pharmacy/alerts/near-expiry")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> NearExpiryAlerts([FromQuery] int days = 60, [FromQuery] int? warehouse_id = null, CancellationToken ct = default)
    {
        if (days != 30 && days != 60 && days != 90) days = 60;
        var result = await _mediator.Send(new GetNearExpiryAlertsQuery(days, warehouse_id), ct);
        return Ok(new { data = result.Value });
    }

    // ─── Lots (stub — danh sach lo thuoc) ────────────────────────────────────
    [HttpGet("api/v1/pharmacy/lots")]
    [RequirePermission("stock.read")]
    public IActionResult ListLots(
        [FromQuery] string? drug_id,
        [FromQuery] string? warehouse_id,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20)
    {
        // Stub — lot tracking chua implement day du; tra empty de FE khong 404
        return Ok(new { data = Array.Empty<object>(), meta = new { page, page_size, total = 0, total_pages = 0 } });
    }

    // ─── Stocktake PDF ────────────────────────────────────────────────────────
    [HttpGet("api/v1/pharmacy/stocktake")]
    [RequirePermission("stock.read")]
    public async Task<IActionResult> StocktakePdf([FromQuery] int warehouse_id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStocktakePdfQuery(warehouse_id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"stocktake-{warehouse_id}-{DateTime.Today:yyyyMMdd}.pdf");
    }
}
