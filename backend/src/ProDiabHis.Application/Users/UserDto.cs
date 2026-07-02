namespace ProDiabHis.Application.Users;

public record RoleRef(string Code, string Name);

public record UserResponse(
    Guid Id,
    int TenantId,
    string Email,
    string FullName,
    string? Phone,
    string? AvatarUrl,
    string Status,
    IReadOnlyList<RoleRef> Roles,
    IReadOnlyList<string> Permissions,
    bool TwoFaEnabled,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);
