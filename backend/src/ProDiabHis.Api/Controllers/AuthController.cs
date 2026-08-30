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

    /// <summary>Xac thuc ma 2FA (buoc 2 dang nhap) — dung khi login tra ve requires2fa=true</summary>
    /// <remarks>Gui mfaPendingToken (nhan tu buoc login) + ma TOTP 6 so hoac recovery code.
    /// Thanh cong tra ve access token + refresh token day du nhu login binh thuong.</remarks>
    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify2fa(
        [FromBody] Verify2faRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Verify2faLoginCommand(request.MfaPendingToken, request.Code),
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
// Global JSON policy la snake_case, nhung login RESPONSE tra "mfaPendingToken" (camelCase, do
// LoginResponse dat [JsonPropertyName] tuong minh) va FE gui lai dung ten do -> ep camelCase o day
// de request body khop, tranh loi VALIDATION_ERROR "MfaPendingToken la bat buoc".
public record Verify2faRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("mfaPendingToken")] string MfaPendingToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("code")] string Code);
public record ForgotPasswordApiRequest(string Email);
public record ResetPasswordApiRequest(string Token, string NewPassword);
