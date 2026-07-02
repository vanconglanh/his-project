namespace ProDiabHis.Application.Roles;

public record RoleResponse(
    string Code,
    string Name,
    string? Description,
    string RoleType,
    int? TenantId,
    IReadOnlyList<string> PermissionCodes
);

public record PermissionResponse(
    string Code,
    string Resource,
    string Action,
    string? Description
);
