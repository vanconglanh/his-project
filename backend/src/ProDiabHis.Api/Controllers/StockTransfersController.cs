using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.StockTransfers;

namespace ProDiabHis.Api.Controllers;

/// <summary>E/Dot3 - Dieu chuyen kho noi bo giua chi nhanh (muc 4.2 BRD, BR-51..BR-60).</summary>
[ApiController]
[Authorize]
[Route("api/v1/stock-transfers")]
public class StockTransfersController : ControllerBase
{
    private readonly IMediator _mediator;
    public StockTransfersController(IMediator mediator) => _mediator = mediator;

    private static IActionResult FromResult<T>(ProDiabHis.Application.Common.Result<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                StockTransferErrors.NotFound => StatusCodes.Status404NotFound,
                StockTransferErrors.SelfApproval => StatusCodes.Status403Forbidden,
                StockTransferErrors.ApprovalPermissionRequired => StatusCodes.Status403Forbidden,
                StockTransferErrors.BranchAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return new ObjectResult(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = result.ErrorDetails } })
            { StatusCode = status };
        }
        return onSuccess != null ? onSuccess(result.Value!) : new OkObjectResult(new { data = result.Value });
    }

    [HttpGet]
    [RequirePermission("stock_transfer.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] int? branch_id,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListStockTransfersQuery(status, branch_id, page, page_size), ct);
        if (!result.IsSuccess) return FromResult(result);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpGet("{id}")]
    [RequirePermission("stock_transfer.read")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockTransferQuery(id), ct);
        return FromResult(result);
    }

    [HttpPost]
    [RequirePermission("stock_transfer.create")]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateStockTransferCommand(request), ct);
        return FromResult(result, v => Created($"/api/v1/stock-transfers/{v.Id}", new { data = v }));
    }

    [HttpPost("{id}/submit")]
    [RequirePermission("stock_transfer.create")]
    public async Task<IActionResult> Submit(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitStockTransferCommand(id), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/approve")]
    [RequirePermission("stock_transfer.approve")]
    public async Task<IActionResult> Approve(string id, [FromBody] ApproveStockTransferRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveStockTransferCommand(id, request ?? new ApproveStockTransferRequest()), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/reject")]
    [RequirePermission("stock_transfer.approve")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectStockTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectStockTransferCommand(id, request), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/ship")]
    [RequirePermission("stock_transfer.ship")]
    public async Task<IActionResult> Ship(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ShipStockTransferCommand(id), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/receive")]
    [RequirePermission("stock_transfer.receive")]
    public async Task<IActionResult> Receive(string id, [FromBody] ReceiveStockTransferRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceiveStockTransferCommand(id, request ?? new ReceiveStockTransferRequest(Array.Empty<ReceiveItemRequest>(), null)), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/partial-receive")]
    [RequirePermission("stock_transfer.receive")]
    public async Task<IActionResult> PartialReceive(string id, [FromBody] ReceiveStockTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new PartialReceiveStockTransferCommand(id, request), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/close")]
    [RequirePermission("stock_transfer.receive")]
    public async Task<IActionResult> Close(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseStockTransferCommand(id), ct);
        return FromResult(result);
    }

    [HttpPost("{id}/cancel")]
    [RequirePermission("stock_transfer.create")]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelStockTransferCommand(id), ct);
        return FromResult(result);
    }
}
