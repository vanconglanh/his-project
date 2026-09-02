using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OtpNet;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

/// <summary>P0 bao mat: kiem thu luong dang nhap 2 buoc (2FA) + endpoint verify TOTP.</summary>
public class Login2faFlowTests
{
    private readonly IEncryptionService _encryption = new FakeEncryptionService();

    /// <summary>Tao InMemory AppDbContext co san 1 user (kem role) de test login.</summary>
    private static ProDiabHis.Infrastructure.Persistence.AppDbContext DbWith(User user)
    {
        var db = TestDbContextFactory.Create();
        foreach (var ur in user.UserRoles)
            if (ur.Role != null) db.Set<Role>().Add(ur.Role);
        db.Users.Add(user);
        db.SaveChanges();
        return db;
    }

    // ---------- LOGIN buoc 1 ----------

    [Fact]
    public async Task Login_WhenUser2faEnabled_ReturnsRequires2faWithoutAccessToken()
    {
        var role = new Role { Id = Guid.NewGuid(), Code = "bac_si", Name = "Bác sĩ" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doc2fa@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Bac Si 2FA",
            TenantId = 1,
            IsActive = true,
            Status = UserStatus.Active,
            TwoFaEnabled = true,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };

        var db = DbWith(user);
        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateMfaPendingToken(user).Returns("pending-token-xyz");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("password123", user.PasswordHash).Returns(true);

        var handler = new LoginCommandHandler(db, jwt, hasher,
            Substitute.For<ILogger<LoginCommandHandler>>(), new ConfigurationBuilder().Build(), new FakeEmptyDapperConnectionFactory());

        var result = await handler.Handle(new LoginCommand("doc2fa@clinic.vn", "password123"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Requires2fa.Should().BeTrue();
        result.Value.MfaPendingToken.Should().Be("pending-token-xyz");
        result.Value.AccessToken.Should().BeEmpty();
        result.Value.RefreshToken.Should().BeEmpty();
        result.Value.MfaSetupRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WhenNormalUserNo2fa_ReturnsFullTokens()
    {
        var role = new Role { Id = Guid.NewGuid(), Code = "bac_si", Name = "Bác sĩ" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "normal@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Nguoi Dung Thuong",
            TenantId = 1,
            IsActive = true,
            Status = UserStatus.Active,
            TwoFaEnabled = false,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };

        var db = DbWith(user);
        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateAccessToken(user, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(), Arg.Any<int?>())
            .Returns("full-access-token");
        jwt.GenerateRefreshToken().Returns("full-refresh-token");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("password123", user.PasswordHash).Returns(true);

        var handler = new LoginCommandHandler(db, jwt, hasher,
            Substitute.For<ILogger<LoginCommandHandler>>(), new ConfigurationBuilder().Build(), new FakeEmptyDapperConnectionFactory());

        var result = await handler.Handle(new LoginCommand("normal@clinic.vn", "password123"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Requires2fa.Should().BeFalse();
        result.Value.MfaSetupRequired.Should().BeFalse();
        result.Value.AccessToken.Should().Be("full-access-token");
        result.Value.RefreshToken.Should().Be("full-refresh-token");
    }

    [Fact]
    public async Task Login_WhenMandatoryRoleNo2fa_ReturnsMfaSetupTokenWithoutAccessToken()
    {
        var role = new Role { Id = Guid.NewGuid(), Code = "admin", Name = "Quản trị viên" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Admin",
            TenantId = 1,
            IsActive = true,
            Status = UserStatus.Active,
            TwoFaEnabled = false,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };

        var db = DbWith(user);
        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateMfaSetupToken(user).Returns("setup-token-abc");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("password123", user.PasswordHash).Returns(true);

        var handler = new LoginCommandHandler(db, jwt, hasher,
            Substitute.For<ILogger<LoginCommandHandler>>(), new ConfigurationBuilder().Build(), new FakeEmptyDapperConnectionFactory());

        var result = await handler.Handle(new LoginCommand("admin@clinic.vn", "password123"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaSetupRequired.Should().BeTrue();
        result.Value.MfaSetupToken.Should().Be("setup-token-abc");
        result.Value.MfaSetupMessage.Should().NotBeNullOrWhiteSpace();
        result.Value.AccessToken.Should().BeEmpty();
        result.Value.RefreshToken.Should().BeEmpty();
    }

    // ---------- VERIFY buoc 2 ----------

    private (Verify2faLoginCommandHandler Handler, IRateLimiter RateLimiter, string ValidCode, Guid UserId)
        BuildVerifyHandler(bool tokenValid = true, bool allowRate = true)
    {
        var userId = Guid.NewGuid();

        var secretBytes = new byte[20];
        new Random(7).NextBytes(secretBytes);
        var secret = Base32Encode(secretBytes);
        var validCode = new Totp(secretBytes).ComputeTotp();

        var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = userId, TenantId = 1, Email = "v@test.vn", FullName = "Verify", PasswordHash = "h",
            Status = UserStatus.Active, IsActive = true,
            TwoFaEnabled = true,
            TwoFaSecret = _encryption.Encrypt(secret),
            UserRoles = new List<UserRole>()
        });
        db.SaveChanges();

        var jwt = Substitute.For<IJwtService>();
        jwt.ValidateMfaToken(Arg.Any<string>(), "mfa-pending")
            .Returns(tokenValid ? ((Guid, int)?)(userId, 1) : null);
        jwt.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(), Arg.Any<int?>())
            .Returns("full-access-token");
        jwt.GenerateRefreshToken().Returns("full-refresh-token");

        var rate = Substitute.For<IRateLimiter>();
        rate.AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(allowRate);

        var loginHandler = new LoginCommandHandler(db, jwt, Substitute.For<IPasswordHasher>(),
            Substitute.For<ILogger<LoginCommandHandler>>(), new ConfigurationBuilder().Build(), new FakeEmptyDapperConnectionFactory());

        var handler = new Verify2faLoginCommandHandler(db, jwt, rate, _encryption, loginHandler,
            Substitute.For<ILogger<Verify2faLoginCommandHandler>>());

        return (handler, rate, validCode, userId);
    }

    [Fact]
    public async Task Verify_WithValidTotp_ReturnsFullTokens()
    {
        var (handler, _, validCode, _) = BuildVerifyHandler();

        var result = await handler.Handle(new Verify2faLoginCommand("pending", validCode), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("full-access-token");
        result.Value.RefreshToken.Should().Be("full-refresh-token");
        result.Value.Requires2fa.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_WithWrongTotp_ReturnsInvalidCode()
    {
        var (handler, _, _, _) = BuildVerifyHandler();

        var result = await handler.Handle(new Verify2faLoginCommand("pending", "000000"), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_MFA_INVALID_CODE");
    }

    [Fact]
    public async Task Verify_WithInvalidToken_ReturnsTokenInvalid()
    {
        var (handler, _, validCode, _) = BuildVerifyHandler(tokenValid: false);

        var result = await handler.Handle(new Verify2faLoginCommand("bad", validCode), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_MFA_TOKEN_INVALID");
    }

    [Fact]
    public async Task Verify_WhenRateLimited_ReturnsTooManyAttempts()
    {
        var (handler, _, validCode, _) = BuildVerifyHandler(allowRate: false);

        var result = await handler.Handle(new Verify2faLoginCommand("pending", validCode), default);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_MFA_TOO_MANY_ATTEMPTS");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new System.Text.StringBuilder();
        var bits = 0; var accumulator = 0;
        foreach (var b in data)
        {
            accumulator = (accumulator << 8) | b; bits += 8;
            while (bits >= 5) { bits -= 5; output.Append(alphabet[(accumulator >> bits) & 31]); }
        }
        if (bits > 0) output.Append(alphabet[(accumulator << (5 - bits)) & 31]);
        return output.ToString();
    }
}
