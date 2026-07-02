using ProDiabHis.Application.PublicApi;
using StackExchange.Redis;

namespace ProDiabHis.Infrastructure.RateLimit;

/// <summary>Redis sliding window rate limiter dung ZADD + ZREMRANGEBYSCORE + ZCARD</summary>
public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<bool> AllowAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)window.TotalMilliseconds;
        var redisKey = new RedisKey($"rl:{key}");

        var tran = db.CreateTransaction();
        // Remove old entries outside window
        _ = tran.SortedSetRemoveRangeByScoreAsync(redisKey, double.NegativeInfinity, now - windowMs);
        // Add current timestamp
        _ = tran.SortedSetAddAsync(redisKey, now.ToString(), now);
        // Count
        var countTask = tran.SortedSetLengthAsync(redisKey);
        // Set expiry
        _ = tran.KeyExpireAsync(redisKey, window);

        await tran.ExecuteAsync();
        var count = await countTask;

        return count <= limit;
    }

    public async Task<long> GetCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)window.TotalMilliseconds;
        return await db.SortedSetLengthAsync($"rl:{key}", now - windowMs, now);
    }
}

/// <summary>In-memory fallback khi khong co Redis (dev mode)</summary>
public class InMemoryRateLimiter : IRateLimiter
{
    // Simple allow-all for dev
    public Task<bool> AllowAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<long> GetCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
        => Task.FromResult(0L);
}
