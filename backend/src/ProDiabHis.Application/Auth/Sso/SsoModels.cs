using ProDiabHis.Application.Common;
using ProDiabHis.Contracts.Auth;

namespace ProDiabHis.Application.Auth.Sso;

/// <summary>Du lieu state luu Redis (GETDEL mot lan o callback)</summary>
public record SsoStateData(string Nonce, string Verifier, string ReturnUrl, string Bind);

/// <summary>Danh tinh da validate tu id_token</summary>
public record OidcIdentity(string Subject, string? Email, bool EmailVerified, string? Sid,
    IReadOnlyList<string> Amr, string IdToken);

/// <summary>Logout token da validate (back-channel)</summary>
public record OidcLogoutToken(string? Sub, string? Sid, string Jti);

/// <summary>Thong tin phien SSO gan vao refresh token + access token</summary>
public record SsoSessionInfo(string Sid, string Sub);

public record SsoStartResult(string AuthorizeUrl, string BindValue, int BindTtlSeconds);

public record SsoCompleteResult(LoginResponse Login, string ReturnUrl);

public record SsoLogoutResult(string? EndSessionUrl);

/// <summary>Client OIDC (discovery, JWKS, doi code, validate token)</summary>
public interface IOidcClient
{
    Task<string> BuildAuthorizeUrlAsync(string state, string nonce, string codeChallenge, CancellationToken ct);
    Task<Result<OidcIdentity>> ExchangeCodeAsync(string code, string codeVerifier, string expectedNonce, CancellationToken ct);
    Task<Result<OidcLogoutToken>> ValidateLogoutTokenAsync(string logoutToken, CancellationToken ct);
    Task<string?> BuildEndSessionUrlAsync(string? idTokenHint, string state, CancellationToken ct);
}

/// <summary>Kho state SSO (Redis). Khong dung Data Protection.</summary>
public interface ISsoStateStore
{
    bool IsAvailable { get; }
    Task SaveStateAsync(string state, SsoStateData data, TimeSpan ttl, CancellationToken ct);
    Task<SsoStateData?> ConsumeStateAsync(string state, CancellationToken ct);
    /// <summary>True neu jti chua tung dung (SET NX)</summary>
    Task<bool> TryMarkLogoutJtiAsync(string jti, TimeSpan ttl, CancellationToken ct);
    Task RevokeSidAsync(string sid, TimeSpan ttl, CancellationToken ct);
    Task<bool> IsSidRevokedAsync(string sid, CancellationToken ct);
    Task SaveIdTokenHintAsync(string sid, string idToken, TimeSpan ttl, CancellationToken ct);
    Task<string?> GetIdTokenHintAsync(string sid, CancellationToken ct);
    /// <summary>Luu lien ket SSO cho buoc 2FA (userId -> sid|sub)</summary>
    Task SaveMfaLinkAsync(Guid userId, SsoSessionInfo info, TimeSpan ttl, CancellationToken ct);
    Task<SsoSessionInfo?> TakeMfaLinkAsync(Guid userId, CancellationToken ct);
    /// <summary>Ticket doi phien SSO (LoginResponse) TTL ngan, dung 1 lan (GETDEL)</summary>
    Task SaveTicketAsync(string ticket, LoginResponse login, TimeSpan ttl, CancellationToken ct);
    Task<LoginResponse?> TakeTicketAsync(string ticket, CancellationToken ct);
}

/// <summary>Khi Redis khong co: SSO bao SSO_IDP_UNAVAILABLE, khong crash</summary>
public class NullSsoStateStore : ISsoStateStore
{
    public bool IsAvailable => false;
    public Task SaveStateAsync(string state, SsoStateData data, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<SsoStateData?> ConsumeStateAsync(string state, CancellationToken ct) => Task.FromResult<SsoStateData?>(null);
    public Task<bool> TryMarkLogoutJtiAsync(string jti, TimeSpan ttl, CancellationToken ct) => Task.FromResult(true);
    public Task RevokeSidAsync(string sid, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<bool> IsSidRevokedAsync(string sid, CancellationToken ct) => Task.FromResult(false);
    public Task SaveIdTokenHintAsync(string sid, string idToken, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<string?> GetIdTokenHintAsync(string sid, CancellationToken ct) => Task.FromResult<string?>(null);
    public Task SaveMfaLinkAsync(Guid userId, SsoSessionInfo info, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<SsoSessionInfo?> TakeMfaLinkAsync(Guid userId, CancellationToken ct) => Task.FromResult<SsoSessionInfo?>(null);
    public Task SaveTicketAsync(string ticket, LoginResponse login, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<LoginResponse?> TakeTicketAsync(string ticket, CancellationToken ct) => Task.FromResult<LoginResponse?>(null);
}
