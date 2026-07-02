using Dapper;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.PublicApi;
using StackExchange.Redis;

namespace ProDiabHis.Infrastructure.ApiKey;

public class PortalAuthServiceImpl : IPortalAuthService
{
    private readonly Application.Common.IDapperConnectionFactory _factory;
    private readonly ISmsGateway _sms;
    private readonly IJwtService _jwt;
    private readonly IConnectionMultiplexer? _redis;

    public PortalAuthServiceImpl(
        Application.Common.IDapperConnectionFactory factory,
        ISmsGateway sms,
        IJwtService jwt,
        IConnectionMultiplexer? redis = null)
    {
        _factory = factory;
        _sms = sms;
        _jwt = jwt;
        _redis = redis;
    }

    public async Task RequestOtpAsync(string phone, int tenantId, string purpose, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();
        var otp = Random.Shared.Next(100000, 999999).ToString();
        var hash = BCrypt.Net.BCrypt.HashPassword(otp, 10);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_otp_log
                (id, tenant_id, phone, otp_hash, purpose, sent_at, expires_at, attempts)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @Phone, @Hash, @Purpose,
                      UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 5 MINUTE), 0)",
            new { Id = Guid.NewGuid().ToString(), TenantId = tenantId, Phone = phone, Hash = hash, Purpose = purpose });

        await _sms.SendAsync(phone, $"[Pro-Diab] Ma OTP: {otp}. Het han sau 5 phut.", cancellationToken);
    }

    public async Task<PortalSessionToken> VerifyOtpAsync(string phone, string otp, int tenantId, CancellationToken cancellationToken = default)
    {
        using var conn = _factory.CreateConnection();

        var account = await conn.QueryFirstOrDefaultAsync<(string patient_id, string? patient_code, string? full_name)>(
            @"SELECT BIN_TO_UUID(pa.patient_id) AS patient_id, p.patient_code, p.full_name
              FROM diab_his_pat_portal_accounts pa
              JOIN his_patient p ON p.id = pa.patient_id
              WHERE pa.tenant_id = @TenantId AND pa.phone = @Phone",
            new { TenantId = tenantId, Phone = phone });

        if (account == default) throw new Exception("PORTAL_PHONE_NOT_REGISTERED");

        var otpLog = await conn.QueryFirstOrDefaultAsync<(string id, string otp_hash, DateTime expires_at, int attempts)>(
            @"SELECT BIN_TO_UUID(id) AS id, otp_hash, expires_at, attempts
              FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone AND verified_at IS NULL
              ORDER BY sent_at DESC LIMIT 1",
            new { TenantId = tenantId, Phone = phone });

        if (otpLog == default || otpLog.expires_at < DateTime.UtcNow)
            throw new OtpExpiredException();

        if (!BCrypt.Net.BCrypt.Verify(otp, otpLog.otp_hash))
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_pat_portal_otp_log SET attempts = attempts + 1 WHERE id = UUID_TO_BIN(@Id)",
                new { Id = otpLog.id });
            if (otpLog.attempts + 1 >= 5) throw new OtpTooManyAttemptsException();
            throw new OtpInvalidException();
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_otp_log SET verified_at = UTC_TIMESTAMP() WHERE id = UUID_TO_BIN(@Id)",
            new { Id = otpLog.id });

        var patientId = Guid.Parse(account.patient_id);
        var accessToken = _jwt.GeneratePortalToken(patientId, account.patient_code!, tenantId, out var jti);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_sessions
                (id, tenant_id, patient_id, jti, issued_at, expires_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@PatientId), @Jti,
                      UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 24 HOUR))",
            new { Id = Guid.NewGuid().ToString(), TenantId = tenantId, PatientId = account.patient_id, Jti = jti });

        return new PortalSessionToken(accessToken, account.patient_code!, account.full_name!, 86400);
    }

    public async Task LogoutAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var remaining = expiresAt - DateTime.UtcNow;
        if (remaining > TimeSpan.Zero && _redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"portal:revoked:{jti}", "1", remaining);
        }
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (_redis == null) return false;
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"portal:revoked:{jti}");
    }
}
