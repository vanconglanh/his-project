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
    /// <param name="overrideBranchId">Dung khi doi chi nhanh (POST /me/switch-branch) — ep branch_id claim
    /// ve gia tri nay thay vi branch mac dinh/is_primary cua user (van phai validate thuoc branch_ids truoc khi goi).</param>
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string>? roleCodes = null,
        int? overrideBranchId = null);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);

    /// <summary>Sinh lookup token (aud=visit-lookup, TTL tinh bang giay) cho Public API visit lookup</summary>
    string GenerateLookupToken(string patientCode, int tenantId, int expiresInSeconds);

    /// <summary>Sinh portal session JWT (aud=patient-portal, TTL 24h)</summary>
    string GeneratePortalToken(Guid patientId, string patientCode, int tenantId, out string jti);

    /// <summary>Sinh token tam (aud=mfa-pending, TTL 5 phut) cho buoc 2 dang nhap: user DA bat 2FA can nhap
    /// ma TOTP. Chua cap quyen truy cap API nghiep vu — chi dung de goi POST /api/v1/auth/2fa/verify.</summary>
    string GenerateMfaPendingToken(User user);

    /// <summary>Sinh token tam (aud=mfa-setup, TTL 10 phut) cho user thuoc role bat buoc 2FA nhung chua bat.
    /// Chi dung duoc cho endpoint me/2fa/setup + me/2fa/enable de bat 2FA lan dau.</summary>
    string GenerateMfaSetupToken(User user);

    /// <summary>Xac thuc token tam (mfa-pending / mfa-setup) theo audience mong doi. Tra ve (userId, tenantId)
    /// neu hop le; null neu token sai/het han/sai audience.</summary>
    (Guid UserId, int TenantId)? ValidateMfaToken(string token, string expectedAudience);
}
