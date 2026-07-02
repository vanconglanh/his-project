using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Auth;

/// <summary>Sinh va xac thuc JWT token</summary>
public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);

    /// <summary>Sinh lookup token (aud=visit-lookup, TTL tinh bang giay) cho Public API visit lookup</summary>
    string GenerateLookupToken(string patientCode, int tenantId, int expiresInSeconds);

    /// <summary>Sinh portal session JWT (aud=patient-portal, TTL 24h)</summary>
    string GeneratePortalToken(Guid patientId, string patientCode, int tenantId, out string jti);
}
