using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Branches;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan ly chi nhanh (Branch) — CRUD, dat mac dinh, gan nhan su</summary>
[ApiController]
[Route("api/v1/branches")]
[Authorize]
[Produces("application/json")]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach chi nhanh cua tenant</summary>
    [HttpGet]
    [RequirePermission("branch.read")]
    public async Task<IActionResult> ListBranches(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListBranchesQuery(isActive, q, page, pageSize), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    /// <summary>Chi tiet chi nhanh</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("branch.read")]
    public async Task<IActionResult> GetBranch(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBranchQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Tao chi nhanh moi</summary>
    [HttpPost]
    [RequirePermission("branch.create")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateBranchCommand(request), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Cap nhat thong tin chi nhanh</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("branch.update")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateBranchCommand(id, request), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BRANCH_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Bat/tat trang thai hoat dong cua chi nhanh</summary>
    [HttpPatch("{id:int}/status")]
    [RequirePermission("branch.update")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetBranchStatusRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetBranchStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BRANCH_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Dat lam chi nhanh mac dinh (tu go default cu trong 1 transaction)</summary>
    [HttpPost("{id:int}/set-default")]
    [RequirePermission("branch.update")]
    public async Task<IActionResult> SetDefault(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetDefaultBranchCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BRANCH_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Xoa mem chi nhanh — chan neu con du lieu van hanh (INV-3) hoac la default (INV-2)</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("branch.delete")]
    public async Task<IActionResult> DeleteBranch(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteBranchCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BRANCH_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Nhan su thuoc chi nhanh</summary>
    [HttpGet("{id:int}/users")]
    [RequirePermission("branch.read")]
    public async Task<IActionResult> ListUsers(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListBranchUsersQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Gan nhan su vao chi nhanh</summary>
    [HttpPost("{id:int}/users")]
    [RequirePermission("branch.assign_user")]
    public async Task<IActionResult> AssignUsers(int id, [FromBody] AssignUsersToBranchRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AssignUsersToBranchCommand(id, request), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Go nhan su khoi chi nhanh</summary>
    [HttpDelete("{id:int}/users/{userId:guid}")]
    [RequirePermission("branch.assign_user")]
    public async Task<IActionResult> RemoveUser(int id, Guid userId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new RemoveUserFromBranchCommand(id, userId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Tinh trang tuan thu BHYT/DTQG theo tung chi nhanh trong pham vi (BR-107)</summary>
    [HttpGet("bhyt-compliance")]
    [RequirePermission("branch.read")]
    public async Task<IActionResult> GetBhytCompliance(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBranchBhytComplianceQuery(), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Nhan ban (clone) chi nhanh moi tu chi nhanh nguon — chi copy cau hinh, khong copy du lieu van hanh (BR-111)</summary>
    [HttpPost("{id:int}/clone")]
    [RequirePermission("branch.create")]
    public async Task<IActionResult> CloneBranch(int id, [FromBody] CloneBranchRequest request, CancellationToken ct = default)
    {
        var req = request with { SourceBranchId = id };
        var result = await _mediator.Send(new CloneBranchCommand(req), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "BRANCH_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Checklist go-live chi nhanh (BR-112)</summary>
    [HttpGet("{id:int}/readiness")]
    [RequirePermission("branch.read")]
    public async Task<IActionResult> GetReadiness(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBranchReadinessQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Kich hoat chi nhanh (DRAFT -> ACTIVE) — chan neu chua dat checklist go-live (AC-8.1.2)</summary>
    [HttpPost("{id:int}/activate")]
    [RequirePermission("branch.update")]
    public async Task<IActionResult> ActivateBranch(int id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateBranchCommand(id), ct);
        if (!result.IsSuccess)
            return result.ErrorCode switch
            {
                "BRANCH_NOT_FOUND" => NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } }),
                "BRANCH_NOT_READY" => BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = result.ErrorDetails } }),
                _ => UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
            };
        return Ok(new { data = result.Value });
    }
}

public record SetBranchStatusRequest(bool IsActive);
