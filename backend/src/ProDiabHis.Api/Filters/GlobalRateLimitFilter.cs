using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// Global rate limit filter ap dung per user (100/min) va per tenant (1000/min).
/// Dung Redis sliding window thong qua IRateLimiter.
/// Dang ky nhu IAsyncAuthorizationFilter de chay som trong pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GlobalRateLimitAttribute : Attribute { }

public class GlobalRateLimitFilter : IAsyncAuthorizationFilter
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<GlobalRateLimitFilter> _logger;

    private const int UserLimitPerMinute = 100;
    private const int TenantLimitPerMinute = 1000;

    public GlobalRateLimitFilter(IRateLimiter rateLimiter, ILogger<GlobalRateLimitFilter> logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst("user_id")?.Value;
        var tenantId = user.FindFirst("tenant_id")?.Value;

        var window = TimeSpan.FromMinutes(1);

        // Per-user limit: 100 req/min
        if (!string.IsNullOrEmpty(userId))
        {
            var userKey = $"user:{tenantId}:{userId}";
            var userAllowed = await _rateLimiter.AllowAsync(userKey, UserLimitPerMinute, window);
            if (!userAllowed)
            {
                _logger.LogWarning("Rate limit exceeded: user={UserId} tenant={TenantId}", userId, tenantId);
                context.Result = new ObjectResult(new
                {
                    error = new
                    {
                        code = "RATE_LIMIT_EXCEEDED",
                        message = "Ban da vuot qua gioi han 100 yeu cau/phut. Vui long thu lai sau."
                    }
                })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                return;
            }
        }

        // Per-tenant limit: 1000 req/min
        if (!string.IsNullOrEmpty(tenantId))
        {
            var tenantKey = $"tenant:{tenantId}";
            var tenantAllowed = await _rateLimiter.AllowAsync(tenantKey, TenantLimitPerMinute, window);
            if (!tenantAllowed)
            {
                _logger.LogWarning("Tenant rate limit exceeded: tenant={TenantId}", tenantId);
                context.Result = new ObjectResult(new
                {
                    error = new
                    {
                        code = "TENANT_RATE_LIMIT_EXCEEDED",
                        message = "Phong kham da vuot qua gioi han 1000 yeu cau/phut."
                    }
                })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
                context.HttpContext.Response.Headers["Retry-After"] = "60";
            }
        }
    }
}
