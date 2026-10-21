using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Controllers;

/// <summary>BM-95: quan ly voucher giam gia (QTV). Ap voucher vao hoa don o BillingsController.</summary>
[ApiController]
[Route("api/v1/vouchers")]
[Authorize]
public class VouchersController : ControllerBase
{
    private readonly IMediator _mediator;
    public VouchersController(IMediator mediator) => _mediator = mediator;

    private IActionResult Error(string? code, string? message) => code switch
    {
        "VOUCHER_NOT_FOUND" => NotFound(new { error = new { code, message } }),
        "VOUCHER_CODE_DUPLICATE" => Conflict(new { error = new { code, message } }),
        _ => BadRequest(new { error = new { code, message } })
    };

    /// <summary>Danh sach voucher (phan trang, loc is_active)</summary>
    [HttpGet]
    [RequirePermission("voucher.read")]
    public async Task<IActionResult> List([FromQuery] bool? is_active, [FromQuery] int page = 1,
        [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListVouchersQuery(is_active, page, page_size), ct);
        if (!result.IsSuccess) return Error(result.ErrorCode, result.ErrorMessage);
        var p = result.Value!;
        return Ok(new { data = p.Items, meta = new { page = p.Page, page_size = p.PageSize, total = p.Total } });
    }

    /// <summary>Chi tiet voucher</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("voucher.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVoucherQuery(id), ct);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Error(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>Tao voucher</summary>
    [HttpPost]
    [RequirePermission("voucher.write")]
    public async Task<IActionResult> Create([FromBody] CreateVoucherRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateVoucherCommand(request), ct);
        return result.IsSuccess ? StatusCode(201, new { data = result.Value }) : Error(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>Cap nhat voucher (khong doi duoc code)</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("voucher.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVoucherRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateVoucherCommand(id, request), ct);
        return result.IsSuccess ? Ok(new { data = result.Value }) : Error(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>Xoa mem voucher</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("voucher.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteVoucherCommand(id), ct);
        return result.IsSuccess ? NoContent() : Error(result.ErrorCode, result.ErrorMessage);
    }
}
