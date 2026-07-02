using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProDiabHis.Application.PublicApi;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// IAsyncAuthorizationFilter cho Public API.
/// Doc X-Api-Key, hash SHA-256, lookup partner, kiem tra scope/IP/expiry, rate limit Redis.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ApiKeyAuthAttribute : Attribute
{
    public string Scope { get; set; } = string.Empty;
}

public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
{
    private readonly IApiKeyStore _keyStore;
    private readonly IRateLimiter _rateLimiter;

    public ApiKeyAuthFilter(IApiKeyStore keyStore, IRateLimiter rateLimiter)
    {
        _keyStore = keyStore;
        _rateLimiter = rateLimiter;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var sw = Stopwatch.StartNew();

        // Get required scope from attribute
        var apiKeyAttr = context.ActionDescriptor.EndpointMetadata
            .OfType<ApiKeyAuthAttribute>()
            .FirstOrDefault();

        if (!request.Headers.TryGetValue("X-Api-Key", out var rawKey) || string.IsNullOrWhiteSpace(rawKey))
        {
            context.Result = ErrorResult("API_KEY_INVALID", "API key khong hop le", StatusCodes.Status401Unauthorized);
            return;
        }

        var hash = ComputeSha256(rawKey!);
        var partner = await _keyStore.FindByHashAsync(hash);

        if (partner == null)
        {
            context.Result = ErrorResult("API_KEY_INVALID", "API key khong hop le", StatusCodes.Status401Unauthorized);
            await LogRequest(null, 0, context, 401, "API_KEY_INVALID", sw);
            return;
        }

        // Status check
        if (partner.Status != "ACTIVE")
        {
            context.Result = ErrorResult("API_KEY_INVALID", "API key da bi vo hieu hoa", StatusCodes.Status401Unauthorized);
            await LogRequest(partner, partner.TenantId, context, 401, "API_KEY_DISABLED", sw);
            return;
        }

        // Expiry check
        if (partner.ExpiresAt.HasValue && partner.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Result = ErrorResult("API_KEY_EXPIRED", "API key da het han", StatusCodes.Status401Unauthorized);
            await LogRequest(partner, partner.TenantId, context, 401, "API_KEY_EXPIRED", sw);
            return;
        }

        // IP whitelist check
        if (partner.IpWhitelist.Count > 0)
        {
            var clientIp = GetClientIp(context.HttpContext);
            if (!partner.IpWhitelist.Contains(clientIp))
            {
                context.Result = ErrorResult("API_KEY_INVALID", "IP khong trong whitelist", StatusCodes.Status403Forbidden);
                await LogRequest(partner, partner.TenantId, context, 403, "IP_NOT_WHITELISTED", sw);
                return;
            }
        }

        // Scope check
        if (apiKeyAttr != null && !string.IsNullOrEmpty(apiKeyAttr.Scope))
        {
            if (!partner.Scopes.Contains(apiKeyAttr.Scope))
            {
                context.Result = ErrorResult("API_SCOPE_DENIED", "API key khong co quyen cho endpoint nay", StatusCodes.Status403Forbidden);
                await LogRequest(partner, partner.TenantId, context, 403, "API_SCOPE_DENIED", sw);
                return;
            }
        }

        // Per-minute rate limit
        var minuteKey = $"{partner.PartnerId}:min:{DateTime.UtcNow:yyyyMMddHHmm}";
        var allowed = await _rateLimiter.AllowAsync(minuteKey, partner.RateLimitPerMin, TimeSpan.FromMinutes(1));
        if (!allowed)
        {
            context.Result = ErrorResult("API_RATE_LIMITED", "Vuot gioi han truy cap, thu lai sau", StatusCodes.Status429TooManyRequests);
            await LogRequest(partner, partner.TenantId, context, 429, "API_RATE_LIMITED", sw);
            return;
        }

        // Daily quota
        var dailyKey = $"{partner.PartnerId}:daily:{DateTime.UtcNow:yyyyMMdd}";
        var dailyCount = await _rateLimiter.GetCountAsync(dailyKey, TimeSpan.FromDays(1));
        if (dailyCount >= partner.DailyQuota)
        {
            context.Result = ErrorResult("API_QUOTA_EXCEEDED", "Vuot han muc ngay, thu lai vao ngay mai", StatusCodes.Status429TooManyRequests);
            await LogRequest(partner, partner.TenantId, context, 429, "API_QUOTA_EXCEEDED", sw);
            return;
        }
        // Increment daily counter
        await _rateLimiter.AllowAsync(dailyKey, int.MaxValue, TimeSpan.FromDays(2));

        // Store partner context in HttpContext for handlers
        context.HttpContext.Items["ApiPartner"] = partner;
        context.HttpContext.Items["TenantId"] = partner.TenantId;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static string GetClientIp(HttpContext ctx)
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static ObjectResult ErrorResult(string code, string message, int statusCode)
        => new(new { error = new { code, message } }) { StatusCode = statusCode };

    private async Task LogRequest(ApiPartnerContext? partner, int tenantId,
        AuthorizationFilterContext context, int statusCode, string? errorCode, Stopwatch sw)
    {
        if (partner == null) return;
        sw.Stop();
        var req = context.HttpContext.Request;
        var ip = GetClientIp(context.HttpContext);
        await _keyStore.LogRequestAsync(partner.PartnerId, tenantId,
            req.Method, req.Path, statusCode, (int)sw.ElapsedMilliseconds, ip, errorCode);
    }
}
