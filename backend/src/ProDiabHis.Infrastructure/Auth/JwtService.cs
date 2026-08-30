using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProDiabHis.Application.Auth;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string>? roleCodes = null,
        int? overrideBranchId = null)
    {
        var secret = _configuration["JWT__SECRET"]
            ?? _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("user_id", user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("full_name", user.FullName)
        };

        var roleList = roles.ToList();
        foreach (var role in roleList)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Ma role on dinh (vd "bac_si") — dung cho tinh nang can so sanh chinh xac (chia se bao cao theo
        // role...) thay vi ClaimTypes.Role (ten hien thi tieng Viet, khong on dinh de so sanh/i18n).
        foreach (var code in roleCodes ?? Enumerable.Empty<string>())
            claims.Add(new Claim("role_code", code));

        // SUPER_ADMIN / ADMIN / Quan tri vien bypass tat ca permission check
        var isAdmin = roleList.Any(r =>
            r.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quan tri", StringComparison.OrdinalIgnoreCase));
        if (isAdmin)
            claims.Add(new Claim("is_super_admin", "true"));

        // Load permissions tu user_roles -> role_permissions.
        // Super admin da bypass moi permission check qua claim is_super_admin (xem RequirePermissionAttribute
        // + frontend usePermissions) nen mang permissions la thua. KHONG nhoi permissions vao JWT cho admin:
        // super admin co ~150 quyen -> token > 4KB -> vuot gioi han ~4096 bytes/cookie -> trinh duyet drop
        // cookie his-access-token -> proxy.ts khong thay cookie -> redirect /login (login loop). (FIX login admin)
        // Van tinh permissions (dung cho hasCrossView ben duoi) nhung chi nhoi vao claim khi khong phai admin.
        var permissions = LoadPermissions(user.Id).ToList();
        if (!isAdmin)
        {
            foreach (var perm in permissions)
                claims.Add(new Claim("permissions", perm));
        }

        // Branch context (D7): branch_id (dang lam viec), branch_ids (CSV cac branch duoc gan),
        // branch_cross_view (bool). is_super_admin bypass toan bo filter chi nhanh.
        var hasCrossView = isAdmin || permissions.Contains("branch.cross_view");
        var (defaultBranchId, allowedBranchIds) = LoadBranchContext(user.Id, user.TenantId);
        var branchId = overrideBranchId ?? defaultBranchId;
        claims.Add(new Claim("branch_id", branchId?.ToString() ?? "0"));
        claims.Add(new Claim("branch_ids", string.Join(',', allowedBranchIds)));
        claims.Add(new Claim("branch_cross_view", hasCrossView ? "true" : "false"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ProDiabHis",
            audience: _configuration["Jwt:Audience"] ?? "ProDiabHis",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public bool ValidateRefreshToken(string token)
    {
        // Basic validation — DB check done in handler
        return !string.IsNullOrWhiteSpace(token) && token.Length >= 64;
    }

    public string GenerateLookupToken(string patientCode, int tenantId, int expiresInSeconds)
    {
        var (key, creds) = GetSigningCredentials();
        var jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new("patient_code", patientCode),
            new("tenant_id", tenantId.ToString()),
            new("purpose", "visit-lookup")
        };
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ProDiabHis",
            audience: "visit-lookup",
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GeneratePortalToken(Guid patientId, string patientCode, int tenantId, out string jti)
    {
        var (key, creds) = GetSigningCredentials();
        jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new("patient_id", patientId.ToString()),
            new("patient_code", patientCode),
            new("tenant_id", tenantId.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ProDiabHis",
            audience: "patient-portal",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateMfaPendingToken(User user)
        => GenerateMfaToken(user, "mfa-pending", TimeSpan.FromMinutes(5));

    public string GenerateMfaSetupToken(User user)
        => GenerateMfaToken(user, "mfa-setup", TimeSpan.FromMinutes(10));

    private string GenerateMfaToken(User user, string audience, TimeSpan ttl)
    {
        var (_, creds) = GetSigningCredentials();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("user_id", user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("purpose", audience)
        };
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ProDiabHis",
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(ttl),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid UserId, int TenantId)? ValidateMfaToken(string token, string expectedAudience)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var (key, _) = GetSigningCredentials();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "ProDiabHis",
            ValidAudience = expectedAudience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParams, out _);
            var uid = principal.FindFirst("user_id")?.Value;
            var tid = principal.FindFirst("tenant_id")?.Value;
            if (uid != null && Guid.TryParse(uid, out var userId)
                && tid != null && int.TryParse(tid, out var tenantId))
            {
                return (userId, tenantId);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<string> LoadPermissions(Guid userId)
    {
        try
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connStr)) return Array.Empty<string>();
            using var conn = new MySqlConnector.MySqlConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT DISTINCT p.code FROM sec_user_roles ur
                                JOIN sec_role_permissions rp ON rp.role_id = ur.role_id
                                JOIN sec_permissions p ON p.id = rp.permission_id
                                WHERE ur.user_id = @uid";
            cmd.Parameters.AddWithValue("@uid", userId.ToString());
            using var rd = cmd.ExecuteReader();
            var list = new List<string>();
            while (rd.Read()) list.Add(rd.GetString(0));
            return list;
        }
        catch { return Array.Empty<string>(); }
    }

    /// <summary>Tra ve (branch_id mac dinh khi dang nhap, danh sach branch_id duoc gan) cho user.
    /// Neu user chua duoc gan branch nao (du lieu cu chua backfill) -> fallback branch mac dinh cua tenant.</summary>
    private (int? DefaultBranchId, List<int> AllowedBranchIds) LoadBranchContext(Guid userId, int tenantId)
    {
        try
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connStr)) return (null, new List<int>());
            using var conn = new MySqlConnector.MySqlConnection(connStr);
            conn.Open();

            var allowed = new List<int>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT branch_id, is_primary FROM diab_his_sec_user_branches
                                     WHERE user_id = @uid AND deleted_at IS NULL";
                cmd.Parameters.AddWithValue("@uid", userId.ToString());
                using var rd = cmd.ExecuteReader();
                int? primary = null;
                while (rd.Read())
                {
                    var bid = rd.GetInt32(0);
                    allowed.Add(bid);
                    var isPrimary = !rd.IsDBNull(1) && Convert.ToInt64(rd.GetValue(1)) != 0;
                    if (isPrimary) primary = bid;
                }
                if (primary.HasValue)
                    return (primary, allowed);
                if (allowed.Count > 0)
                    return (allowed[0], allowed);
            }

            // Fallback: chua co dong nao trong user_branches -> lay branch mac dinh cua tenant
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT id FROM diab_his_sys_branches
                                     WHERE tenant_id = @tid AND is_default = 1 AND deleted_at IS NULL LIMIT 1";
                cmd.Parameters.AddWithValue("@tid", tenantId);
                var result = cmd.ExecuteScalar();
                var defaultBranchId = result != null && result != DBNull.Value ? Convert.ToInt32(result) : (int?)null;
                return (defaultBranchId, defaultBranchId.HasValue ? new List<int> { defaultBranchId.Value } : new List<int>());
            }
        }
        catch { return (null, new List<int>()); }
    }

    private (SymmetricSecurityKey, SigningCredentials) GetSigningCredentials()
    {
        var secret = _configuration["JWT__SECRET"]
            ?? _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        return (key, new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
    }
}
