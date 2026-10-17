using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Auth.Sso;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Contracts.Auth;

namespace ProDiabHis.Api.Controllers;

/// <summary>Dang nhap mot lan (SSO OIDC) voi DiaB IdentityServer — BFF confidential client</summary>
[ApiController]
[Produces("application/json")]
public class SsoController : ControllerBase
{
    /// <summary>Cookie access token — CUNG ten/thuoc tinh voi route handler FE /session/set-cookie</summary>
    public const string AccessCookie = "his-access-token";
    public const string RefreshCookie = "his-refresh-token";
    public const string BindCookie = "sso_bind";
    public const string MfaPendingCookie = "his-sso-mfa-pending";
    public const string MfaSetupCookie = "his-sso-mfa-setup";
    /// <summary>Path rong de ca /auth/refresh lan /auth/sso/* nhan duoc cookie refresh</summary>
    private const string AuthPath = "/api/v1/auth";
    private const string CookiePath = "/api/v1/auth/sso";

    private readonly IMediator _mediator;
    private readonly SsoOptions _options;
    private readonly IRateLimiter _rateLimiter;
    private readonly ISsoStateStore _store;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SsoController> _logger;

    public SsoController(IMediator mediator, SsoOptions options, IRateLimiter rateLimiter, ISsoStateStore store,
        IWebHostEnvironment env, ILogger<SsoController> logger)
    {
        _mediator = mediator; _options = options; _rateLimiter = rateLimiter; _store = store; _env = env; _logger = logger;
    }

    public record SsoConfigResponse(
        [property: JsonPropertyName("enabled")] bool Enabled,
        [property: JsonPropertyName("providerName")] string ProviderName);

    public record SsoLogoutRequest(
        [property: JsonPropertyName("refreshToken")] string? RefreshToken);

    public record SsoLogoutResponse(
        [property: JsonPropertyName("endSessionUrl")] string? EndSessionUrl);

    private string ClientIp()
        => Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
           ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private string FrontendUrl(string path)
        => (_options.FrontendBaseUrl ?? "").TrimEnd('/') + path;

    private IActionResult ErrorRedirect(string? code)
        => Redirect(FrontendUrl("/login?error=" + Uri.EscapeDataString(code ?? "SSO_IDP_ERROR")));

    private CookieOptions Cookie(TimeSpan maxAge, string path = "/") => new()
    {
        HttpOnly = true,
        Secure = !_env.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = path,
        MaxAge = maxAge
    };

    private void DeleteCookie(string name, string path)
        => Response.Cookies.Delete(name, new CookieOptions { Path = path, Secure = !_env.IsDevelopment(), HttpOnly = true, SameSite = SameSiteMode.Lax });

    /// <summary>Cau hinh SSO cho man hinh dang nhap (an/hien nut)</summary>
    [HttpGet("api/v1/auth/sso/config")]
    [AllowAnonymous]
    public IActionResult Config()
        => Ok(new { data = new SsoConfigResponse(_options.Enabled, _options.DisplayName), meta = new { } });

    /// <summary>Bat dau dang nhap SSO — 302 sang IdP</summary>
    [HttpGet("api/v1/auth/sso/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl, CancellationToken ct)
    {
        if (!_options.Enabled) return ErrorRedirect("SSO_DISABLED");
        if (!await _rateLimiter.AllowAsync("sso-ip:" + ClientIp(), 30, TimeSpan.FromMinutes(1), ct))
            return ErrorRedirect("RATE_LIMIT_EXCEEDED");

        var result = await _mediator.Send(new StartSsoLoginCommand(returnUrl), ct);
        if (!result.IsSuccess) return ErrorRedirect(result.ErrorCode);

        Response.Cookies.Append(BindCookie, result.Value!.BindValue,
            Cookie(TimeSpan.FromSeconds(result.Value.BindTtlSeconds), CookiePath));
        return Redirect(result.Value.AuthorizeUrl);
    }

    /// <summary>Callback tu IdP — luon ket thuc bang 302 ve FE</summary>
    [HttpGet("api/v1/auth/sso/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state,
        [FromQuery] string? error, CancellationToken ct)
    {
        try
        {
            if (!_options.Enabled) return ErrorRedirect("SSO_DISABLED");
            var ip = ClientIp();
            if (!await _rateLimiter.AllowAsync("sso-ip:" + ip, 30, TimeSpan.FromMinutes(1), ct))
                return ErrorRedirect("RATE_LIMIT_EXCEEDED");

            var bind = Request.Cookies[BindCookie];
            DeleteCookie(BindCookie, CookiePath);

            var result = await _mediator.Send(new CompleteSsoLoginCommand(code, state, error, bind, ip,
                Request.Headers.UserAgent.ToString()), ct);
            if (!result.IsSuccess) return ErrorRedirect(result.ErrorCode);

            var login = result.Value!.Login;
            var returnUrl = SsoReturnUrl.Sanitize(result.Value.ReturnUrl);

            if (login.Requires2fa)
            {
                Response.Cookies.Append(MfaPendingCookie, login.MfaPendingToken!, Cookie(TimeSpan.FromMinutes(5), CookiePath));
                return Redirect(FrontendUrl("/login/2fa"));
            }
            if (login.MfaSetupRequired)
            {
                Response.Cookies.Append(MfaSetupCookie, login.MfaSetupToken!, Cookie(TimeSpan.FromMinutes(10), CookiePath));
                return Redirect(FrontendUrl("/login/2fa?setup=1"));
            }

            Response.Cookies.Append(AccessCookie, login.AccessToken, Cookie(TimeSpan.FromSeconds(login.ExpiresIn)));
            Response.Cookies.Append(RefreshCookie, login.RefreshToken, Cookie(TimeSpan.FromDays(7), AuthPath));
            return Redirect(FrontendUrl(returnUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO callback loi khong du kien");
            return ErrorRedirect("SSO_IDP_ERROR");
        }
    }

    /// <summary>FE goi sau callback de lay LoginResponse tu cookie httpOnly. Xoay refresh token (giu sso_sid)
    /// va set lai cookie his-refresh-token = token MOI (path /api/v1/auth).</summary>
    [HttpPost("api/v1/auth/sso/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> Complete(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var refresh = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(refresh))
        {
            var r = await _mediator.Send(new RefreshTokenCommand(refresh), ct);
            if (!r.IsSuccess)
            {
                DeleteCookie(RefreshCookie, AuthPath);
                return Unauthorized(new { error = new { code = r.ErrorCode, message = r.ErrorMessage, details = new { } } });
            }
            Response.Cookies.Append(RefreshCookie, r.Value!.RefreshToken, Cookie(TimeSpan.FromDays(7), AuthPath));
            return Ok(new { data = r.Value, meta = new { } });
        }

        var pending = Request.Cookies[MfaPendingCookie];
        if (!string.IsNullOrEmpty(pending))
        {
            DeleteCookie(MfaPendingCookie, CookiePath);
            return Ok(new { data = new { requires2fa = true, mfaPendingToken = pending }, meta = new { } });
        }
        var setup = Request.Cookies[MfaSetupCookie];
        if (!string.IsNullOrEmpty(setup))
        {
            DeleteCookie(MfaSetupCookie, CookiePath);
            return Ok(new { data = new { mfaSetupRequired = true, mfaSetupToken = setup }, meta = new { } });
        }
        return Unauthorized(new { error = new { code = "SSO_NO_PENDING_SESSION", message = "Không có phiên SSO đang chờ", details = new { } } });
    }

    /// <summary>Dang xuat SSO — thu hoi phien hien tai, tra endSessionUrl (RP-initiated logout)</summary>
    [HttpPost("api/v1/auth/sso/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] SsoLogoutRequest? body,
        CancellationToken ct)
    {
        var refresh = body?.RefreshToken ?? Request.Cookies[RefreshCookie];
        var result = await _mediator.Send(new SsoLogoutCommand(refresh, ClientIp(), Request.Headers.UserAgent.ToString()), ct);

        DeleteCookie(AccessCookie, "/");
        DeleteCookie(RefreshCookie, AuthPath);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = new { } } });
        return Ok(new { data = new SsoLogoutResponse(result.Value!.EndSessionUrl), meta = new { } });
    }

    /// <summary>Back-channel logout tu IdP (form logout_token). Khong cookie, khong rate-limit theo user.</summary>
    [HttpPost("api/v1/auth/sso/backchannel-logout")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> BackchannelLogout([FromForm(Name = "logout_token")] string? logoutToken,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await _mediator.Send(new SsoBackchannelLogoutCommand(logoutToken, ClientIp()), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = new { } } });
        return Ok();
    }

    /// <summary>Go lien ket tai khoan SSO cua user (admin)</summary>
    [HttpDelete("api/v1/users/{id:guid}/external-logins/{provider}")]
    [Authorize]
    [RequirePermission("users.update")]
    public async Task<IActionResult> RemoveExternalLogin(Guid id, string provider, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveExternalLoginCommand(id, provider), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = new { } } });
        return NoContent();
    }
}
