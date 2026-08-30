using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Branches;

namespace ProDiabHis.Api.Controllers;

/// <summary>Chuyen co so noi bo — chuyen benh nhan giua 2 chi nhanh cung to chuc (BR-29)</summary>
[ApiController]
[Route("api/v1/internal-referrals")]
[Authorize]
[Produces("application/json")]
public class InternalReferralsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalReferralsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Tao phieu chuyen co so noi bo (trang thai SENT, chi nhanh nguon = chi nhanh hien tai)</summary>
    [HttpPost]
    [RequirePermission("internal_referral.write")]
    public async Task<IActionResult> Create([FromBody] CreateInternalReferralRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateInternalReferralCommand(request), ct);
        if (!result.IsSuccess)
            return result.ErrorCode is "BRANCH_NOT_FOUND" or "PATIENT_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Danh sach phieu chuyen den/di lien quan chi nhanh trong pham vi (mac dinh SENT/ACCEPTED)</summary>
    [HttpGet("incoming")]
    [RequirePermission("internal_referral.read")]
    public async Task<IActionResult> ListIncoming([FromQuery] string? status, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListIncomingInternalReferralsQuery(status), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat trang thai phieu chuyen co so (ACCEPTED/COMPLETED/CANCELLED)</summary>
    [HttpPatch("{id:int}/status")]
    [RequirePermission("internal_referral.write")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInternalReferralStatusRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateInternalReferralStatusCommand(id, request), ct);
        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "REFERRAL_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "BRANCH_ACCESS_DENIED" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
            };
        return Ok(new { data = result.Value });
    }
}
