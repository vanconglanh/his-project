using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/cashier")]
[Authorize]
public class CashierController : ControllerBase
{
    private readonly IMediator _mediator;
    public CashierController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/cashier/closing/today
    [HttpGet("closing/today")]
    [RequirePermission("cashier.report")]
    public async Task<IActionResult> Today(
        [FromQuery] string? cashier_user_id,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
    {
        var userId = Guid.TryParse(cashier_user_id, out var uid) ? uid : (Guid?)null;
        var result = await _mediator.Send(new GetTodayReportQuery(userId, date), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/cashier/closing/open
    [HttpPost("closing/open")]
    [RequirePermission("cashier.shift_open")]
    public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new OpenShiftCommand(request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CASHIER_SHIFT_NOT_OPEN")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return StatusCode(201, new { data = result.Value });
    }

    // POST /api/v1/cashier/closing/close
    [HttpPost("closing/close")]
    [RequirePermission("cashier.shift_close")]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseShiftCommand(request), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "CASHIER_SHIFT_ALREADY_CLOSED" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "CASHIER_CASH_DIFFERENCE" => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/cashier/closing/history
    [HttpGet("closing/history")]
    [RequirePermission("cashier.report")]
    public async Task<IActionResult> History(
        [FromQuery] string? cashier_user_id,
        [FromQuery] DateOnly? from_date,
        [FromQuery] DateOnly? to_date,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var userId = Guid.TryParse(cashier_user_id, out var uid) ? uid : (Guid?)null;
        var result = await _mediator.Send(new ListShiftsQuery(userId, from_date, to_date, status, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // GET /api/v1/cashier/closing/{id}/pdf
    [HttpGet("closing/{id:guid}/pdf")]
    [RequirePermission("cashier.report")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportShiftPdfQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"shift-{id}.pdf");
    }

    /// <summary>Lay thong tin ca lam viec hien tai cua thu ngan dang dang nhap.</summary>
    // GET /api/v1/cashier/shift
    [HttpGet("shift")]
    [RequirePermission("cashier.report")]
    public async Task<IActionResult> GetCurrentShift(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCurrentShiftQuery(), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    /// <summary>In bien lai K80 (thermal PDF) cho giao dich thu tien + ghi audit log</summary>
    // POST /api/v1/cashier/receipts/{paymentId}/print
    [HttpPost("receipts/{paymentId:guid}/print")]
    [RequirePermission("cashier.print_receipt")]
    public async Task<IActionResult> PrintReceipt(Guid paymentId, [FromBody] PrintReceiptRequest? request, CancellationToken ct)
    {
        var req = request ?? new PrintReceiptRequest();
        var result = await _mediator.Send(new PrintReceiptCommand(paymentId, req), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "PAYMENT_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "PAYMENT_VOIDED" => Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => Problem(result.ErrorMessage, statusCode: 400)
            };
        }

        var value = result.Value!;
        Response.Headers["Content-Disposition"] = $"inline; filename=\"receipt-{value.ReceiptNo}.pdf\"";
        return File(value.PdfBytes, "application/pdf");
    }

    // GET /api/v1/cashier/debts
    [HttpGet("debts")]
    [RequirePermission("cashier.debt_view")]
    public async Task<IActionResult> Debts(
        [FromQuery] string? q,
        [FromQuery] int? older_than_days,
        [FromQuery] decimal min_balance = 1,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDebtsQuery(q, min_balance, older_than_days, page, Math.Min(page_size, 100)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var (items, total, totalDebt) = result.Value;
        return Ok(new
        {
            data = items,
            meta = new { page, page_size, total, total_pages = (int)Math.Ceiling(total / (double)page_size) },
            summary = new { total_patients = total, total_debt = totalDebt }
        });
    }
}
