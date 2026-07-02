namespace ProDiabHis.Domain.Entities;

/// <summary>Nhat ky thao tac he thong. Map bang sec_audit_logs</summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    /// <summary>JSON chua chi tiet thay doi</summary>
    public string? DetailsJson { get; set; }
    /// <summary>Muc do nghiem trong: INFO/WARN/ERROR/CRITICAL</summary>
    public string Severity { get; set; } = "INFO";
    /// <summary>Phat hien truy cap cheo tenant (cross-tenant attempt)</summary>
    public bool CrossTenantAttempt { get; set; } = false;
    /// <summary>HTTP Request ID de trace correlation</summary>
    public string? RequestId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AuditAction
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string Export = "EXPORT";
    public const string Sign = "SIGN";
    public const string EncryptionRotate = "ENCRYPTION_ROTATE";
    public const string FailedLogin = "FAILED_LOGIN";
    public const string CrossTenantAttempt = "CROSS_TENANT_ATTEMPT";
}
