namespace ProDiabHis.Application.Common;

public enum AuditSeverity
{
    INFO,
    WARN,
    ERROR,
    CRITICAL
}

/// <summary>Ghi audit log cho cac thao tac tren du lieu nhay cam</summary>
public interface IAuditService
{
    /// <summary>Ghi mot ban ghi audit log</summary>
    Task LogAsync(
        string action,
        string? resourceType,
        string? resourceId,
        object? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>Ghi audit log voi severity va cross-tenant detection</summary>
    Task LogAsync(
        string action,
        string? resourceType,
        string? resourceId,
        AuditSeverity severity,
        bool crossTenantAttempt = false,
        string? requestId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}
