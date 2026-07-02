using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirst("user_id")?.Value;
            return val != null && Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public int? TenantId
    {
        get
        {
            var val = User?.FindFirst("tenant_id")?.Value;
            return val != null && int.TryParse(val, out var tid) ? tid : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];
}
