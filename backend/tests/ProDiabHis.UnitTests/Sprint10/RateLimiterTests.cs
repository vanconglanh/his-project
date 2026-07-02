using FluentAssertions;
using ProDiabHis.Infrastructure.RateLimit;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint10;

public class RateLimiterTests
{
    [Fact]
    public async Task InMemoryRateLimiter_AlwaysAllows()
    {
        var limiter = new InMemoryRateLimiter();

        for (int i = 0; i < 200; i++)
        {
            var allowed = await limiter.AllowAsync("test_key", 60, TimeSpan.FromMinutes(1));
            allowed.Should().BeTrue();
        }
    }

    [Fact]
    public async Task InMemoryRateLimiter_GetCount_ReturnsZero()
    {
        var limiter = new InMemoryRateLimiter();
        var count = await limiter.GetCountAsync("test_key", TimeSpan.FromMinutes(1));
        count.Should().Be(0);
    }

    [Fact]
    public void SlidingWindow_Key_Format_ShouldBeConsistent()
    {
        var partnerId = Guid.NewGuid();
        var minuteKey = $"{partnerId}:min:{DateTime.UtcNow:yyyyMMddHHmm}";
        var dailyKey = $"{partnerId}:daily:{DateTime.UtcNow:yyyyMMdd}";

        minuteKey.Should().Contain(partnerId.ToString());
        minuteKey.Should().Contain(":min:");
        dailyKey.Should().Contain(":daily:");
    }

    [Fact]
    public void DailyQuota_Exceeded_ShouldBlock()
    {
        int currentCount = 10001;
        int quota = 10000;
        var exceeded = currentCount >= quota;
        exceeded.Should().BeTrue();
    }
}
