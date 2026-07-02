using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Nguoi dung he thong. Map bang sec_users</summary>
public class User : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = UserStatus.Pending;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    /// <summary>Token moi (64 char hex), su dung 1 lan</summary>
    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiresAt { get; set; }
    /// <summary>TOTP secret ma hoa AES-256-GCM</summary>
    public string? TwoFaSecret { get; set; }
    public bool TwoFaEnabled { get; set; } = false;
    /// <summary>Recovery codes ma hoa, luu JSON</summary>
    public string? TwoFaRecoveryCodesJson { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public static class UserStatus
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Locked = "LOCKED";
    public const string Disabled = "DISABLED";
}
