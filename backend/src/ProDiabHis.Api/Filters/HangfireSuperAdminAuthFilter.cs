using Hangfire.Dashboard;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// Hangfire dashboard chi cho phep SUPER_ADMIN truy cap.
/// Kiem tra JWT claim is_super_admin=true.
/// </summary>
public class HangfireSuperAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        return httpContext.User.FindFirst("is_super_admin")?.Value == "true";
    }
}
