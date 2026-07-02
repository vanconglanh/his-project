namespace ProDiabHis.Application.AuditLogs;

public record AuditLogResponse(
    Guid Id,
    int TenantId,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string? ResourceType,
    string? ResourceId,
    string? IpAddress,
    string? UserAgent,
    object? Details,
    DateTime CreatedAt,
    string Severity = "INFO",
    bool CrossTenantAttempt = false,
    string? RequestId = null
);
