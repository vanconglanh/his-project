using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Tai khoan cong benh nhan. Map bang diab_his_pat_portal_accounts</summary>
public class PortalAccount : BaseEntity
{
    public int TenantId { get; set; }
    public Guid PatientId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastOtpSentAt { get; set; }
}

/// <summary>Log OTP da gui. Map bang diab_his_pat_portal_otp_log</summary>
public class PortalOtpLog : BaseEntity
{
    public int TenantId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string OtpHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = OtpPurpose.Login;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; } = 0;
}

/// <summary>Session JWT cong benh nhan (revocation list). Map bang diab_his_pat_portal_sessions</summary>
public class PortalSession : BaseEntity
{
    public int TenantId { get; set; }
    public Guid PatientId { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public static class OtpPurpose
{
    public const string Login = "LOGIN";
    public const string Lookup = "LOOKUP";
}
