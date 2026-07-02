using System.Text.Json.Serialization;

namespace ProDiabHis.Contracts.Auth;

/// <summary>Ket qua dang nhap thanh cong</summary>
public record LoginResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("expiresIn")] int ExpiresIn,
    [property: JsonPropertyName("user")] UserInfo User,
    [property: JsonPropertyName("permissions")] IReadOnlyList<string> Permissions);

public record UserInfo(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("tenantId")] int TenantId,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("roleCodes")] IReadOnlyList<string> RoleCodes);
