using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Auth;

/// <summary>Sinh va xac thuc JWT token</summary>
public interface IJwtService
{
    /// <summary>
    /// Sinh access token. <paramref name="roles"/> la ten hien thi (vd "Bác sĩ") — giu nguyen de tuong thich
    /// nguoc (ClaimTypes.Role). <paramref name="roleCodes"/> (optional) la ma role on dinh (vd "bac_si") —
    /// nhung tinh nang can so sanh chinh xac (chia se bao cao theo role...) nen dung claim "role_code" nay
    /// thay vi Roles (ten hien thi, co the trung/doi theo ngon ngu).
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string>? roleCodes = null);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);

    /// <summary>Sinh lookup token (aud=visit-lookup, TTL tinh bang giay) cho Public API visit lookup</summary>
    string GenerateLookupToken(string patientCode, int tenantId, int expiresInSeconds);

    /// <summary>Sinh portal session JWT (aud=patient-portal, TTL 24h)</summary>
    string GeneratePortalToken(Guid patientId, string patientCode, int tenantId, out string jti);
}
