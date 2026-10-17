namespace ProDiabHis.Domain.Entities;

/// <summary>Refresh token luu trong sec_sessions</summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int TenantId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? IpAddress { get; set; }
    /// <summary>sid phien IdP (SSO). NULL = phien dang nhap mat khau — back-channel logout KHONG dong toi.</summary>
    public string? SsoSid { get; set; }
    /// <summary>sub cua IdP (SSO)</summary>
    public string? SsoSub { get; set; }

    public User? User { get; set; }
}
