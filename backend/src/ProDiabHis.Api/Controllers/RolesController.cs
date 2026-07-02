using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Roles;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan ly vai tro, quyen va audit log</summary>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach role (SYSTEM + CUSTOM cua tenant)</summary>
    [HttpGet]
    [RequirePermission("role.read")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListRolesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    /// <summary>Tao custom role</summary>
    [HttpPost]
    [RequirePermission("role.write")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRoleCommand(
            request.Code, request.Name, request.Description, request.PermissionCodes), ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Chi tiet role</summary>
    [HttpGet("{code}")]
    [RequirePermission("role.read")]
    public async Task<IActionResult> GetRole(string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoleQuery(code), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat custom role</summary>
    [HttpPut("{code}")]
    [RequirePermission("role.write")]
    public async Task<IActionResult> UpdateRole(string code, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(
            code, request.Name, request.Description, request.PermissionCodes), ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ROLE_NOT_FOUND")
                return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode == "ROLE_SYSTEM_PROTECTED")
                return StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>Xoa custom role</summary>
    [HttpDelete("{code}")]
    [RequirePermission("role.write")]
    public async Task<IActionResult> DeleteRole(string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(code), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ROLE_NOT_FOUND")
                return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            if (result.ErrorCode == "ROLE_SYSTEM_PROTECTED")
                return StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }
}

// Request DTOs
public record CreateRoleRequest(string Code, string Name, string? Description, IEnumerable<string> PermissionCodes);
public record UpdateRoleRequest(string? Name, string? Description, IEnumerable<string>? PermissionCodes);
