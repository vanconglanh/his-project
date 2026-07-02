using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Middlewares;

/// <summary>Middleware lay tenant_id tu JWT claim va set vao ITenantProvider</summary>
public class TenantScopeMiddleware
{
    private readonly RequestDelegate _next;

    public TenantScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx, ITenantProvider provider)
    {
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && int.TryParse(tenantClaim, out var tid))
        {
            provider.SetTenantId(tid);
        }
        // Neu khong co tenant claim, middleware khong set — ITenantProvider giu gia tri mac dinh 0.
        // Request sau do se that bai o HasQueryFilter hoac RequirePermission neu can tenant scope.
        await _next(ctx);
    }
}
