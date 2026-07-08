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

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string>? roleCodes = null)
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

        // Load permissions tu user_roles -> role_permissions
        foreach (var perm in LoadPermissions(user.Id))
            claims.Add(new Claim("permissions", perm));

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

    private (SymmetricSecurityKey, SigningCredentials) GetSigningCredentials()
    {
        var secret = _configuration["JWT__SECRET"]
            ?? _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        return (key, new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
    }
}
