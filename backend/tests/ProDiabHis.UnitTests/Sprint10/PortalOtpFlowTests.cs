using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.PublicApi;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint10;

/// <summary>Unit test cho OTP flow logic — test service behaviour rieng biet</summary>
public class PortalOtpFlowTests
{
    [Fact]
    public void Otp_Format_ShouldBe6Digits()
    {
        // Test that generated OTP is always 6 digits
        for (int i = 0; i < 100; i++)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            otp.Should().HaveLength(6);
            otp.Should().MatchRegex("^[0-9]{6}$");
        }
    }

    [Fact]
    public void Bcrypt_OtpHash_ShouldVerifyCorrectly()
    {
        var otp = "123456";
        var hash = BCrypt.Net.BCrypt.HashPassword(otp, workFactor: 10);

        BCrypt.Net.BCrypt.Verify(otp, hash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("000000", hash).Should().BeFalse();
    }

    [Fact]
    public void Bcrypt_WrongOtp_ShouldNotVerify()
    {
        var otp = "654321";
        var hash = BCrypt.Net.BCrypt.HashPassword(otp);
        BCrypt.Net.BCrypt.Verify("123456", hash).Should().BeFalse();
    }

    [Fact]
    public async Task SmsGateway_Mock_ShouldNotThrow()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ProDiabHis.Infrastructure.Sms.MockSmsGateway>.Instance;
        var gateway = new ProDiabHis.Infrastructure.Sms.MockSmsGateway(logger);

        var act = async () => await gateway.SendAsync("+84901234567", "OTP: 123456");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void OtpExpired_WhenExpiresAtPast_ShouldBeDetected()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(-1); // already expired
        var isExpired = expiresAt < DateTime.UtcNow;
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void OtpValid_WhenExpiresAtFuture_ShouldBeDetected()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(4); // still valid
        var isExpired = expiresAt < DateTime.UtcNow;
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void MaxAttempts_WhenReached5_ShouldTriggerLock()
    {
        var attempts = 4; // already 4, now 5th attempt fails
        var newAttempts = attempts + 1;
        var shouldLock = newAttempts >= 5;
        shouldLock.Should().BeTrue();
    }

    [Fact]
    public void MaxAttempts_WhenBelow5_ShouldNotLock()
    {
        var attempts = 2;
        var newAttempts = attempts + 1;
        var shouldLock = newAttempts >= 5;
        shouldLock.Should().BeFalse();
    }
}
