using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OtpNet;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Users;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Users;

public class Setup2FAFlowTests
{
    private readonly IEncryptionService _encryption = new ProDiabHis.UnitTests.FakeEncryptionService();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task Setup_GeneratesSecretAndSavesEncrypted()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);

        var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = userId, TenantId = 1, Email = "doc@test.vn",
            FullName = "Test", PasswordHash = "h", Status = UserStatus.Active
        });
        await db.SaveChangesAsync();

        var handler = new Setup2FACommandHandler(db, _currentUser, _encryption);
        var result = await handler.Handle(new Setup2FACommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Secret.Should().NotBeNullOrEmpty();
        result.Value.OtpauthUrl.Should().StartWith("otpauth://totp/");

        var user = db.Users.IgnoreQueryFilters().First(u => u.Id == userId);
        user.TwoFaSecret.Should().NotBeNull();
    }

    [Fact]
    public async Task Enable_WithValidTotpCode_Enables2FAAndReturns10RecoveryCodes()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);

        // Tao secret base32 va tao TOTP code hop le
        var secretBytes = new byte[20];
        new Random(42).NextBytes(secretBytes);
        var secret = Base32Encode(secretBytes);
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = userId, TenantId = 1, Email = "doc@test.vn", FullName = "Test", PasswordHash = "h",
            Status = UserStatus.Active,
            TwoFaSecret = _encryption.Encrypt(secret),
            TwoFaEnabled = false
        });
        await db.SaveChangesAsync();

        var handler = new Enable2FACommandHandler(db, _currentUser, _encryption);
        var result = await handler.Handle(new Enable2FACommand(validCode), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecoveryCodes.Should().HaveCount(10);
        result.Value.RecoveryCodes.Should().AllSatisfy(c =>
            c.Should().MatchRegex(@"^[a-f0-9]{5}-[a-f0-9]{5}$"));

        var user = db.Users.IgnoreQueryFilters().First(u => u.Id == userId);
        user.TwoFaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Enable_WithInvalidCode_ReturnsError()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);

        var secretBytes = new byte[20];
        new Random(99).NextBytes(secretBytes);
        var secret = Base32Encode(secretBytes);

        var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = userId, TenantId = 1, Email = "doc@test.vn", FullName = "Test", PasswordHash = "h",
            Status = UserStatus.Active,
            TwoFaSecret = _encryption.Encrypt(secret),
            TwoFaEnabled = false
        });
        await db.SaveChangesAsync();

        var handler = new Enable2FACommandHandler(db, _currentUser, _encryption);
        var result = await handler.Handle(new Enable2FACommand("000000"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TWO_FA_INVALID_CODE");
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
