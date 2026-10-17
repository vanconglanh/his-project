using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Auth.Sso;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using ProDiabHis.Infrastructure.Sso;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

/// <summary>Kho state SSO in-memory cho test (GETDEL thuc su)</summary>
internal class FakeSsoStateStore : ISsoStateStore
{
    public bool IsAvailable { get; set; } = true;
    private readonly Dictionary<string, SsoStateData> _states = new();
    private readonly HashSet<string> _jtis = new();
    public HashSet<string> RevokedSids { get; } = new();
    public Dictionary<Guid, SsoSessionInfo> MfaLinks { get; } = new();

    public Task SaveStateAsync(string state, SsoStateData data, TimeSpan ttl, CancellationToken ct)
    { _states[state] = data; return Task.CompletedTask; }
    public Task<SsoStateData?> ConsumeStateAsync(string state, CancellationToken ct)
    {
        if (_states.Remove(state, out var d)) return Task.FromResult<SsoStateData?>(d);
        return Task.FromResult<SsoStateData?>(null);
    }
    public Task<bool> TryMarkLogoutJtiAsync(string jti, TimeSpan ttl, CancellationToken ct) => Task.FromResult(_jtis.Add(jti));
    public Task RevokeSidAsync(string sid, TimeSpan ttl, CancellationToken ct) { RevokedSids.Add(sid); return Task.CompletedTask; }
    public Task<bool> IsSidRevokedAsync(string sid, CancellationToken ct) => Task.FromResult(RevokedSids.Contains(sid));
    public Task SaveIdTokenHintAsync(string sid, string idToken, TimeSpan ttl, CancellationToken ct) => Task.CompletedTask;
    public Task<string?> GetIdTokenHintAsync(string sid, CancellationToken ct) => Task.FromResult<string?>("hint");
    public Task SaveMfaLinkAsync(Guid userId, SsoSessionInfo info, TimeSpan ttl, CancellationToken ct)
    { MfaLinks[userId] = info; return Task.CompletedTask; }
    public Task<SsoSessionInfo?> TakeMfaLinkAsync(Guid userId, CancellationToken ct)
        => Task.FromResult(MfaLinks.Remove(userId, out var i) ? i : null);
    private readonly Dictionary<string, ProDiabHis.Contracts.Auth.LoginResponse> _tickets = new();
    public Task SaveTicketAsync(string ticket, ProDiabHis.Contracts.Auth.LoginResponse login, TimeSpan ttl, CancellationToken ct)
    { _tickets[ticket] = login; return Task.CompletedTask; }
    public Task<ProDiabHis.Contracts.Auth.LoginResponse?> TakeTicketAsync(string ticket, CancellationToken ct)
        => Task.FromResult(_tickets.Remove(ticket, out var l) ? l : null);
}

public class SsoReturnUrlTests
{
    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("evil.com")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("/a\r\nb")]
    public void Sanitize_Invalid_FallsBackToDefault(string? input)
        => SsoReturnUrl.Sanitize(input).Should().Be(SsoReturnUrl.Default);

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/sso/complete?returnUrl=%2Fdashboard")]
    [InlineData("/sso/complete?returnUrl=/dashboard")]
    public void Sanitize_InternalPath_IsKept(string input)
        => SsoReturnUrl.Sanitize(input).Should().Be(input);
}

public class OidcTokenValidatorTests
{
    private const string Issuer = "https://idp.test";
    private const string ClientId = "prodiab-his";
    private const string Nonce = "nonce-123";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly SsoOptions _opts = new() { ClientId = ClientId, Authority = "https://login.test" };

    private OpenIdConnectConfiguration Cfg()
    {
        var c = new OpenIdConnectConfiguration { Issuer = Issuer };
        c.SigningKeys.Add(new RsaSecurityKey(_rsa) { KeyId = "k1" });
        return c;
    }

    private string Token(string iss = Issuer, string aud = ClientId, string? nonce = Nonce, SecurityKey? key = null,
        string alg = SecurityAlgorithms.RsaSha256, DateTime? exp = null, DateTime? nbf = null,
        Dictionary<string, object>? extra = null)
    {
        var claims = new Dictionary<string, object> { ["sub"] = "sub-1", ["email"] = "a@b.vn", ["email_verified"] = "true", ["sid"] = "sid-1" };
        if (nonce != null) claims["nonce"] = nonce;
        if (extra != null) foreach (var kv in extra) claims[kv.Key] = kv.Value;
        var k = key ?? new RsaSecurityKey(_rsa) { KeyId = "k1" };
        var d = new SecurityTokenDescriptor
        {
            Issuer = iss,
            Audience = aud,
            Claims = claims,
            NotBefore = nbf ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = exp ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(k, alg)
        };
        return new JsonWebTokenHandler().CreateToken(d);
    }

    private async Task<Result<OidcIdentity>> Validate(string t)
        => (await OidcTokenValidator.ValidateIdTokenAsync(t, Cfg(), _opts, Nonce)).result;

    [Fact]
    public async Task IdToken_Valid_ReturnsIdentity()
    {
        var r = await Validate(Token(extra: new() { ["amr"] = new[] { "pwd", "mfa" } }));
        r.IsSuccess.Should().BeTrue();
        r.Value!.Subject.Should().Be("sub-1");
        r.Value.EmailVerified.Should().BeTrue();
        r.Value.Sid.Should().Be("sid-1");
        r.Value.Amr.Should().Contain("mfa");
    }

    [Fact] public async Task IdToken_WrongIssuer_Rejected() => (await Validate(Token(iss: "https://evil"))).IsSuccess.Should().BeFalse();
    [Fact] public async Task IdToken_WrongAudience_Rejected() => (await Validate(Token(aud: "other"))).IsSuccess.Should().BeFalse();
    [Fact] public async Task IdToken_WrongNonce_Rejected() => (await Validate(Token(nonce: "zzz"))).IsSuccess.Should().BeFalse();
    [Fact] public async Task IdToken_MissingNonce_Rejected() => (await Validate(Token(nonce: null))).IsSuccess.Should().BeFalse();

    [Fact]
    public async Task IdToken_AltIssuer_RejectedByDefault_AcceptedWhenConfigured()
    {
        // iss khac discovery.Issuer (vd host IdP khac gia tri IssuerUri) -> mac dinh tu choi
        var altIss = "https://is.staging.diab.com.vn";
        var tok = Token(iss: altIss);
        (await OidcTokenValidator.ValidateIdTokenAsync(tok, Cfg(), _opts, Nonce)).result.IsSuccess.Should().BeFalse();

        // khi cau hinh Sso__ValidIssuers co altIss -> chap nhan
        var opts2 = new SsoOptions { ClientId = ClientId, Authority = "https://login.test", ValidIssuers = new[] { altIss } };
        (await OidcTokenValidator.ValidateIdTokenAsync(tok, Cfg(), opts2, Nonce)).result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IdToken_ExpiredSigningCert_ValidWhenIgnoreFlag_RejectedWhenStrict()
    {
        // Mo phong cert signing cua IS da het han (giong IDX10249 tren IS DiaB)
        using var rsa = RSA.Create(2048);
        var reqc = new CertificateRequest("CN=diab-is-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = reqc.CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-6), DateTimeOffset.UtcNow.AddYears(-5));
        var key = new X509SecurityKey(cert) { KeyId = "kexp" };
        var cfg = new OpenIdConnectConfiguration { Issuer = Issuer };
        cfg.SigningKeys.Add(key);
        var tok = Token(key: key);

        // Mac dinh IgnoreSigningCertExpiry=true -> van verify chu ky, bo qua han cert -> hop le
        var optsIgnore = new SsoOptions { ClientId = ClientId };
        (await OidcTokenValidator.ValidateIdTokenAsync(tok, cfg, optsIgnore, Nonce)).result.IsSuccess.Should().BeTrue();

        // Strict -> tu choi vi cert het han (IDX10249)
        var optsStrict = new SsoOptions { ClientId = ClientId, IgnoreSigningCertExpiry = false };
        (await OidcTokenValidator.ValidateIdTokenAsync(tok, cfg, optsStrict, Nonce)).result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IdToken_WrongSignature_RejectedEvenWhenIgnoreExpiry()
    {
        // Ky bang key khac khong co trong JWKS -> van bi tu choi du IgnoreSigningCertExpiry=true (mac dinh)
        using var other = RSA.Create(2048);
        var tok = Token(key: new RsaSecurityKey(other) { KeyId = "kother" });
        (await OidcTokenValidator.ValidateIdTokenAsync(tok, Cfg(), _opts, Nonce)).result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IdToken_Expired_Rejected()
        => (await Validate(Token(nbf: DateTime.UtcNow.AddMinutes(-30), exp: DateTime.UtcNow.AddMinutes(-20)))).IsSuccess.Should().BeFalse();

    [Fact]
    public async Task IdToken_WrongSignature_Rejected()
    {
        using var other = RSA.Create(2048);
        var t = Token(key: new RsaSecurityKey(other) { KeyId = "k1" });
        (await Validate(t)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IdToken_Hs256Alg_Rejected()
    {
        var sym = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "k1" };
        (await Validate(Token(key: sym, alg: SecurityAlgorithms.HmacSha256))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IdToken_WrongAzp_Rejected()
        => (await Validate(Token(extra: new() { ["azp"] = "other" }))).IsSuccess.Should().BeFalse();

    // ---- logout token ----
    private string LogoutToken(bool events = true, bool nonce = false, string jti = "j1", string? sid = "sid-1")
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = "sub-1",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        if (sid != null) claims["sid"] = sid;
        if (jti != "") claims["jti"] = jti;
        if (events) claims["events"] = new Dictionary<string, object> { [OidcTokenValidator.BackchannelEvent] = new Dictionary<string, object>() };
        if (nonce) claims["nonce"] = "n";
        var d = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = ClientId,
            Claims = claims,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "k1" }, SecurityAlgorithms.RsaSha256)
        };
        return new JsonWebTokenHandler().CreateToken(d);
    }

    private async Task<Result<OidcLogoutToken>> ValidateLogout(string t)
        => (await OidcTokenValidator.ValidateLogoutTokenAsync(t, Cfg(), _opts)).result;

    [Fact]
    public async Task LogoutToken_Valid_Accepted()
    {
        var r = await ValidateLogout(LogoutToken());
        r.IsSuccess.Should().BeTrue();
        r.Value!.Sid.Should().Be("sid-1");
        r.Value.Jti.Should().Be("j1");
    }

    [Fact] public async Task LogoutToken_MissingEvents_Rejected() => (await ValidateLogout(LogoutToken(events: false))).IsSuccess.Should().BeFalse();
    [Fact] public async Task LogoutToken_WithNonce_Rejected() => (await ValidateLogout(LogoutToken(nonce: true))).IsSuccess.Should().BeFalse();
    [Fact] public async Task LogoutToken_MissingJti_Rejected() => (await ValidateLogout(LogoutToken(jti: ""))).IsSuccess.Should().BeFalse();
}

public class SsoHandlerTests
{
    private const string Bind = "bind-1";

    private static SsoOptions Opts(Action<SsoOptions>? cfg = null)
    {
        var o = new SsoOptions
        {
            Enabled = true, ProviderKey = "diab-is", ClientId = "prodiab-his", RequireEmailVerified = true
        };
        cfg?.Invoke(o);
        return o;
    }

    private static User MakeUser(string email, int tenant = 1, string roleCode = "bac_si", bool twoFa = false,
        string status = UserStatus.Active, bool active = true)
    {
        var role = new Role { Id = Guid.NewGuid(), Code = roleCode, Name = roleCode };
        return new User
        {
            Id = Guid.NewGuid(), Email = email, PasswordHash = "x", FullName = "Test", TenantId = tenant,
            IsActive = active, Status = status, TwoFaEnabled = twoFa,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };
    }

    private static AppDbContext Db(params User[] users)
    {
        var db = TestDbContextFactory.Create();
        foreach (var u in users)
        {
            foreach (var ur in u.UserRoles) if (ur.Role != null && db.Set<Role>().Find(ur.Role.Id) == null) db.Set<Role>().Add(ur.Role);
            db.Users.Add(u);
        }
        db.SaveChanges();
        return db;
    }

    private static IJwtService Jwt()
    {
        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateSsoAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<string>())
            .Returns("sso-access");
        jwt.GenerateRefreshToken().Returns(_ => Guid.NewGuid().ToString("N"));
        jwt.GenerateMfaPendingToken(Arg.Any<User>()).Returns("pending-tok");
        jwt.GenerateMfaSetupToken(Arg.Any<User>()).Returns("setup-tok");
        return jwt;
    }

    private static CompleteSsoLoginCommandHandler Handler(AppDbContext db, SsoOptions opts, IOidcClient oidc,
        FakeSsoStateStore store, IJwtService? jwt = null)
    {
        jwt ??= Jwt();
        var login = new LoginCommandHandler(db, jwt, Substitute.For<IPasswordHasher>(),
            Substitute.For<ILogger<LoginCommandHandler>>(), new ConfigurationBuilder().Build(), new FakeEmptyDapperConnectionFactory());
        return new CompleteSsoLoginCommandHandler(db, opts, oidc, store, jwt, login,
            Substitute.For<ILogger<CompleteSsoLoginCommandHandler>>());
    }

    private static IOidcClient OidcReturning(OidcIdentity id)
    {
        var oidc = Substitute.For<IOidcClient>();
        oidc.ExchangeCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<OidcIdentity>.Success(id));
        return oidc;
    }

    private static OidcIdentity Id(string email = "bs@clinic.vn", bool verified = true, string sub = "sub-1",
        string sid = "sid-1", params string[] amr)
        => new(sub, email, verified, sid, amr, "id-token");

    private static async Task<Result<SsoCompleteResult>> Run(CompleteSsoLoginCommandHandler h, FakeSsoStateStore store,
        string state = "st1", string? bind = Bind)
    {
        await store.SaveStateAsync(state, new SsoStateData("n", "v", "/sso/complete?returnUrl=/dashboard", Bind), TimeSpan.FromMinutes(5), default);
        return await h.Handle(new CompleteSsoLoginCommand("code", state, null, bind, "1.1.1.1", "ua"), default);
    }

    // ---------- mapping ----------

    [Fact]
    public async Task Map_ByEmail_OneUser_CreatesLinkAndIssuesSsoSession()
    {
        var u = MakeUser("BS@Clinic.vn");
        var db = Db(u);
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id("  bs@clinic.VN ")), store), store);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Login.AccessToken.Should().Be("sso-access");
        r.Value.ReturnUrl.Should().Be("/sso/complete?returnUrl=/dashboard");
        db.ExternalLogins.IgnoreQueryFilters().Should().ContainSingle(l => l.Subject == "sub-1" && l.UserId == u.Id);
        db.RefreshTokens.IgnoreQueryFilters().Should().ContainSingle(t => t.SsoSid == "sid-1" && t.SsoSub == "sub-1");
    }

    [Fact]
    public async Task Map_ByExistingLink_WorksEvenIfEmailUnverifiedOrChanged()
    {
        var u = MakeUser("bs@clinic.vn");
        var db = Db(u);
        db.ExternalLogins.Add(new ExternalLogin { TenantId = 1, UserId = u.Id, Provider = "diab-is", Subject = "sub-1" });
        db.SaveChanges();
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id("other@x.vn", verified: false)), store), store);
        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Map_NoMatch_NotProvisioned()
    {
        var db = Db(MakeUser("someone@clinic.vn"));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id("nobody@clinic.vn")), store), store);
        r.ErrorCode.Should().Be("SSO_USER_NOT_PROVISIONED");
    }

    [Fact]
    public async Task Map_LockedUser_Disabled()
    {
        var db = Db(MakeUser("bs@clinic.vn", status: UserStatus.Locked));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id()), store), store);
        r.ErrorCode.Should().Be("SSO_USER_DISABLED");
    }

    [Fact]
    public async Task Map_EmailNotVerified_Rejected()
    {
        var db = Db(MakeUser("bs@clinic.vn"));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id(verified: false)), store), store);
        r.ErrorCode.Should().Be("SSO_EMAIL_NOT_VERIFIED");
    }

    [Fact]
    public async Task Map_SameEmailTwoTenants_Ambiguous()
    {
        var db = Db(MakeUser("bs@clinic.vn", tenant: 1), MakeUser("bs@clinic.vn", tenant: 2));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id()), store), store);
        r.ErrorCode.Should().Be("SSO_AMBIGUOUS_ACCOUNT");
    }

    // ---------- 2FA gating ----------

    [Fact]
    public async Task Admin_With2faEnabled_Requires2fa_AndKeepsSsoLinkForVerify()
    {
        var u = MakeUser("ad@clinic.vn", roleCode: "admin", twoFa: true);
        var db = Db(u);
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id("ad@clinic.vn")), store), store);
        r.IsSuccess.Should().BeTrue();
        r.Value!.Login.Requires2fa.Should().BeTrue();
        r.Value.Login.MfaPendingToken.Should().Be("pending-tok");
        r.Value.Login.AccessToken.Should().BeEmpty();
        store.MfaLinks[u.Id].Sid.Should().Be("sid-1");
        db.RefreshTokens.IgnoreQueryFilters().Should().BeEmpty();
    }

    [Fact]
    public async Task Admin_MandatoryRoleWithout2fa_MfaSetupRequired()
    {
        var db = Db(MakeUser("ad@clinic.vn", roleCode: "admin"));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id("ad@clinic.vn")), store), store);
        r.Value!.Login.MfaSetupRequired.Should().BeTrue();
        r.Value.Login.MfaSetupToken.Should().Be("setup-tok");
        r.Value.Login.AccessToken.Should().BeEmpty();
    }

    [Fact]
    public async Task Admin_TrustIdpMfaFalse_StillRequires2fa_EvenWithMfaAmr()
    {
        var db = Db(MakeUser("ad@clinic.vn", roleCode: "admin", twoFa: true));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(o => o.TrustIdpMfa = false),
            OidcReturning(Id("ad@clinic.vn", amr: "mfa")), store), store);
        r.Value!.Login.Requires2fa.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_TrustIdpMfaTrue_WithMfaAmr_SkipsHis2fa()
    {
        var db = Db(MakeUser("ad@clinic.vn", roleCode: "admin", twoFa: true));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(o => o.TrustIdpMfa = true),
            OidcReturning(Id("ad@clinic.vn", amr: "mfa")), store), store);
        r.Value!.Login.Requires2fa.Should().BeFalse();
        r.Value.Login.AccessToken.Should().Be("sso-access");
    }

    [Fact]
    public async Task Admin_TrustIdpMfaTrue_WithoutMfaAmr_Still2fa()
    {
        var db = Db(MakeUser("ad@clinic.vn", roleCode: "admin", twoFa: true));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(o => o.TrustIdpMfa = true),
            OidcReturning(Id("ad@clinic.vn", amr: "pwd")), store), store);
        r.Value!.Login.Requires2fa.Should().BeTrue();
    }

    // ---------- state ----------

    [Fact]
    public async Task State_ReusedSecondTime_InvalidState()
    {
        var db = Db(MakeUser("bs@clinic.vn"));
        var store = new FakeSsoStateStore();
        var h = Handler(db, Opts(), OidcReturning(Id()), store);
        (await Run(h, store)).IsSuccess.Should().BeTrue();
        var second = await h.Handle(new CompleteSsoLoginCommand("code", "st1", null, Bind, null, null), default);
        second.ErrorCode.Should().Be("SSO_INVALID_STATE");
    }

    [Fact]
    public async Task State_BindCookieMismatch_InvalidState()
    {
        var db = Db(MakeUser("bs@clinic.vn"));
        var store = new FakeSsoStateStore();
        var r = await Run(Handler(db, Opts(), OidcReturning(Id()), store), store, bind: "wrong");
        r.ErrorCode.Should().Be("SSO_INVALID_STATE");
    }

    [Fact]
    public async Task Complete_Disabled_ReturnsSsoDisabled()
    {
        var db = Db();
        var store = new FakeSsoStateStore();
        var r = await Handler(db, Opts(o => o.Enabled = false), Substitute.For<IOidcClient>(), store)
            .Handle(new CompleteSsoLoginCommand("c", "s", null, Bind, null, null), default);
        r.ErrorCode.Should().Be("SSO_DISABLED");
    }

    [Fact]
    public async Task Start_WithoutRedis_ReturnsIdpUnavailable_NoCrash()
    {
        var h = new StartSsoLoginCommandHandler(Opts(), Substitute.For<IOidcClient>(),
            new FakeSsoStateStore { IsAvailable = false }, Substitute.For<ILogger<StartSsoLoginCommandHandler>>());
        var r = await h.Handle(new StartSsoLoginCommand("/x"), default);
        r.ErrorCode.Should().Be("SSO_IDP_UNAVAILABLE");
    }

    // ---------- back-channel ----------

    private static SsoBackchannelLogoutCommandHandler BcHandler(AppDbContext db, FakeSsoStateStore store,
        Result<OidcLogoutToken> validation, SsoOptions? opts = null)
    {
        var oidc = Substitute.For<IOidcClient>();
        oidc.ValidateLogoutTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(validation);
        return new SsoBackchannelLogoutCommandHandler(db, opts ?? Opts(), oidc, store,
            Substitute.For<ILogger<SsoBackchannelLogoutCommandHandler>>());
    }

    [Fact]
    public async Task Backchannel_RevokesOnlySsoSession_PasswordSessionUntouched()
    {
        var u = MakeUser("bs@clinic.vn");
        var db = Db(u);
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "sso-rt", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-1", SsoSub = "sub-1" });
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "pwd-rt", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "other-sso", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-2", SsoSub = "sub-1" });
        db.SaveChanges();
        var store = new FakeSsoStateStore();

        var r = await BcHandler(db, store, Result<OidcLogoutToken>.Success(new OidcLogoutToken("sub-1", "sid-1", "j1")))
            .Handle(new SsoBackchannelLogoutCommand("tok", null), default);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(1);
        db.RefreshTokens.Single(t => t.Token == "sso-rt").IsRevoked.Should().BeTrue();
        db.RefreshTokens.Single(t => t.Token == "pwd-rt").IsRevoked.Should().BeFalse();
        db.RefreshTokens.Single(t => t.Token == "other-sso").IsRevoked.Should().BeFalse();
        store.RevokedSids.Should().Contain("sid-1");
    }

    [Fact]
    public async Task Backchannel_SubOnly_RevokesAllSsoSessionsOfSub_NotPasswordSession()
    {
        var u = MakeUser("bs@clinic.vn");
        var db = Db(u);
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-1", SsoSub = "sub-1" });
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "b", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-2", SsoSub = "sub-1" });
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "pwd", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        db.SaveChanges();

        var r = await BcHandler(db, new FakeSsoStateStore(), Result<OidcLogoutToken>.Success(new OidcLogoutToken("sub-1", null, "j2")))
            .Handle(new SsoBackchannelLogoutCommand("tok", null), default);

        r.Value.Should().Be(2);
        db.RefreshTokens.Single(t => t.Token == "pwd").IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Backchannel_ReplayedJti_Rejected()
    {
        var db = Db(MakeUser("bs@clinic.vn"));
        var store = new FakeSsoStateStore();
        var h = BcHandler(db, store, Result<OidcLogoutToken>.Success(new OidcLogoutToken("sub-1", "sid-1", "same-jti")));
        (await h.Handle(new SsoBackchannelLogoutCommand("tok", null), default)).IsSuccess.Should().BeTrue();
        (await h.Handle(new SsoBackchannelLogoutCommand("tok", null), default)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Backchannel_InvalidToken_RejectedAndRevokesNothing()
    {
        var u = MakeUser("bs@clinic.vn");
        var db = Db(u);
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-1", SsoSub = "sub-1" });
        db.SaveChanges();
        var r = await BcHandler(db, new FakeSsoStateStore(), Result<OidcLogoutToken>.Failure("SSO_TOKEN_INVALID", "x"))
            .Handle(new SsoBackchannelLogoutCommand("tok", null), default);
        r.IsSuccess.Should().BeFalse();
        db.RefreshTokens.Single().IsRevoked.Should().BeFalse();
    }

    // ---------- logout ----------

    [Fact]
    public async Task Logout_RevokesOnlyCurrentSession_ReturnsEndSessionUrl()
    {
        var u = MakeUser("bs@clinic.vn");
        var db = Db(u);
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "cur", ExpiresAt = DateTime.UtcNow.AddDays(1), SsoSid = "sid-1", SsoSub = "sub-1" });
        db.RefreshTokens.Add(new RefreshToken { UserId = u.Id, TenantId = 1, Token = "other", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        db.SaveChanges();
        var oidc = Substitute.For<IOidcClient>();
        oidc.BuildEndSessionUrlAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("https://idp/end");
        var h = new SsoLogoutCommandHandler(db, Opts(), oidc, new FakeSsoStateStore(),
            Substitute.For<ILogger<SsoLogoutCommandHandler>>());

        var r = await h.Handle(new SsoLogoutCommand("cur", null, null), default);

        r.Value!.EndSessionUrl.Should().Be("https://idp/end");
        db.RefreshTokens.Single(t => t.Token == "cur").IsRevoked.Should().BeTrue();
        db.RefreshTokens.Single(t => t.Token == "other").IsRevoked.Should().BeFalse();
    }
}
