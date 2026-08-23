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
    /// <paramref name="systemRoleCodes"/> (optional) la tap con CUA roleCodes — CHI chua ma cua nhung role
    /// ma Role.RoleType thuc su la System (role seed that, khong ai tao duoc qua API tao role CUSTOM).
    /// Claim "is_super_admin" CHI duoc gan true khi 1 ma trong systemRoleCodes nam trong danh sach
    /// ReservedRoleCodes (ADMIN/SUPER_ADMIN) — tuyet doi KHONG duoc suy ra tu roleCodes (co the chua ma
    /// cua role CUSTOM do tenant tu tao trung mao mã reserved).
    /// </summary>
    string GenerateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string>? roleCodes = null,
        IEnumerable<string>? systemRoleCodes = null);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);

    /// <summary>Sinh lookup token (aud=visit-lookup, TTL tinh bang giay) cho Public API visit lookup</summary>
    string GenerateLookupToken(string patientCode, int tenantId, int expiresInSeconds);

    /// <summary>Sinh portal session JWT (aud=patient-portal, TTL 24h)</summary>
    string GeneratePortalToken(Guid patientId, string patientCode, int tenantId, out string jti);
}
