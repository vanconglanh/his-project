using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Users;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan ly nguoi dung trong tenant</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach user trong tenant</summary>
    [HttpGet]
    [RequirePermission("user.read")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListUsersQuery(page, page_size, role, status, q), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    /// <summary>Moi user moi qua email</summary>
    [HttpPost("invite")]
    [RequirePermission("user.invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new InviteUserCommand(
            request.Email, request.FullName, request.Phone, request.RoleCodes), ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return StatusCode(201, new
        {
            data = new
            {
                user_id = result.Value!.UserId,
                email = result.Value.Email,
                invite_expires_at = result.Value.InviteExpiresAt
            }
        });
    }

    /// <summary>Chap nhan loi moi va dat mat khau (public)</summary>
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AcceptInviteCommand(request.Token, request.Password, request.FullName), ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new
        {
            data = new
            {
                user = result.Value!.User,
                access_token = result.Value.AccessToken,
                refresh_token = result.Value.RefreshToken
            }
        });
    }

    /// <summary>Chi tiet user</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("user.read")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat profile user</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("user.write")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, request.FullName, request.Phone, request.AvatarUrl), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "USER_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Xoa mem user</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("user.delete")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Gan role cho user</summary>
    [HttpPost("{id:guid}/roles")]
    [RequirePermission("user.assign_role")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignRolesCommand(id, request.RoleCodes), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Thu hoi 1 role</summary>
    [HttpDelete("{id:guid}/roles/{roleCode}")]
    [RequirePermission("user.assign_role")]
    public async Task<IActionResult> RevokeRole(Guid id, string roleCode, CancellationToken ct)
    {
        var result = await _mediator.Send(new RevokeRoleCommand(id, roleCode), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Khoa user</summary>
    [HttpPost("{id:guid}/disable")]
    [RequirePermission("user.write")]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DisableUserCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Mo khoa user</summary>
    [HttpPost("{id:guid}/enable")]
    [RequirePermission("user.write")]
    public async Task<IActionResult> EnableUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new EnableUserCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Profile cua user dang dang nhap</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMeQuery(), ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat profile ban than</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateMeCommand(request.FullName, request.Phone, request.AvatarUrl), ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Doi mat khau</summary>
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(request.OldPassword, request.NewPassword), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Khoi tao TOTP 2FA</summary>
    [HttpPost("me/2fa/setup")]
    public async Task<IActionResult> Setup2FA(CancellationToken ct)
    {
        var result = await _mediator.Send(new Setup2FACommand(), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new
        {
            data = new
            {
                secret = result.Value!.Secret,
                otpauth_url = result.Value.OtpauthUrl,
                qr_png_base64 = result.Value.QrPngBase64
            }
        });
    }

    /// <summary>Kich hoat 2FA sau khi xac minh TOTP code</summary>
    [HttpPost("me/2fa/enable")]
    public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new Enable2FACommand(request.Code), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = new { recovery_codes = result.Value!.RecoveryCodes } });
    }

    /// <summary>Tat 2FA</summary>
    [HttpPost("me/2fa/disable")]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new Disable2FACommand(request.Password, request.Code), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}

// Request DTOs
public record InviteUserRequest(string Email, string FullName, string? Phone, IEnumerable<string> RoleCodes);
public record AcceptInviteRequest(string Token, string Password, string? FullName);
public record UpdateUserRequest(string? FullName, string? Phone, string? AvatarUrl);
public record UpdateMeRequest(string? FullName, string? Phone, string? AvatarUrl);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record AssignRolesRequest(IEnumerable<string> RoleCodes);
public record Enable2FARequest(string Code);
public record Disable2FARequest(string Password, string? Code);
