using Dapper;
using MediatR;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Patient Portal — dang nhap bang MA KICH HOAT (le tan cap) + PIN 6 so.
// Khong phu thuoc SMS (MVP mien phi). Quen PIN -> OTP qua email.
// Tai dung bang diab_his_pat_portal_accounts (cot pin_hash, activation_code_hash...).
// ============================================================

// --- DTOs ---
public record PortalActivateRequest(string Phone, string ActivationCode, string Pin);
public record PortalPinLoginRequest(string Phone, string Pin);
public record PortalForgotPinRequest(string Phone);
public record PortalResetPinRequest(string Phone, string Otp, string NewPin);
public record PortalTenantInfoResponse(int TenantId, string Name, string? LogoUrl, string? VapidPublicKey);
public record PortalIssueActivationResponse(string ActivationCode, DateTime ExpiresAt);

// --- Exceptions ---
public class PortalActivationInvalidException : Exception { public PortalActivationInvalidException() : base("PORTAL_ACTIVATION_INVALID") { } }
public class PortalPinInvalidException : Exception { public PortalPinInvalidException() : base("PORTAL_PIN_INVALID") { } }
public class PortalNotActivatedException : Exception { public PortalNotActivatedException() : base("PORTAL_NOT_ACTIVATED") { } }
public class PortalAccountLockedException : Exception { public PortalAccountLockedException() : base("PORTAL_ACCOUNT_LOCKED") { } }

internal static class PortalPinRules
{
    public static bool IsValidPin(string pin) => pin is { Length: 6 } && pin.All(char.IsDigit);
}

// ============================================================
// Le tan cap ma kich hoat cho benh nhan (in kem phieu kham / doc mieng)
// ============================================================
public record IssuePortalActivationCommand(Guid PatientId, int TenantId) : IRequest<PortalIssueActivationResponse>;

public class IssuePortalActivationHandler : IRequestHandler<IssuePortalActivationCommand, PortalIssueActivationResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPasswordHasher _hasher;
    public IssuePortalActivationHandler(IDapperConnectionFactory db, IPasswordHasher hasher)
    {
        _db = db; _hasher = hasher;
    }

    public async Task<PortalIssueActivationResponse> Handle(IssuePortalActivationCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var patient = await conn.QueryFirstOrDefaultAsync<(string id, string? phone)>(
            "SELECT id, phone FROM diab_his_pat_patients WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = cmd.PatientId.ToString(), cmd.TenantId });
        if (patient.id == null) throw new PatientNotFoundException();
        if (string.IsNullOrWhiteSpace(patient.phone)) throw new PortalPhoneNotRegisteredException();

        // Ma 8 ky tu de doc: bo O/0/I/1 gay nham
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new string(Enumerable.Range(0, 8).Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]).ToArray());
        var codeHash = _hasher.Hash(code);
        var expiresAt = DateTime.UtcNow.AddHours(72);

        // Upsert theo (tenant_id, phone): tao tai khoan chua kich hoat neu chua co
        var existing = await conn.ExecuteScalarAsync<string?>(
            "SELECT id FROM diab_his_pat_portal_accounts WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, patient.phone });

        if (existing == null)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pat_portal_accounts
                    (id, tenant_id, patient_id, phone, activation_code_hash, activation_expires_at, created_at, updated_at)
                  VALUES (@Id, @TenantId, @PatientId, @Phone, @CodeHash, @ExpiresAt, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
                new { Id = Guid.NewGuid().ToString(), cmd.TenantId, PatientId = patient.id, patient.phone, CodeHash = codeHash, ExpiresAt = expiresAt });
        }
        else
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_pat_portal_accounts
                  SET activation_code_hash = @CodeHash, activation_expires_at = @ExpiresAt,
                      failed_attempts = 0, locked_until = NULL, updated_at = UTC_TIMESTAMP()
                  WHERE tenant_id = @TenantId AND phone = @Phone",
                new { CodeHash = codeHash, ExpiresAt = expiresAt, cmd.TenantId, patient.phone });
        }

        return new PortalIssueActivationResponse(code, expiresAt);
    }
}

// ============================================================
// Benh nhan kich hoat: SDT + ma kich hoat -> dat PIN -> tra token
// ============================================================
public record PortalActivateCommand(string Phone, string ActivationCode, string Pin, int TenantId) : IRequest<PortalAuthResponse>;

public class PortalActivateHandler : IRequestHandler<PortalActivateCommand, PortalAuthResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    public PortalActivateHandler(IDapperConnectionFactory db, IPasswordHasher hasher, IJwtService jwt)
    {
        _db = db; _hasher = hasher; _jwt = jwt;
    }

    public async Task<PortalAuthResponse> Handle(PortalActivateCommand cmd, CancellationToken cancellationToken)
    {
        if (!PortalPinRules.IsValidPin(cmd.Pin)) throw new PortalPinInvalidException();

        using var conn = _db.CreateConnection();
        var acc = await conn.QueryFirstOrDefaultAsync<(string patient_id, string? activation_code_hash, DateTime? activation_expires_at)>(
            @"SELECT patient_id, activation_code_hash, activation_expires_at
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (acc.patient_id == null || acc.activation_code_hash == null
            || acc.activation_expires_at == null || acc.activation_expires_at < DateTime.UtcNow
            || !_hasher.Verify(cmd.ActivationCode.Trim().ToUpperInvariant(), acc.activation_code_hash))
            throw new PortalActivationInvalidException();

        var pinHash = _hasher.Hash(cmd.Pin);
        await conn.ExecuteAsync(
            @"UPDATE diab_his_pat_portal_accounts
              SET pin_hash = @PinHash, activated_at = UTC_TIMESTAMP(),
                  activation_code_hash = NULL, activation_expires_at = NULL,
                  failed_attempts = 0, locked_until = NULL, updated_at = UTC_TIMESTAMP()
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { PinHash = pinHash, cmd.TenantId, cmd.Phone });

        return await IssueTokenAsync(conn, acc.patient_id, cmd.TenantId, _jwt);
    }

    internal static async Task<PortalAuthResponse> IssueTokenAsync(
        System.Data.IDbConnection conn, string patientId, int tenantId, IJwtService jwt)
    {
        var patient = await conn.QueryFirstAsync<(string patient_code, string full_name)>(
            "SELECT code AS patient_code, full_name FROM diab_his_pat_patients WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = patientId, TenantId = tenantId });

        var token = jwt.GeneratePortalToken(Guid.Parse(patientId), patient.patient_code, tenantId, out var jti);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_sessions (id, tenant_id, patient_id, jti, issued_at, expires_at)
              VALUES (@Id, @TenantId, @PatientId, @Jti, UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 DAY))",
            new { Id = Guid.NewGuid().ToString(), TenantId = tenantId, PatientId = patientId, Jti = jti });

        // Phien 30 ngay (nguoi lon tuoi it phai dang nhap lai)
        return new PortalAuthResponse(token, "Bearer", 30 * 86400, patient.patient_code, patient.full_name);
    }
}

// ============================================================
// Dang nhap PIN: SDT + PIN
// ============================================================
public record PortalPinLoginCommand(string Phone, string Pin, int TenantId) : IRequest<PortalAuthResponse>;

public class PortalPinLoginHandler : IRequestHandler<PortalPinLoginCommand, PortalAuthResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    public PortalPinLoginHandler(IDapperConnectionFactory db, IPasswordHasher hasher, IJwtService jwt)
    {
        _db = db; _hasher = hasher; _jwt = jwt;
    }

    public async Task<PortalAuthResponse> Handle(PortalPinLoginCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var acc = await conn.QueryFirstOrDefaultAsync<(string patient_id, string? pin_hash, int failed_attempts, DateTime? locked_until)>(
            @"SELECT patient_id, pin_hash, failed_attempts, locked_until
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (acc.patient_id == null) throw new PortalPhoneNotRegisteredException();
        if (acc.locked_until.HasValue && acc.locked_until.Value > DateTime.UtcNow) throw new PortalAccountLockedException();
        if (acc.pin_hash == null) throw new PortalNotActivatedException();

        if (!_hasher.Verify(cmd.Pin, acc.pin_hash))
        {
            var attempts = acc.failed_attempts + 1;
            if (attempts >= 5)
                await conn.ExecuteAsync(
                    @"UPDATE diab_his_pat_portal_accounts
                      SET failed_attempts = @A, locked_until = DATE_ADD(UTC_TIMESTAMP(), INTERVAL 15 MINUTE)
                      WHERE tenant_id = @TenantId AND phone = @Phone",
                    new { A = attempts, cmd.TenantId, cmd.Phone });
            else
                await conn.ExecuteAsync(
                    "UPDATE diab_his_pat_portal_accounts SET failed_attempts = @A WHERE tenant_id = @TenantId AND phone = @Phone",
                    new { A = attempts, cmd.TenantId, cmd.Phone });

            if (attempts >= 5) throw new PortalAccountLockedException();
            throw new PortalPinInvalidException();
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_accounts SET failed_attempts = 0, locked_until = NULL WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        return await PortalActivateHandler.IssueTokenAsync(conn, acc.patient_id, cmd.TenantId, _jwt);
    }
}

// ============================================================
// Quen PIN: gui OTP qua email (neu co email trong ho so)
// ============================================================
public record PortalForgotPinCommand(string Phone, int TenantId) : IRequest;

public class PortalForgotPinHandler : IRequestHandler<PortalForgotPinCommand>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _email;
    public PortalForgotPinHandler(IDapperConnectionFactory db, IPasswordHasher hasher, IEmailSender email)
    {
        _db = db; _hasher = hasher; _email = email;
    }

    public async Task Handle(PortalForgotPinCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var acc = await conn.QueryFirstOrDefaultAsync<(string patient_id, string? email)>(
            @"SELECT pa.patient_id, COALESCE(pa.email, p.email) AS email
              FROM diab_his_pat_portal_accounts pa
              JOIN diab_his_pat_patients p ON p.id = pa.patient_id
              WHERE pa.tenant_id = @TenantId AND pa.phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        // Khong tiet lo SDT ton tai hay khong (chong do email) — im lang neu thieu account/email
        if (acc.patient_id == null || string.IsNullOrWhiteSpace(acc.email)) return;

        var otp = Random.Shared.Next(100000, 999999).ToString();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_otp_log (id, tenant_id, phone, otp_hash, purpose, sent_at, expires_at, attempts)
              VALUES (@Id, @TenantId, @Phone, @Hash, 'RESET_PIN', UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 10 MINUTE), 0)",
            new { Id = Guid.NewGuid().ToString(), cmd.TenantId, cmd.Phone, Hash = _hasher.Hash(otp) });

        await _email.SendAsync(acc.email!, "Pro-Diab — Ma xac nhan dat lai ma PIN",
            $"<p>Ma xac nhan dat lai ma PIN cua ban la: <b style=\"font-size:20px\">{otp}</b></p>" +
            "<p>Ma co hieu luc trong 10 phut. Neu ban khong yeu cau, hay bo qua email nay.</p>",
            cancellationToken);
    }
}

public record PortalResetPinCommand(string Phone, string Otp, string NewPin, int TenantId) : IRequest<PortalAuthResponse>;

public class PortalResetPinHandler : IRequestHandler<PortalResetPinCommand, PortalAuthResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    public PortalResetPinHandler(IDapperConnectionFactory db, IPasswordHasher hasher, IJwtService jwt)
    {
        _db = db; _hasher = hasher; _jwt = jwt;
    }

    public async Task<PortalAuthResponse> Handle(PortalResetPinCommand cmd, CancellationToken cancellationToken)
    {
        if (!PortalPinRules.IsValidPin(cmd.NewPin)) throw new PortalPinInvalidException();

        using var conn = _db.CreateConnection();
        var otpLog = await conn.QueryFirstOrDefaultAsync<(string id, string otp_hash, DateTime expires_at)>(
            @"SELECT id, otp_hash, expires_at FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone AND purpose = 'RESET_PIN' AND verified_at IS NULL
              ORDER BY sent_at DESC LIMIT 1",
            new { cmd.TenantId, cmd.Phone });

        if (otpLog.id == null || otpLog.expires_at < DateTime.UtcNow) throw new OtpExpiredException();
        if (!_hasher.Verify(cmd.Otp, otpLog.otp_hash)) throw new OtpInvalidException();

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_otp_log SET verified_at = UTC_TIMESTAMP() WHERE id = @Id",
            new { Id = otpLog.id });

        var acc = await conn.QueryFirstOrDefaultAsync<(string patient_id, string _)>(
            "SELECT patient_id, '' FROM diab_his_pat_portal_accounts WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });
        if (acc.patient_id == null) throw new PortalPhoneNotRegisteredException();

        await conn.ExecuteAsync(
            @"UPDATE diab_his_pat_portal_accounts
              SET pin_hash = @PinHash, failed_attempts = 0, locked_until = NULL, updated_at = UTC_TIMESTAMP()
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { PinHash = _hasher.Hash(cmd.NewPin), cmd.TenantId, cmd.Phone });

        return await PortalActivateHandler.IssueTokenAsync(conn, acc.patient_id, cmd.TenantId, _jwt);
    }
}

// ============================================================
// Tenant-info: phong kham hien tai (resolve tu Host qua middleware) — cho man login
// ============================================================
public record GetPortalTenantInfoQuery(int TenantId) : IRequest<PortalTenantInfoResponse>;

public class GetPortalTenantInfoHandler : IRequestHandler<GetPortalTenantInfoQuery, PortalTenantInfoResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IVapidKeyService _vapid;
    public GetPortalTenantInfoHandler(IDapperConnectionFactory db, IVapidKeyService vapid)
    {
        _db = db; _vapid = vapid;
    }

    public async Task<PortalTenantInfoResponse> Handle(GetPortalTenantInfoQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var t = await conn.QueryFirstOrDefaultAsync<(string name, string? logo_url)>(
            "SELECT name, logo_url FROM diab_his_sys_tenants WHERE id = @Id AND deleted_at IS NULL",
            new { Id = q.TenantId });

        var vapidPub = await _vapid.GetOrCreateKeyPairAsync(q.TenantId, cancellationToken);
        return new PortalTenantInfoResponse(q.TenantId, t.name ?? "Phòng khám", t.logo_url, vapidPub.PublicKey);
    }
}
