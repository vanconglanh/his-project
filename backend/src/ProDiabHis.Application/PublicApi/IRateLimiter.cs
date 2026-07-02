namespace ProDiabHis.Application.PublicApi;

/// <summary>Redis sliding window rate limiter</summary>
public interface IRateLimiter
{
    /// <summary>Kiem tra va tang counter. Tra ve true neu cho phep, false neu vuot limit.</summary>
    Task<bool> AllowAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>Dem so request hien tai trong window</summary>
    Task<long> GetCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);
}
