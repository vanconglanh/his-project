using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Middlewares;

/// <summary>
/// Set tenant cho request. Thu tu resolve:
///  1. JWT claim tenant_id (request da dang nhap)
///  2. Host header (request chua auth, vd trang login tren domain tenant) qua catalog
/// Validate: neu token (claim) va domain (host) tro ve tenant KHAC nhau -> 403 (chong dung
/// token tenant A tren domain tenant B). Giai doan shared-DB: catalog chua co domain -> host
/// resolve ra null -> hanh vi giu nguyen nhu cu (chi dung JWT claim).
/// </summary>
public class TenantScopeMiddleware
{
    private readonly RequestDelegate _next;

    public TenantScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx, ITenantProvider provider, ITenantConnectionResolver resolver)
    {
        var hostTenant = resolver.ResolveByHost(ctx.Request.Host.Host);

        int? claimTid = null;
        var tenantClaim = ctx.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && int.TryParse(tenantClaim, out var parsed))
            claimTid = parsed;

        if (claimTid.HasValue)
        {
            if (hostTenant is not null && hostTenant.TenantId != claimTid.Value)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = new { code = "TENANT_HOST_MISMATCH", message = "Phiên đăng nhập không hợp lệ với tên miền này" }
                });
                return;
            }
            provider.SetTenantId(claimTid.Value);
        }
        else if (hostTenant is not null)
        {
            // Request chua auth (login...) tren domain tenant -> set de handler tim user dung DB tenant
            provider.SetTenantId(hostTenant.TenantId);
        }
        // else: khong claim + khong host tenant -> giu mac dinh 0 (super-admin portal / localhost / health check)

        await _next(ctx);
    }
}
