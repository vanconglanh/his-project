using Microsoft.AspNetCore.Http;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Auth;

/// <summary>Doc claim "permissions" trong JWT — cung nguon voi RequirePermissionAttribute.</summary>
public class PermissionChecker : IPermissionChecker
{
    private readonly IHttpContextAccessor _accessor;

    public PermissionChecker(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool HasPermission(string permissionCode)
    {
        var user = _accessor.HttpContext?.User;
        if (user is null) return false;

        // Super admin: JWT khong nhoi permissions[] (tranh cookie > 4KB) -> cap quyen tron goi.
        if (user.FindFirst("is_super_admin")?.Value == "true") return true;

        return user.FindAll("permissions").Any(c => c.Value == permissionCode);
    }
}
