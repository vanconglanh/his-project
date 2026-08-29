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
    [property: JsonPropertyName("mfaSetupMessage")] string? MfaSetupMessage = null);

public record UserInfo(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("tenantId")] int TenantId,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("roleCodes")] IReadOnlyList<string> RoleCodes);
