using System.Text.Json.Serialization;

namespace ProDiabHis.Contracts.Auth;

/// <summary>Ket qua dang nhap thanh cong</summary>
public record LoginResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("expiresIn")] int ExpiresIn,
    [property: JsonPropertyName("user")] UserInfo User,
    [property: JsonPropertyName("permissions")] IReadOnlyList<string> Permissions,
    /// <summary>True neu role cua user thuoc danh sach bat buoc 2FA (Security:MandatoryMfaRoles)
    /// nhung user CHUA bat 2FA. FE phai dieu huong bat buoc sang /account/security de thiet lap 2FA
    /// truoc khi cho phep su dung binh thuong.</summary>
    [property: JsonPropertyName("mfaSetupRequired")] bool MfaSetupRequired = false,
    /// <summary>Thong bao tieng Viet khi mfaSetupRequired = true</summary>
    [property: JsonPropertyName("mfaSetupMessage")] string? MfaSetupMessage = null,
    /// <summary>True neu user DA bat 2FA — login chi tra ve buoc 1, CHUA cap AccessToken/RefreshToken.
    /// FE phai dieu huong sang man hinh nhap ma TOTP roi goi POST /api/v1/auth/2fa/verify voi
    /// <see cref="MfaPendingToken"/> + ma 6 so (hoac recovery code) de lay token day du.</summary>
    [property: JsonPropertyName("requires2fa")] bool Requires2fa = false,
    /// <summary>Token tam (aud=mfa-pending, TTL 5 phut) dung cho buoc verify TOTP khi Requires2fa=true.
    /// Khong dung duoc cho API nghiep vu.</summary>
    [property: JsonPropertyName("mfaPendingToken")] string? MfaPendingToken = null,
    /// <summary>Token tam (aud=mfa-setup, TTL 10 phut) khi MfaSetupRequired=true. Chi dung duoc cho
    /// POST /api/v1/users/me/2fa/setup + /me/2fa/enable de bat 2FA lan dau. Khong dung duoc cho API khac.</summary>
    [property: JsonPropertyName("mfaSetupToken")] string? MfaSetupToken = null);

public record UserInfo(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("tenantId")] int TenantId,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("roleCodes")] IReadOnlyList<string> RoleCodes);
