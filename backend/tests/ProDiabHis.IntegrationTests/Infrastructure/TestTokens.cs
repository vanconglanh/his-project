using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// Sinh JWT that (ky bang dung secret cua test host) de goi API qua HTTP.
/// Claim shape bam sat JwtService.GenerateAccessToken cua production:
/// sub / user_id / tenant_id / email / full_name / permissions[] / is_super_admin
/// / branch_id / branch_ids / branch_cross_view.
/// Nho vay test di qua DUNG pipeline that: JwtBearer -> TenantScopeMiddleware
/// -> BranchScopeMiddleware -> RequirePermissionAttribute.
/// </summary>
public static class TestTokens
{
    /// <summary>Secret dung cho ca test host va viec ky token (>= 32 byte cho HMAC-SHA256).</summary>
    public const string Secret = "prodiab_integration_test_secret_key_0123456789";
    public const string Issuer = "ProDiabHis";
    public const string Audience = "ProDiabHis";

    /// <summary>
    /// Token nguoi dung thuong: chi co dung nhung permission truyen vao.
    /// Moi lan goi dung mot userId khac nhau de tranh dung tran rate limit per-user.
    /// </summary>
    public static string ForPermissions(
        int tenantId,
        Guid userId,
        IEnumerable<string> permissions,
        int branchId = 1,
        IEnumerable<int>? branchIds = null,
        bool crossView = false)
        => Build(tenantId, userId, permissions, isSuperAdmin: false, branchId, branchIds, crossView);

    /// <summary>Token super admin: bypass moi permission check (claim is_super_admin=true).</summary>
    public static string ForSuperAdmin(int tenantId = 1, Guid? userId = null, int branchId = 1)
        => Build(tenantId, userId, Array.Empty<string>(), isSuperAdmin: true, branchId, null, crossView: true);

    /// <summary>Token hop le ve chu ky nhung KHONG co permission nao — dung test 403.</summary>
    public static string WithNoPermission(int tenantId = 1, Guid? userId = null)
        => Build(tenantId, userId, Array.Empty<string>(), isSuperAdmin: false, 1, null, false);

    /// <summary>Token da het han — dung test 401.</summary>
    public static string Expired(int tenantId = 1, Guid? userId = null)
        => Build(tenantId, userId, new[] { "patient.read" }, false, 1, null, false,
            expires: DateTime.UtcNow.AddMinutes(-5));

    private static string Build(
        int tenantId,
        Guid? userIdOrNull,
        IEnumerable<string> permissions,
        bool isSuperAdmin,
        int branchId,
        IEnumerable<int>? branchIds,
        bool crossView,
        DateTime? expires = null)
    {
        // QUAN TRONG: User.Id trong Domain la Guid (BaseEntity.Id) nen JwtService that phat
        // claim sub/user_id dang GUID. Test phai phat dung dinh dang do, neu khong cac controller
        // co Guid.Parse(User.FindFirst("user_id")) se nem exception -> 500 gia.
        var userId = userIdOrNull ?? Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, $"user{userId}@test.vn"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("user_id", userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new("full_name", $"Nguoi dung {userId}"),
            new("branch_id", branchId.ToString()),
            new("branch_ids", string.Join(',', branchIds ?? new[] { branchId })),
            new("branch_cross_view", (crossView || isSuperAdmin) ? "true" : "false")
        };

        if (isSuperAdmin)
        {
            claims.Add(new Claim("is_super_admin", "true"));
            claims.Add(new Claim(ClaimTypes.Role, "SUPER_ADMIN"));
            claims.Add(new Claim("role_code", "super_admin"));
        }
        else
        {
            foreach (var p in permissions)
                claims.Add(new Claim("permissions", p));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
