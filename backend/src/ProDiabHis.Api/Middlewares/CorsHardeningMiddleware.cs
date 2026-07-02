namespace ProDiabHis.Api.Middlewares;

/// <summary>
/// CORS hardening: chi allow origins trong whitelist tu config CorsHardening:AllowedOrigins.
/// Tra 403 neu Origin khong nam trong whitelist (preflight or actual request).
/// </summary>
public class CorsHardeningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<CorsHardeningMiddleware> _logger;

    public CorsHardeningMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<CorsHardeningMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var origins = configuration.GetSection("CorsHardening:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        _allowedOrigins = new HashSet<string>(origins, StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        // Chi enforce khi co Origin header (browser requests)
        if (!string.IsNullOrEmpty(origin) && _allowedOrigins.Count > 0)
        {
            if (!_allowedOrigins.Contains(origin))
            {
                _logger.LogWarning("CORS blocked: Origin={Origin} Path={Path}",
                    origin, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new { code = "CORS_BLOCKED", message = "Origin khong duoc phep truy cap API nay" }
                });
                return;
            }
        }

        await _next(context);
    }
}
