namespace ProDiabHis.Application.PublicApi;

public record PortalSessionToken(string AccessToken, string PatientCode, string FullName, int ExpiresIn);

/// <summary>OTP + session management cho Patient Portal</summary>
public interface IPortalAuthService
{
    /// <summary>Gen OTP 6 digit, hash bcrypt, luu DB + Redis TTL 5 phut, gui SMS</summary>
    Task RequestOtpAsync(string phone, int tenantId, string purpose, CancellationToken cancellationToken = default);

    /// <summary>So hash OTP, neu dung thi gen JWT portal session (24h, aud=patient-portal)</summary>
    Task<PortalSessionToken> VerifyOtpAsync(string phone, string otp, int tenantId, CancellationToken cancellationToken = default);

    /// <summary>Revoke JWT bang JTI (blacklist Redis TTL = remaining lifetime)</summary>
    Task LogoutAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Kiem tra JTI co bi revoke khong</summary>
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
