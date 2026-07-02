using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// Filter kiem tra nguoi dung co quyen thao tac cu the trong JWT claim permissions[].
/// Tra 403 voi code PERMISSION_DENIED neu thieu quyen.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

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

        var permissions = user.FindAll("permissions").Select(c => c.Value).ToList();
        var isSuperAdmin = user.FindFirst("is_super_admin")?.Value == "true";

        if (!isSuperAdmin && !permissions.Contains(_permission))
        {
            context.Result = new ObjectResult(new
            {
                error = new { code = "PERMISSION_DENIED", message = "Bạn không có quyền thực hiện thao tác này" }
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
