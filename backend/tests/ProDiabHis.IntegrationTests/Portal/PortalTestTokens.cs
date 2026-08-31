using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProDiabHis.IntegrationTests.Infrastructure;

namespace ProDiabHis.IntegrationTests.Portal;

/// <summary>
/// Sinh JWT hop le cho scheme "PortalBearer" (cong benh nhan).
/// Bam sat JwtService.GeneratePortalToken cua production:
///   issuer   = "ProDiabHis"  (giong TestTokens.Issuer)
///   audience = "patient-portal"  (KHAC voi token noi bo aud="ProDiabHis")
///   claims   = jti / patient_id (GUID) / patient_code / tenant_id
///   ky bang  = TestTokens.Secret (chinh la JWT__SECRET ma test host nap)
///
/// Nho aud rieng "patient-portal", token noi bo thuong (aud="ProDiabHis") se bi
/// PortalBearer tu choi -> 401. Day la ranh gioi bao mat quan trong cua cong benh nhan.
/// </summary>
public static class PortalTestTokens
{
    // Audience RIENG cua cong benh nhan — Program.cs cau hinh ValidAudience="patient-portal".
    public const string PortalAudience = "patient-portal";

    /// <summary>Token portal hop le cho 1 benh nhan cu the.</summary>
    public static string ForPatient(Guid patientId, int tenantId, string patientCode = "BN000001")
        => Build(patientId, tenantId, patientCode);

    /// <summary>Token portal da het han — dung test 401 (ValidateLifetime=true, ClockSkew=0).</summary>
    public static string Expired(Guid patientId, int tenantId, string patientCode = "BN000001")
        => Build(patientId, tenantId, patientCode, expires: DateTime.UtcNow.AddMinutes(-5));

    /// <summary>
    /// Token ky dung secret nhung SAI audience (aud="ProDiabHis" nhu token noi bo).
    /// Dung de chung minh endpoint portal tu choi token khong phai cua cong benh nhan.
    /// </summary>
    public static string WithWrongAudience(Guid patientId, int tenantId, string patientCode = "BN000001")
        => Build(patientId, tenantId, patientCode, audience: TestTokens.Audience);

    private static string Build(
        Guid patientId,
        int tenantId,
        string patientCode,
        string audience = PortalAudience,
        DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("patient_id", patientId.ToString()),
            new("patient_code", patientCode),
            new("tenant_id", tenantId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestTokens.Secret));
        var token = new JwtSecurityToken(
            issuer: TestTokens.Issuer,
            audience: audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
