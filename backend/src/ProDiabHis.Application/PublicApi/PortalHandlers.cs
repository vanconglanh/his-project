using Dapper;
using MediatR;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// GHI CHU KIEN TRUC (2026-07-08):
//   Toan bo id he thong benh nhan/luot kham la CHAR(36) (chuoi UUID), KHONG phai
//   BINARY(16). Cac view "his_patient"/"his_encounter" khong ton tai -> query truc
//   tiep bang that: diab_his_pat_patients, diab_his_enc_encounters,
//   diab_his_enc_diagnoses, diab_his_sec_users, diab_his_pat_insurances.
//   Bo het UUID_TO_BIN/BIN_TO_UUID. So sanh id = @Id (chuoi).
// ============================================================

// ============================================================
// Portal: Request OTP (dung cho quen PIN — gui OTP qua email/SMS)
// ============================================================
public record PortalRequestOtpCommand(string Phone, int TenantId) : IRequest;

public class PortalRequestOtpHandler : IRequestHandler<PortalRequestOtpCommand>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ISmsGateway _sms;
    private readonly IPasswordHasher _hasher;

    public PortalRequestOtpHandler(IDapperConnectionFactory db, ISmsGateway sms, IPasswordHasher hasher)
    {
        _db = db;
        _sms = sms;
        _hasher = hasher;
    }

    public async Task Handle(PortalRequestOtpCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var account = await conn.QueryFirstOrDefaultAsync<(string patient_id, int failed_attempts, DateTime? locked_until)>(
            @"SELECT patient_id, failed_attempts, locked_until
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (account.patient_id == null)
            throw new PortalPhoneNotRegisteredException();

        if (account.locked_until.HasValue && account.locked_until.Value > DateTime.UtcNow)
            throw new OtpTooManyAttemptsException();

        var sentLastHour = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone
                AND sent_at >= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 HOUR) AND purpose = 'LOGIN'",
            new { cmd.TenantId, cmd.Phone });

        if (sentLastHour >= 5)
            throw new OtpTooManyAttemptsException();

        var otp = Random.Shared.Next(100000, 999999).ToString();
        var hash = _hasher.Hash(otp);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_otp_log
                (id, tenant_id, phone, otp_hash, purpose, sent_at, expires_at, attempts)
              VALUES (@Id, @TenantId, @Phone, @Hash, 'LOGIN', UTC_TIMESTAMP(),
                      DATE_ADD(UTC_TIMESTAMP(), INTERVAL 5 MINUTE), 0)",
            new { Id = Guid.NewGuid().ToString(), cmd.TenantId, cmd.Phone, Hash = hash });

        await conn.ExecuteAsync(
            @"UPDATE diab_his_pat_portal_accounts SET last_otp_sent_at = UTC_TIMESTAMP()
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        await _sms.SendAsync(cmd.Phone, $"[Pro-Diab] Ma OTP dang nhap: {otp}. Het han sau 5 phut.", cancellationToken);
    }
}

public class PortalPhoneNotRegisteredException : Exception
{
    public PortalPhoneNotRegisteredException() : base("PORTAL_PHONE_NOT_REGISTERED") { }
}

// ============================================================
// Portal: Verify OTP
// ============================================================
public record PortalVerifyOtpCommand(string Phone, string Otp, int TenantId) : IRequest<PortalAuthResponse>;

public class PortalVerifyOtpHandler : IRequestHandler<PortalVerifyOtpCommand, PortalAuthResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _hasher;

    public PortalVerifyOtpHandler(IDapperConnectionFactory db, IJwtService jwt, IPasswordHasher hasher)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<PortalAuthResponse> Handle(PortalVerifyOtpCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var account = await conn.QueryFirstOrDefaultAsync<(string patient_id, int failed_attempts, DateTime? locked_until)>(
            @"SELECT patient_id, failed_attempts, locked_until
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (account.patient_id == null) throw new PortalPhoneNotRegisteredException();
        if (account.locked_until.HasValue && account.locked_until.Value > DateTime.UtcNow)
            throw new OtpTooManyAttemptsException();

        var otpLog = await conn.QueryFirstOrDefaultAsync<(string id, string otp_hash, DateTime expires_at, int attempts)>(
            @"SELECT id, otp_hash, expires_at, attempts
              FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone AND purpose = 'LOGIN'
                AND verified_at IS NULL
              ORDER BY sent_at DESC LIMIT 1",
            new { cmd.TenantId, cmd.Phone });

        if (otpLog.id == null || otpLog.expires_at < DateTime.UtcNow)
            throw new OtpExpiredException();

        if (!_hasher.Verify(cmd.Otp, otpLog.otp_hash))
        {
            var newAttempts = otpLog.attempts + 1;
            await conn.ExecuteAsync(
                "UPDATE diab_his_pat_portal_otp_log SET attempts = @A WHERE id = @Id",
                new { A = newAttempts, Id = otpLog.id });

            if (newAttempts >= 5)
            {
                await conn.ExecuteAsync(
                    @"UPDATE diab_his_pat_portal_accounts
                      SET failed_attempts = @A, locked_until = DATE_ADD(UTC_TIMESTAMP(), INTERVAL 15 MINUTE)
                      WHERE tenant_id = @TenantId AND phone = @Phone",
                    new { A = newAttempts, cmd.TenantId, cmd.Phone });
                throw new OtpTooManyAttemptsException();
            }
            throw new OtpInvalidException();
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_otp_log SET verified_at = UTC_TIMESTAMP() WHERE id = @Id",
            new { Id = otpLog.id });

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_accounts SET failed_attempts = 0, locked_until = NULL WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        var patient = await conn.QueryFirstAsync<(string patient_code, string full_name)>(
            "SELECT code AS patient_code, full_name FROM diab_his_pat_patients WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = account.patient_id, cmd.TenantId });

        var token = _jwt.GeneratePortalToken(Guid.Parse(account.patient_id), patient.patient_code, cmd.TenantId, out var jti);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_sessions
                (id, tenant_id, patient_id, jti, issued_at, expires_at)
              VALUES (@Id, @TenantId, @PatientId, @Jti,
                      UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 24 HOUR))",
            new { Id = Guid.NewGuid().ToString(), cmd.TenantId, PatientId = account.patient_id, Jti = jti });

        return new PortalAuthResponse(token, "Bearer", 86400, patient.patient_code, patient.full_name);
    }
}

// ============================================================
// Portal: Logout
// ============================================================
public record PortalLogoutCommand(string Jti, DateTime ExpiresAt) : IRequest;

public class PortalLogoutHandler : IRequestHandler<PortalLogoutCommand>
{
    private readonly IPortalAuthService _portalAuth;
    public PortalLogoutHandler(IPortalAuthService portalAuth) => _portalAuth = portalAuth;

    public async Task Handle(PortalLogoutCommand cmd, CancellationToken cancellationToken)
        => await _portalAuth.LogoutAsync(cmd.Jti, cmd.ExpiresAt, cancellationToken);
}

// ============================================================
// Portal: Get Me
// ============================================================
public record GetPortalMeQuery(Guid PatientId, int TenantId) : IRequest<PortalMeResponse>;

public class GetPortalMeHandler : IRequestHandler<GetPortalMeQuery, PortalMeResponse>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalMeHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PortalMeResponse> Handle(GetPortalMeQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var p = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT p.code AS patient_code, p.full_name, p.gender,
                     p.date_of_birth AS dob, p.phone, p.street AS address,
                     (SELECT i.card_no_masked FROM diab_his_pat_insurances i
                       WHERE i.patient_id = p.id AND i.type = 'BHYT' AND i.deleted_at IS NULL
                       ORDER BY i.valid_to DESC LIMIT 1) AS bhyt_number
              FROM diab_his_pat_patients p
              WHERE p.id = @Id AND p.tenant_id = @TenantId AND p.deleted_at IS NULL",
            new { Id = q.PatientId.ToString(), q.TenantId });

        if (p == null) throw new PatientNotFoundException();

        return new PortalMeResponse(
            (string)p.patient_code, (string)p.full_name, (string?)p.gender ?? "",
            p.dob != null ? DateOnly.FromDateTime((DateTime)p.dob) : default,
            (string?)p.phone ?? "", (string?)p.address, (string?)p.bhyt_number);
    }
}

// ============================================================
// Portal: Encounters (danh sach)
// ============================================================
public record GetPortalEncountersQuery(Guid PatientId, int TenantId, int Page, int PageSize)
    : IRequest<(List<PortalEncounterResponse> Items, int Total)>;

public class GetPortalEncountersHandler : IRequestHandler<GetPortalEncountersQuery, (List<PortalEncounterResponse>, int)>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalEncountersHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<(List<PortalEncounterResponse>, int)> Handle(GetPortalEncountersQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        int offset = (q.Page - 1) * q.PageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_enc_encounters WHERE patient_id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = q.PatientId.ToString(), q.TenantId });

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT e.id AS id, e.encounter_no AS encounter_code, e.started_at AS visited_at,
                     u.full_name AS doctor_name, e.chief_complaint, e.status
              FROM diab_his_enc_encounters e
              LEFT JOIN diab_his_sec_users u ON u.id = e.doctor_id
              WHERE e.patient_id = @Id AND e.tenant_id = @TenantId AND e.deleted_at IS NULL
              ORDER BY e.started_at DESC, e.created_at DESC
              LIMIT @PageSize OFFSET @Offset",
            new { Id = q.PatientId.ToString(), q.TenantId, q.PageSize, Offset = offset });

        var items = new List<PortalEncounterResponse>();
        foreach (var r in rows)
        {
            var diagRows = await conn.QueryAsync<(string icd10, string name)>(
                @"SELECT icd10_code AS icd10, name
                  FROM diab_his_enc_diagnoses WHERE encounter_id = @Id AND deleted_at IS NULL
                  ORDER BY (type = 'PRIMARY') DESC",
                new { Id = (string)r.id });

            items.Add(new PortalEncounterResponse(
                Guid.Parse((string)r.id),
                (string?)r.encounter_code ?? "",
                r.visited_at != null ? (DateTime)r.visited_at : default,
                (string?)r.doctor_name ?? "",
                (string?)r.chief_complaint ?? "",
                diagRows.Select(d => new DiagnosisItem(d.icd10, d.name)).ToList(),
                (string?)r.status ?? ""));
        }

        return (items, total);
    }
}

// ============================================================
// Portal: Appointments
// ============================================================
public record GetPortalAppointmentsQuery(Guid PatientId, int TenantId) : IRequest<List<PublicAppointmentResponse>>;

public class GetPortalAppointmentsHandler : IRequestHandler<GetPortalAppointmentsQuery, List<PublicAppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalAppointmentsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PublicAppointmentResponse>> Handle(GetPortalAppointmentsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT a.uuid AS id, a.appointment_code, a.status, a.appointment_at,
                     u.full_name AS doctor_name, a.partner_reference
              FROM diab_his_sch_appointments a
              LEFT JOIN diab_his_sec_users u ON u.id = a.doctor_ref
              WHERE a.patient_ref = @PatientId AND a.tenant_id = @TenantId AND a.deleted_at IS NULL
              ORDER BY a.appointment_at DESC",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        return rows.Select(r => new PublicAppointmentResponse(
            r.id != null ? Guid.Parse((string)r.id) : Guid.Empty,
            (string?)r.appointment_code ?? "",
            (string?)r.status ?? "",
            (DateTime)r.appointment_at, (string?)r.doctor_name,
            null,
            (string?)r.partner_reference)).ToList();
    }
}

public record CreatePortalAppointmentCommand(Guid PatientId, int TenantId, PortalAppointmentCreateRequest Request) : IRequest<PublicAppointmentResponse>;

public class CreatePortalAppointmentHandler : IRequestHandler<CreatePortalAppointmentCommand, PublicAppointmentResponse>
{
    private readonly IDapperConnectionFactory _db;
    public CreatePortalAppointmentHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PublicAppointmentResponse> Handle(CreatePortalAppointmentCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        // Kiem tra trung slot bac si (+-30 phut), bo qua lich da huy
        if (cmd.Request.DoctorId.HasValue)
        {
            var conflict = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM diab_his_sch_appointments
                  WHERE tenant_id = @TenantId AND doctor_ref = @DoctorRef
                    AND ABS(TIMESTAMPDIFF(MINUTE, appointment_at, @AppointmentAt)) < 30
                    AND status <> 'CANCELLED' AND deleted_at IS NULL",
                new { cmd.TenantId, DoctorRef = cmd.Request.DoctorId.ToString(), cmd.Request.AppointmentAt });
            if (conflict > 0) throw new SlotTakenException();
        }

        var apptUuid = Guid.NewGuid();
        var code = $"LH{DateTime.UtcNow:yyyyMMdd}{apptUuid.ToString("N")[..6].ToUpper()}";

        // id INT AUTO_INCREMENT — chi set uuid/patient_ref/doctor_ref (CHAR36), status PENDING (khop ENUM 0016)
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sch_appointments
                (tenant_id, uuid, appointment_code, patient_ref, doctor_ref,
                 appointment_at, note, status, source, created_at, updated_at)
              VALUES (@TenantId, @Uuid, @Code, @PatientRef, @DoctorRef,
                      @AppointmentAt, @Note, 'PENDING', 'APP', UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                cmd.TenantId,
                Uuid = apptUuid.ToString(),
                Code = code,
                PatientRef = cmd.PatientId.ToString(),
                DoctorRef = cmd.Request.DoctorId?.ToString(),
                cmd.Request.AppointmentAt,
                Note = cmd.Request.Note
            });

        return new PublicAppointmentResponse(apptUuid, code, "PENDING", cmd.Request.AppointmentAt, null, null, null);
    }
}

public record CancelPortalAppointmentCommand(Guid AppointmentId, Guid PatientId, int TenantId) : IRequest;

public class CancelPortalAppointmentHandler : IRequestHandler<CancelPortalAppointmentCommand>
{
    private readonly IDapperConnectionFactory _db;
    public CancelPortalAppointmentHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(CancelPortalAppointmentCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var appt = await conn.QueryFirstOrDefaultAsync<(string status, DateTime appointment_at)>(
            @"SELECT status, appointment_at FROM diab_his_sch_appointments
              WHERE uuid = @Id AND patient_ref = @PatientId AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = cmd.AppointmentId.ToString(), PatientId = cmd.PatientId.ToString(), cmd.TenantId });

        if (appt.status == null) throw new AppointmentNotFoundException();

        if ((appt.appointment_at - DateTime.UtcNow).TotalHours < 2)
            throw new AppointmentCancelTooLateException();

        await conn.ExecuteAsync(
            "UPDATE diab_his_sch_appointments SET status = 'CANCELLED', updated_at = UTC_TIMESTAMP() WHERE uuid = @Id AND tenant_id = @TenantId",
            new { Id = cmd.AppointmentId.ToString(), cmd.TenantId });
    }
}

public class AppointmentNotFoundException : Exception { public AppointmentNotFoundException() : base("APPOINTMENT_NOT_FOUND") { } }
public class AppointmentCancelTooLateException : Exception { public AppointmentCancelTooLateException() : base("APPOINTMENT_CANCEL_TOO_LATE") { } }
