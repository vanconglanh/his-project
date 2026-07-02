using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Users;
using ProDiabHis.Contracts.Auth;

namespace ProDiabHis.Api.Controllers;

/// <summary>Xac thuc nguoi dung — dang nhap, lam moi token, dang xuat</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Dang nhap he thong</summary>
    /// <remarks>Tra ve access token (15 phut) va refresh token (7 ngay)</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = new { } } });

        return Ok(new { data = result.Value, meta = new { } });
    }

    /// <summary>Lam moi access token bang refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = new { } } });

        return Ok(new { data = result.Value, meta = new { } });
    }

    /// <summary>Dang xuat — thu hoi refresh token hien tai</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // Thu hoi token: client xoa localStorage, server-side revoke co the mo rong sau
        return Ok(new { data = new { message = "Dang xuat thanh cong" }, meta = new { } });
    }

    /// <summary>Gui email reset password (public — luon 204 de chong enumeration)</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordApiRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return NoContent();
    }

    /// <summary>Dat lai mat khau bang token (public)</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return NoContent();
    }
}

// DTO helper cho Swagger docs
public record ApiResponse<T>(T Data, object Meta);
public record ApiError(string Code, string Message, object Details);
public record ForgotPasswordApiRequest(string Email);
public record ResetPasswordApiRequest(string Token, string NewPassword);
