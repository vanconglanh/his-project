using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ProDiabHis.Application.Auth.Sso;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Sso;

/// <summary>Client OIDC cho BFF: discovery + JWKS cache (ConfigurationManager), doi code, validate token.
/// KHONG luu token IdP.</summary>
public class OidcClient : IOidcClient
{
    public const string HttpClientName = "SsoOidc";

    private readonly SsoOptions _options;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<OidcClient> _logger;
    private readonly Lazy<ConfigurationManager<OpenIdConnectConfiguration>> _cfg;

    public OidcClient(SsoOptions options, IHttpClientFactory http, ILogger<OidcClient> logger)
    {
        _options = options; _http = http; _logger = logger;
        _cfg = new Lazy<ConfigurationManager<OpenIdConnectConfiguration>>(() =>
        {
            var authority = _options.Authority.TrimEnd('/');
            var retriever = new HttpDocumentRetriever(_http.CreateClient(HttpClientName))
            {
                RequireHttps = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            };
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                authority + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(), retriever);
        });
    }

    private Task<OpenIdConnectConfiguration> GetConfigAsync(CancellationToken ct)
        => _cfg.Value.GetConfigurationAsync(ct);

    public async Task<string> BuildAuthorizeUrlAsync(string state, string nonce, string codeChallenge, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        var q = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["scope"] = _options.Scopes,
            ["redirect_uri"] = _options.RedirectUri,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };
        return cfg.AuthorizationEndpoint + (cfg.AuthorizationEndpoint.Contains('?') ? "&" : "?")
            + string.Join("&", q.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
    }

    public async Task<Result<OidcIdentity>> ExchangeCodeAsync(string code, string codeVerifier, string expectedNonce, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        using var client = _http.CreateClient(HttpClientName);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = _options.RedirectUri,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });
        using var resp = await client.PostAsync(cfg.TokenEndpoint, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            // KHONG log body (co the chua thong tin nhay cam)
            _logger.LogWarning("SSO token endpoint tra ve {Status}", (int)resp.StatusCode);
            return Result<OidcIdentity>.Failure("SSO_IDP_ERROR", "Nhà cung cấp đăng nhập trả về lỗi");
        }

        string? idToken;
        try
        {
            using var doc = JsonDocument.Parse(body);
            idToken = doc.RootElement.TryGetProperty("id_token", out var t) ? t.GetString() : null;
        }
        catch (JsonException)
        {
            return Result<OidcIdentity>.Failure("SSO_IDP_ERROR", "Nhà cung cấp đăng nhập trả về lỗi");
        }
        if (string.IsNullOrEmpty(idToken))
            return Result<OidcIdentity>.Failure("SSO_TOKEN_INVALID", "Token đăng nhập SSO không hợp lệ");

        var (result, err) = await OidcTokenValidator.ValidateIdTokenAsync(idToken, cfg, _options, expectedNonce, _logger);
        if (!result.IsSuccess && OidcTokenValidator.IsKeyProblem(err))
        {
            // kid la -> yeu cau refresh JWKS (ConfigurationManager gioi han tan suat) roi thu lai 1 lan
            _cfg.Value.RequestRefresh();
            cfg = await GetConfigAsync(ct);
            (result, _) = await OidcTokenValidator.ValidateIdTokenAsync(idToken, cfg, _options, expectedNonce, _logger);
        }
        return result;
    }

    public async Task<Result<OidcLogoutToken>> ValidateLogoutTokenAsync(string logoutToken, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        var (result, err) = await OidcTokenValidator.ValidateLogoutTokenAsync(logoutToken, cfg, _options, _logger);
        if (!result.IsSuccess && OidcTokenValidator.IsKeyProblem(err))
        {
            _cfg.Value.RequestRefresh();
            cfg = await GetConfigAsync(ct);
            (result, _) = await OidcTokenValidator.ValidateLogoutTokenAsync(logoutToken, cfg, _options, _logger);
        }
        return result;
    }

    public async Task<string?> BuildEndSessionUrlAsync(string? idTokenHint, string state, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        if (string.IsNullOrEmpty(cfg.EndSessionEndpoint)) return null;
        var q = new List<string>();
        if (!string.IsNullOrEmpty(idTokenHint)) q.Add("id_token_hint=" + Uri.EscapeDataString(idTokenHint));
        if (!string.IsNullOrEmpty(_options.PostLogoutRedirectUri))
            q.Add("post_logout_redirect_uri=" + Uri.EscapeDataString(_options.PostLogoutRedirectUri));
        q.Add("state=" + Uri.EscapeDataString(state));
        return cfg.EndSessionEndpoint + (cfg.EndSessionEndpoint.Contains('?') ? "&" : "?") + string.Join("&", q);
    }
}
