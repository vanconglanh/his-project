using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.CLS;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Controllers;

/// <summary>Đợt chỉ định CLS (G01/G02) - đơn vị thu tiền và gate thực hiện CLS</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class ClsRoundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClsRoundsController(IMediator mediator) => _mediator = mediator;

    private IActionResult Fail<T>(Result<T> result)
    {
        var status = result.ErrorCode switch
        {
            "CLS_ROUND_NOT_FOUND" or "ENCOUNTER_NOT_FOUND" => 404,
            "CLS_ORDER_UNPAID" => 402,
            "CLS_ROUND_NOT_OPEN" or "CLS_ROUND_ALREADY_PAID" or "CLS_ROUND_INVALID_TRANSITION" => 409,
            "CLS_ROUND_EMPTY" or "CLS_WAIVE_REASON_REQUIRED" or "BILLING_AMOUNT_MISMATCH" => 400,
            _ => 422
        };
        return StatusCode(status, new
        {
            error = new { code = result.ErrorCode, message = result.ErrorMessage, details = result.ErrorDetails }
        });
    }

    /// <summary>Tạo đợt chỉ định CLS mới cho lượt khám</summary>
    [HttpPost("encounters/{encounterId:guid}/cls-rounds")]
    [RequirePermission("cls_round.create")]
    public async Task<IActionResult> Create(Guid encounterId, [FromBody] CreateClsRoundRequest request,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateClsRoundCommand(encounterId, request), ct);
        if (!result.IsSuccess) return Fail(result);
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Danh sách đợt chỉ định CLS của lượt khám</summary>
    [HttpGet("encounters/{encounterId:guid}/cls-rounds")]
    [RequirePermission("cls_round.read")]
    public async Task<IActionResult> ListByEncounter(Guid encounterId, [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListClsRoundsQuery(encounterId, status), ct);
        if (!result.IsSuccess) return Fail(result);
        var v = result.Value!;
        return Ok(new
        {
            data = v.Rounds,
            meta = new { total = v.Total, unpaidRounds = v.UnpaidRounds, unpaidAmount = v.UnpaidAmount }
        });
    }

    /// <summary>Chi tiết 1 đợt chỉ định CLS</summary>
    [HttpGet("cls-rounds/{id:guid}")]
    [RequirePermission("cls_round.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetClsRoundQuery(id), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value });
    }

    /// <summary>Chốt đợt chỉ định (khóa thêm dịch vụ, tính lại tổng tiền)</summary>
    [HttpPost("cls-rounds/{id:guid}/submit")]
    [RequirePermission("cls_round.submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SubmitClsRoundCommand(id), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value });
    }

    /// <summary>Đánh dấu đợt chỉ định đã thanh toán</summary>
    [HttpPost("cls-rounds/{id:guid}/pay")]
    [RequirePermission("cls_round.pay")]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayClsRoundRequest? request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new PayClsRoundCommand(id, request ?? new PayClsRoundRequest(null, null, null, null)), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value });
    }

    /// <summary>Miễn / nợ viện phí đợt chỉ định (bắt buộc lý do, luôn ghi audit log)</summary>
    [HttpPost("cls-rounds/{id:guid}/waive")]
    [RequirePermission("cls_round.waive")]
    public async Task<IActionResult> Waive(Guid id, [FromBody] WaiveClsRoundRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new WaiveClsRoundCommand(id, request), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value });
    }

    /// <summary>Hủy đợt chỉ định (hủy luôn các chỉ định chưa thực hiện trong đợt)</summary>
    [HttpPost("cls-rounds/{id:guid}/cancel")]
    [RequirePermission("cls_round.cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelClsRoundRequest? request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CancelClsRoundCommand(id, request?.Reason), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value });
    }
}
