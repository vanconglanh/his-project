using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Me;

namespace ProDiabHis.Api.Controllers;

/// <summary>Thong tin phien lam viec cua user hien tai (branch context, doi chi nhanh)</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
[Produces("application/json")]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator) => _mediator = mediator;

    /// <summary>Chi nhanh dang lam viec + danh sach chi nhanh duoc phep xem — FE dung de render dropdown</summary>
    [HttpGet("branch-context")]
    public async Task<IActionResult> GetBranchContext(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBranchContextQuery(), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Doi chi nhanh dang lam viec — cap access token moi voi branch_id cap nhat</summary>
    [HttpPost("switch-branch")]
    public async Task<IActionResult> SwitchBranch([FromBody] SwitchBranchRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SwitchBranchCommand(request.BranchId), ct);
        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "BRANCH_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "BRANCH_ACCESS_DENIED" => StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                _ => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
            };
        return Ok(new { data = result.Value });
    }
}
