using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// Filter chi cho phep SUPER_ADMIN (is_super_admin=true trong JWT) truy cap.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireSuperAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = new { code = "AUTH_TOKEN_INVALID", message = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại" }
            });
            return;
        }

        var isSuperAdmin = user.FindFirst("is_super_admin")?.Value == "true";
        if (!isSuperAdmin)
        {
            context.Result = new ObjectResult(new
            {
                error = new { code = "PERMISSION_DENIED", message = "Bạn không có quyền thực hiện thao tác này" }
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
