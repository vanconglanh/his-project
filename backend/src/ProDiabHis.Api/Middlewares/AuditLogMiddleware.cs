using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Middlewares;

/// <summary>Middleware ghi audit log cho cac request thay doi du lieu</summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    private static readonly HashSet<string> AuditMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx, ICurrentUser currentUser)
    {
        await _next(ctx);

        if (AuditMethods.Contains(ctx.Request.Method))
        {
            _logger.LogInformation(
                "AUDIT | Method={Method} Path={Path} StatusCode={StatusCode} UserId={UserId} TenantId={TenantId}",
                ctx.Request.Method,
                ctx.Request.Path,
                ctx.Response.StatusCode,
                currentUser.UserId,
                currentUser.TenantId);
        }
    }
}
