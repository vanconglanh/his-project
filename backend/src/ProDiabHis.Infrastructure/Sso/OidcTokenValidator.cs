using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ProDiabHis.Application.Auth.Sso;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Sso;

/// <summary>Validate id_token / logout_token theo cau hinh discovery (issuer + JWKS).
/// Issuer lay tu discovery (config.Issuer) + Sso__ValidIssuers bo sung.</summary>
public static class OidcTokenValidator
{
    public const string BackchannelEvent = "http://schemas.openid.net/event/backchannel-logout";
    private static readonly string[] Rs256 = { SecurityAlgorithms.RsaSha256 };

    private static string[] BuildValidIssuers(OpenIdConnectConfiguration cfg, SsoOptions opts)
    {
        var set = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg.Issuer)) set.Add(cfg.Issuer);
        foreach (var i in opts.ValidIssuers ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(i) && !set.Contains(i, StringComparer.Ordinal)) set.Add(i);
        return set.ToArray();
    }

    private static TokenValidationParameters Params(OpenIdConnectConfiguration cfg, SsoOptions opts, bool lifetime)
        => new()
        {
            ValidateIssuer = true,
            ValidIssuers = BuildValidIssuers(cfg, opts),
            ValidateAudience = true,
            ValidAudience = opts.ClientId,
            ValidateLifetime = lifetime,
            RequireExpirationTime = lifetime,
            ClockSkew = TimeSpan.FromMinutes(2),
            RequireSignedTokens = true,
            // Van VERIFY chu ky bang IssuerSigningKeys (JWKS). ValidateIssuerSigningKey chi kiem
            // them han/chain cua X509 cert -> tat khi IgnoreSigningCertExpiry (cert IS het han 2020).
            ValidateIssuerSigningKey = !opts.IgnoreSigningCertExpiry,
            IssuerSigningKeys = cfg.SigningKeys,
            ValidAlgorithms = Rs256
        };

    /// <summary>True neu loi la do khong tim thay / sai khoa (co the do xoay khoa -> refresh JWKS roi thu lai)</summary>
    public static bool IsKeyProblem(Exception? ex)
        => ex is SecurityTokenSignatureKeyNotFoundException || ex is SecurityTokenInvalidSignatureException;

    /// <summary>Decode header+payload KHONG validate chu ky, de log chan doan an toan (khong log chu ky/token).</summary>
    private static void LogTokenDiag(ILogger? logger, string tokenKind, string token, OpenIdConnectConfiguration cfg, SsoOptions opts, Exception? ex)
    {
        if (logger is null) return;
        try
        {
            var jwt = new JsonWebToken(token);
            string? Claim(string t) => jwt.Claims.FirstOrDefault(c => c.Type == t)?.Value;
            var kids = string.Join(",", cfg.SigningKeys.Select(k => k.KeyId));
            logger.LogWarning(
                "SSO {Kind} validate FAIL exType={ExType} exMsg={ExMsg} | token.alg={Alg} token.kid={Kid} token.iss={Iss} token.aud={Aud} nonce_present={HasNonce} sub_present={HasSub} | expect.issuers=[{ValidIssuers}] expect.aud={Aud2} jwks.kids=[{JwksKids}]",
                tokenKind,
                ex?.GetType().Name ?? "(none)",
                ex?.Message ?? "(none)",
                jwt.Alg,
                jwt.Kid,
                Claim("iss"),
                Claim("aud"),
                !string.IsNullOrEmpty(Claim("nonce")),
                !string.IsNullOrEmpty(Claim("sub")),
                string.Join(",", BuildValidIssuers(cfg, opts)),
                opts.ClientId,
                kids);
        }
        catch (Exception e) { logger.LogWarning("SSO {Kind} diag decode loi: {Msg}", tokenKind, e.Message); }
    }

    public static Task<(Result<OidcIdentity> result, Exception? error)> ValidateIdTokenAsync(
        string idToken, OpenIdConnectConfiguration cfg, SsoOptions opts, string expectedNonce)
        => ValidateIdTokenAsync(idToken, cfg, opts, expectedNonce, null);

    public static async Task<(Result<OidcIdentity> result, Exception? error)> ValidateIdTokenAsync(
        string idToken, OpenIdConnectConfiguration cfg, SsoOptions opts, string expectedNonce, ILogger? logger)
    {
        var bad = Result<OidcIdentity>.Failure("SSO_TOKEN_INVALID", "Token đăng nhập SSO không hợp lệ");
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var r = await handler.ValidateTokenAsync(idToken, Params(cfg, opts, true));
        if (!r.IsValid || r.SecurityToken is not JsonWebToken jwt)
        {
            LogTokenDiag(logger, "id_token", idToken, cfg, opts, r.Exception);
            return (bad, r.Exception);
        }

        var nonce = jwt.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
        if (string.IsNullOrEmpty(nonce) || !SsoCrypto.FixedEquals(nonce, expectedNonce))
        {
            logger?.LogWarning("SSO id_token validate FAIL branch=nonce nonce_present={P} match=false", !string.IsNullOrEmpty(nonce));
            return (bad, null);
        }

        var azp = jwt.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
        if (!string.IsNullOrEmpty(azp) && azp != opts.ClientId)
        {
            logger?.LogWarning("SSO id_token validate FAIL branch=azp azp={Azp} clientId={Cid}", azp, opts.ClientId);
            return (bad, null);
        }

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            logger?.LogWarning("SSO id_token validate FAIL branch=sub (thieu sub)");
            return (bad, null);
        }

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var ev = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
        var emailVerified = string.Equals(ev, "true", StringComparison.OrdinalIgnoreCase);
        var sid = jwt.Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
        var amr = jwt.Claims.Where(c => c.Type == "amr").Select(c => c.Value).ToList();

        return (Result<OidcIdentity>.Success(new OidcIdentity(sub, email, emailVerified, sid, amr, idToken)), null);
    }

    public static Task<(Result<OidcLogoutToken> result, Exception? error)> ValidateLogoutTokenAsync(
        string logoutToken, OpenIdConnectConfiguration cfg, SsoOptions opts)
        => ValidateLogoutTokenAsync(logoutToken, cfg, opts, null);

    public static async Task<(Result<OidcLogoutToken> result, Exception? error)> ValidateLogoutTokenAsync(
        string logoutToken, OpenIdConnectConfiguration cfg, SsoOptions opts, ILogger? logger)
    {
        var bad = Result<OidcLogoutToken>.Failure("SSO_TOKEN_INVALID", "logout_token không hợp lệ");
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        // logout_token co the khong co exp -> tu kiem tra iat
        var r = await handler.ValidateTokenAsync(logoutToken, Params(cfg, opts, false));
        if (!r.IsValid || r.SecurityToken is not JsonWebToken jwt)
        {
            LogTokenDiag(logger, "logout_token", logoutToken, cfg, opts, r.Exception);
            return (bad, r.Exception);
        }

        // iat gan (+-5 phut)
        var iat = jwt.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;
        if (!long.TryParse(iat, out var iatSec)) return (bad, null);
        var diff = Math.Abs((DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(iatSec)).TotalMinutes);
        if (diff > 5) return (bad, null);

        // events phai chua backchannel-logout
        var events = jwt.Claims.FirstOrDefault(c => c.Type == "events")?.Value;
        if (string.IsNullOrEmpty(events)) return (bad, null);
        try
        {
            using var doc = JsonDocument.Parse(events);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty(BackchannelEvent, out _))
                return (bad, null);
        }
        catch (JsonException) { return (bad, null); }

        // KHONG duoc co nonce
        if (jwt.Claims.Any(c => c.Type == "nonce")) return (bad, null);

        var jti = jwt.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        if (string.IsNullOrEmpty(jti)) return (bad, null);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var sid = jwt.Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
        if (string.IsNullOrEmpty(sub) && string.IsNullOrEmpty(sid)) return (bad, null);

        return (Result<OidcLogoutToken>.Success(new OidcLogoutToken(sub, sid, jti)), null);
    }
}
