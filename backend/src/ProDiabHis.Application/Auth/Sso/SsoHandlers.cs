using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Contracts.Auth;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Auth.Sso;

/// <summary>Ghi audit SSO truc tiep (luong anonymous nen IAuditService khong co tenant/user).
/// Loi audit KHONG chan luong.</summary>
internal static class SsoAudit
{
    // action VARCHAR(30): moi ma AUTH_SSO_* deu toi da 28 ky tu
    public const string LoginSuccess = "AUTH_SSO_LOGIN_SUCCESS";
    public const string LoginDenied = "AUTH_SSO_LOGIN_DENIED";
    public const string LinkCreated = "AUTH_SSO_LINK_CREATED";
    public const string LinkRemoved = "AUTH_SSO_LINK_REMOVED";
    public const string Logout = "AUTH_SSO_LOGOUT";
    public const string BackchannelLogout = "AUTH_SSO_BACKCHANNEL_LOGOUT";
    public const string MfaBypassed = "AUTH_SSO_2FA_BYPASSED_BY_AMR";

    public static void Add(IApplicationDbContext db, string action, int tenantId, Guid? userId, string? email,
        string? ip, string? ua, object details, string severity = "INFO")
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            UserEmail = email,
            Action = action,
            ResourceType = "sso",
            IpAddress = ip,
            UserAgent = ua?.Length > 500 ? ua[..500] : ua,
            DetailsJson = JsonSerializer.Serialize(details),
            Severity = severity
        });
    }

    public static async Task TrySaveAsync(IApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Ghi audit SSO that bai"); }
    }
}

public class StartSsoLoginCommandHandler : IRequestHandler<StartSsoLoginCommand, Result<SsoStartResult>>
{
    private readonly SsoOptions _options;
    private readonly IOidcClient _oidc;
    private readonly ISsoStateStore _store;
    private readonly ILogger<StartSsoLoginCommandHandler> _logger;

    public StartSsoLoginCommandHandler(SsoOptions options, IOidcClient oidc, ISsoStateStore store,
        ILogger<StartSsoLoginCommandHandler> logger)
    {
        _options = options; _oidc = oidc; _store = store; _logger = logger;
    }

    public async Task<Result<SsoStartResult>> Handle(StartSsoLoginCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SsoStartResult>.Failure("SSO_DISABLED", "Đăng nhập SSO chưa được bật");
        if (!_store.IsAvailable)
        {
            _logger.LogError("SSO can Redis nhung Redis chua cau hinh");
            return Result<SsoStartResult>.Failure("SSO_IDP_UNAVAILABLE", "Dịch vụ đăng nhập SSO tạm thời không khả dụng");
        }

        var state = SsoCrypto.RandomUrlSafe(32);
        var nonce = SsoCrypto.RandomUrlSafe(32);
        var verifier = SsoCrypto.RandomUrlSafe(32);
        var bind = SsoCrypto.RandomUrlSafe(32);
        var returnUrl = SsoReturnUrl.Sanitize(request.ReturnUrl);

        try
        {
            var url = await _oidc.BuildAuthorizeUrlAsync(state, nonce, SsoCrypto.S256(verifier), ct);
            await _store.SaveStateAsync(state, new SsoStateData(nonce, verifier, returnUrl, bind),
                TimeSpan.FromSeconds(_options.StateTtlSeconds), ct);
            return Result<SsoStartResult>.Success(new SsoStartResult(url, bind, _options.StateTtlSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Khong khoi tao duoc SSO login (discovery/redis)");
            return Result<SsoStartResult>.Failure("SSO_IDP_UNAVAILABLE", "Dịch vụ đăng nhập SSO tạm thời không khả dụng");
        }
    }
}

public class CompleteSsoLoginCommandHandler : IRequestHandler<CompleteSsoLoginCommand, Result<SsoCompleteResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly SsoOptions _options;
    private readonly IOidcClient _oidc;
    private readonly ISsoStateStore _store;
    private readonly IJwtService _jwt;
    private readonly LoginCommandHandler _login;
    private readonly ILogger<CompleteSsoLoginCommandHandler> _logger;

    public CompleteSsoLoginCommandHandler(IApplicationDbContext db, SsoOptions options, IOidcClient oidc,
        ISsoStateStore store, IJwtService jwt, LoginCommandHandler login,
        ILogger<CompleteSsoLoginCommandHandler> logger)
    {
        _db = db; _options = options; _oidc = oidc; _store = store; _jwt = jwt; _login = login; _logger = logger;
    }

    private async Task<Result<SsoCompleteResult>> DenyAsync(string code, string message, string reason,
        CancellationToken ct, CompleteSsoLoginCommand req, int tenantId = 0, User? user = null, string? sub = null)
    {
        SsoAudit.Add(_db, SsoAudit.LoginDenied, tenantId, user?.Id, user?.Email, req.IpAddress, req.UserAgent,
            new { reason, code, sub }, "WARN");
        await SsoAudit.TrySaveAsync(_db, _logger, ct);
        _logger.LogWarning("SSO login bi tu choi: {Reason}", reason);
        return Result<SsoCompleteResult>.Failure(code, message);
    }

    public async Task<Result<SsoCompleteResult>> Handle(CompleteSsoLoginCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SsoCompleteResult>.Failure("SSO_DISABLED", "Đăng nhập SSO chưa được bật");
        if (!_store.IsAvailable)
            return Result<SsoCompleteResult>.Failure("SSO_IDP_UNAVAILABLE", "Dịch vụ đăng nhập SSO tạm thời không khả dụng");

        // 1) state: GETDEL mot lan
        SsoStateData? st = null;
        if (!string.IsNullOrEmpty(request.State))
            st = await _store.ConsumeStateAsync(request.State, ct);
        if (st is null)
            return await DenyAsync("SSO_INVALID_STATE", "Phiên đăng nhập SSO không hợp lệ hoặc đã hết hạn", "invalid_state", ct, request);

        // 2) bind cookie
        if (string.IsNullOrEmpty(request.BindCookie) || !SsoCrypto.FixedEquals(request.BindCookie, st.Bind))
            return await DenyAsync("SSO_INVALID_STATE", "Phiên đăng nhập SSO không hợp lệ hoặc đã hết hạn", "bind_mismatch", ct, request);

        if (!string.IsNullOrEmpty(request.Error) || string.IsNullOrEmpty(request.Code))
            return await DenyAsync("SSO_IDP_ERROR", "Nhà cung cấp đăng nhập trả về lỗi", "idp_error:" + request.Error, ct, request);

        // 3) doi code + validate id_token
        Result<OidcIdentity> ex;
        try { ex = await _oidc.ExchangeCodeAsync(request.Code, st.Verifier, st.Nonce, ct); }
        catch (Exception e)
        {
            _logger.LogError(e, "Doi code SSO that bai");
            return Result<SsoCompleteResult>.Failure("SSO_IDP_UNAVAILABLE", "Dịch vụ đăng nhập SSO tạm thời không khả dụng");
        }
        if (!ex.IsSuccess)
            return await DenyAsync(ex.ErrorCode ?? "SSO_TOKEN_INVALID", ex.ErrorMessage ?? "Token SSO không hợp lệ", "exchange_failed", ct, request);
        var id = ex.Value!;

        // 4) map user
        var map = await ResolveUserAsync(id, ct);
        if (!map.ok)
            return await DenyAsync(map.code!, map.message!, map.reason!, ct, request, map.user?.TenantId ?? 0, map.user, id.Subject);
        var user = map.user!;
        var link = map.link!;
        link.LastLoginAt = DateTime.UtcNow;
        link.LastSid = id.Sid;

        var roleCodes = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Code).ToList();
        var effectiveSid = !string.IsNullOrEmpty(id.Sid) ? id.Sid! : "sub:" + id.Subject;
        var sessionInfo = new SsoSessionInfo(effectiveSid, id.Subject);

        // 5) gating 2FA (giong login mat khau). MFA cua IdP chi duoc tin khi TrustIdpMfa=true + amr hop le.
        var trustedAmr = _options.TrustedMfaAmrValues.Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
        var idpMfa = _options.TrustIdpMfa
            && id.Amr.Any(a => trustedAmr.Contains(a, StringComparer.OrdinalIgnoreCase));

        if (idpMfa)
        {
            SsoAudit.Add(_db, SsoAudit.MfaBypassed, user.TenantId, user.Id, user.Email, request.IpAddress,
                request.UserAgent, new { sub = id.Subject, sid = id.Sid, amr = id.Amr });
        }
        else
        {
            if (user.TwoFaEnabled)
            {
                try { await _store.SaveMfaLinkAsync(user.Id, sessionInfo, TimeSpan.FromMinutes(5), ct); }
                catch (Exception e) { _logger.LogWarning(e, "Khong luu duoc lien ket SSO cho buoc 2FA"); }
                await _db.SaveChangesAsync(ct);
                return Result<SsoCompleteResult>.Success(new SsoCompleteResult(new LoginResponse(
                    AccessToken: "", RefreshToken: "", ExpiresIn: 0,
                    User: new UserInfo(user.Id, user.Email, user.FullName, user.TenantId,
                        Array.Empty<string>(), Array.Empty<string>()),
                    Permissions: Array.Empty<string>(),
                    Requires2fa: true,
                    MfaPendingToken: _jwt.GenerateMfaPendingToken(user)), st.ReturnUrl));
            }

            var mandatory = await _login.GetMandatoryMfaRolesForSsoAsync(user.TenantId, ct);
            if (roleCodes.Any(rc => mandatory.Contains(rc, StringComparer.OrdinalIgnoreCase)))
            {
                await _db.SaveChangesAsync(ct);
                return Result<SsoCompleteResult>.Success(new SsoCompleteResult(new LoginResponse(
                    AccessToken: "", RefreshToken: "", ExpiresIn: 0,
                    User: new UserInfo(user.Id, user.Email, user.FullName, user.TenantId,
                        Array.Empty<string>(), Array.Empty<string>()),
                    Permissions: Array.Empty<string>(),
                    MfaSetupRequired: true,
                    MfaSetupMessage: "Tài khoản của bạn thuộc nhóm vai trò bắt buộc bật xác thực hai lớp (2FA). Vui lòng thiết lập 2FA ngay để tiếp tục sử dụng hệ thống.",
                    MfaSetupToken: _jwt.GenerateMfaSetupToken(user)), st.ReturnUrl));
            }
        }

        // 6) phat phien HIS (tai dung BuildSuccessResponseAsync: token + refresh + LastLoginAt + SaveChanges)
        SsoAudit.Add(_db, SsoAudit.LoginSuccess, user.TenantId, user.Id, user.Email, request.IpAddress,
            request.UserAgent, new { sub = id.Subject, sid = id.Sid, amr = id.Amr });
        var resp = await _login.BuildSuccessResponseAsync(user, ct, sessionInfo);
        if (string.IsNullOrEmpty(resp.AccessToken) || string.IsNullOrEmpty(resp.RefreshToken))
            return Result<SsoCompleteResult>.Failure("AUTH_TOKEN_ISSUE_FAILED", "Không thể phát hành phiên đăng nhập, vui lòng thử lại sau");

        try { await _store.SaveIdTokenHintAsync(effectiveSid, id.IdToken, TimeSpan.FromDays(7), ct); }
        catch (Exception e) { _logger.LogWarning(e, "Khong luu duoc id_token_hint"); }

        return Result<SsoCompleteResult>.Success(new SsoCompleteResult(resp, st.ReturnUrl));
    }

    private async Task<(bool ok, User? user, ExternalLogin? link, string? code, string? message, string? reason)>
        ResolveUserAsync(OidcIdentity id, CancellationToken ct)
    {
        var provider = _options.ProviderKey;

        // a) Lien ket da co theo (provider, subject)
        var links = await _db.ExternalLogins.IgnoreQueryFilters()
            .Where(l => l.Provider == provider && l.Subject == id.Subject && l.DeletedAt == null)
            .ToListAsync(ct);
        if (links.Count > 1)
            return (false, null, null, "SSO_AMBIGUOUS_ACCOUNT", "Tài khoản liên kết với nhiều phòng khám, vui lòng liên hệ quản trị viên", "multi_link");
        if (links.Count == 1)
        {
            var l = links[0];
            var u = await _db.Users.Include(x => x.UserRoles).ThenInclude(ur => ur.Role).IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == l.UserId, ct);
            if (u is null || u.DeletedAt != null || !u.IsActive || u.Status != UserStatus.Active)
                return (false, u, null, "SSO_USER_DISABLED", "Tài khoản HIS đã bị khóa hoặc vô hiệu hóa", "user_disabled");
            return (true, u, l, null, null, null);
        }

        // b) Map theo email (chi lan dau)
        if (_options.RequireEmailVerified && !id.EmailVerified)
            return (false, null, null, "SSO_EMAIL_NOT_VERIFIED", "Email chưa được xác minh trên tài khoản DiaB", "email_not_verified");
        if (string.IsNullOrWhiteSpace(id.Email))
            return (false, null, null, "SSO_USER_NOT_PROVISIONED", "Tài khoản chưa được cấp quyền HIS", "no_email");

        var email = id.Email.Trim().ToLowerInvariant();
        var matches = await _db.Users.Include(x => x.UserRoles).ThenInclude(ur => ur.Role).IgnoreQueryFilters()
            .Where(x => x.DeletedAt == null && x.Email.ToLower() == email)
            .ToListAsync(ct);
        if (matches.Count == 0)
            return (false, null, null, "SSO_USER_NOT_PROVISIONED", "Tài khoản chưa được cấp quyền HIS", "not_provisioned");
        var active = matches.Where(x => x.IsActive && x.Status == UserStatus.Active).ToList();
        if (active.Count == 0)
            return (false, matches[0], null, "SSO_USER_DISABLED", "Tài khoản HIS đã bị khóa hoặc vô hiệu hóa", "user_disabled");
        if (active.Count > 1)
            return (false, null, null, "SSO_AMBIGUOUS_ACCOUNT", "Email trùng ở nhiều phòng khám, vui lòng liên hệ quản trị viên", "ambiguous_email");

        var user = active[0];
        var link = new ExternalLogin
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            Provider = provider,
            Subject = id.Subject,
            EmailAtLink = email
        };
        _db.ExternalLogins.Add(link);
        SsoAudit.Add(_db, SsoAudit.LinkCreated, user.TenantId, user.Id, user.Email, null, null,
            new { provider, sub = id.Subject });
        return (true, user, link, null, null, null);
    }
}

public class SsoLogoutCommandHandler : IRequestHandler<SsoLogoutCommand, Result<SsoLogoutResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly SsoOptions _options;
    private readonly IOidcClient _oidc;
    private readonly ISsoStateStore _store;
    private readonly ILogger<SsoLogoutCommandHandler> _logger;

    public SsoLogoutCommandHandler(IApplicationDbContext db, SsoOptions options, IOidcClient oidc,
        ISsoStateStore store, ILogger<SsoLogoutCommandHandler> logger)
    {
        _db = db; _options = options; _oidc = oidc; _store = store; _logger = logger;
    }

    public async Task<Result<SsoLogoutResult>> Handle(SsoLogoutCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SsoLogoutResult>.Failure("SSO_DISABLED", "Đăng nhập SSO chưa được bật");

        string? sid = null;
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // CHI thu hoi phien hien tai (refresh token nay)
            var rt = await _db.RefreshTokens.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);
            if (rt is not null)
            {
                rt.IsRevoked = true;
                sid = rt.SsoSid;
                SsoAudit.Add(_db, SsoAudit.Logout, rt.TenantId, rt.UserId, null, request.IpAddress,
                    request.UserAgent, new { sso = sid != null, sid });
                await SsoAudit.TrySaveAsync(_db, _logger, ct);
            }
        }

        string? endSession = null;
        if (sid is not null)
        {
            try
            {
                var hint = await _store.GetIdTokenHintAsync(sid, ct);
                endSession = await _oidc.BuildEndSessionUrlAsync(hint, SsoCrypto.RandomUrlSafe(16), ct);
            }
            catch (Exception e) { _logger.LogWarning(e, "Khong tao duoc end_session url"); }
        }
        return Result<SsoLogoutResult>.Success(new SsoLogoutResult(endSession));
    }
}

public class SsoBackchannelLogoutCommandHandler : IRequestHandler<SsoBackchannelLogoutCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly SsoOptions _options;
    private readonly IOidcClient _oidc;
    private readonly ISsoStateStore _store;
    private readonly ILogger<SsoBackchannelLogoutCommandHandler> _logger;

    public SsoBackchannelLogoutCommandHandler(IApplicationDbContext db, SsoOptions options, IOidcClient oidc,
        ISsoStateStore store, ILogger<SsoBackchannelLogoutCommandHandler> logger)
    {
        _db = db; _options = options; _oidc = oidc; _store = store; _logger = logger;
    }

    public async Task<Result<int>> Handle(SsoBackchannelLogoutCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<int>.Failure("SSO_DISABLED", "Đăng nhập SSO chưa được bật");
        if (string.IsNullOrWhiteSpace(request.LogoutToken))
            return Result<int>.Failure("SSO_TOKEN_INVALID", "Thiếu logout_token");

        Result<OidcLogoutToken> v;
        try { v = await _oidc.ValidateLogoutTokenAsync(request.LogoutToken, ct); }
        catch (Exception e)
        {
            _logger.LogError(e, "Validate logout_token loi");
            return Result<int>.Failure("SSO_IDP_UNAVAILABLE", "Không xác thực được logout token");
        }
        if (!v.IsSuccess) return Result<int>.Failure(v.ErrorCode ?? "SSO_TOKEN_INVALID", v.ErrorMessage ?? "logout_token không hợp lệ");
        var lt = v.Value!;

        // chong replay
        if (!await _store.TryMarkLogoutJtiAsync(lt.Jti, TimeSpan.FromMinutes(15), ct))
            return Result<int>.Failure("SSO_TOKEN_INVALID", "logout_token đã được sử dụng");

        // CHI thu hoi phien SSO. KHONG thu hoi theo user_id -> phien mat khau (sso_sid IS NULL) khong bi anh huong.
        List<RefreshToken> sessions;
        if (!string.IsNullOrEmpty(lt.Sid))
            sessions = await _db.RefreshTokens.IgnoreQueryFilters()
                .Where(r => r.SsoSid == lt.Sid && !r.IsRevoked).ToListAsync(ct);
        else
            sessions = await _db.RefreshTokens.IgnoreQueryFilters()
                .Where(r => r.SsoSub == lt.Sub && r.SsoSid != null && !r.IsRevoked).ToListAsync(ct);

        foreach (var s in sessions) s.IsRevoked = true;

        if (_options.EnforceSidRevocation)
        {
            var sids = sessions.Select(s => s.SsoSid!).Distinct().ToList();
            if (!string.IsNullOrEmpty(lt.Sid) && !sids.Contains(lt.Sid)) sids.Add(lt.Sid);
            foreach (var sid in sids)
                await _store.RevokeSidAsync(sid, TimeSpan.FromMinutes(16), ct);
        }

        SsoAudit.Add(_db, SsoAudit.BackchannelLogout, sessions.FirstOrDefault()?.TenantId ?? 0, null, null,
            request.IpAddress, null, new { sub = lt.Sub, sid = lt.Sid, revoked = sessions.Count });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("SSO back-channel logout: thu hoi {Count} phien", sessions.Count);
        return Result<int>.Success(sessions.Count);
    }
}

public class RemoveExternalLoginCommandHandler : IRequestHandler<RemoveExternalLoginCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ITenantProvider _tenant;

    public RemoveExternalLoginCommandHandler(IApplicationDbContext db, ICurrentUser current, ITenantProvider tenant)
    {
        _db = db; _current = current; _tenant = tenant;
    }

    public async Task<Result> Handle(RemoveExternalLoginCommand request, CancellationToken ct)
    {
        // Query filter tenant ap dung (admin chi go lien ket trong tenant cua minh)
        var links = await _db.ExternalLogins
            .Where(l => l.UserId == request.UserId && l.Provider == request.Provider && l.DeletedAt == null)
            .ToListAsync(ct);
        if (links.Count == 0)
            return Result.Failure("EXTERNAL_LOGIN_NOT_FOUND", "Không tìm thấy liên kết tài khoản");

        foreach (var l in links)
        {
            l.DeletedAt = DateTime.UtcNow;
            l.DeletedBy = _current.UserId;
        }
        SsoAudit.Add(_db, SsoAudit.LinkRemoved, _tenant.TenantId, _current.UserId, _current.Email, null, null,
            new { targetUserId = request.UserId, provider = request.Provider });
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
