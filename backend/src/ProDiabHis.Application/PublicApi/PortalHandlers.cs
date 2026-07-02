using Dapper;
using MediatR;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Portal: Request OTP
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

        var account = await conn.QueryFirstOrDefaultAsync<(Guid patient_id, int failed_attempts, DateTime? locked_until, DateTime? last_otp_sent_at)>(
            @"SELECT BIN_TO_UUID(patient_id) AS patient_id, failed_attempts, locked_until, last_otp_sent_at
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (account == default)
            throw new PortalPhoneNotRegisteredException();

        // Check lock
        if (account.locked_until.HasValue && account.locked_until.Value > DateTime.UtcNow)
            throw new OtpTooManyAttemptsException();

        // Throttle: max 5 req/hour
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
              VALUES (UUID_TO_BIN(@Id), @TenantId, @Phone, @Hash, 'LOGIN', UTC_TIMESTAMP(),
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
            @"SELECT BIN_TO_UUID(patient_id) AS patient_id, failed_attempts, locked_until
              FROM diab_his_pat_portal_accounts
              WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        if (account == default) throw new PortalPhoneNotRegisteredException();
        if (account.locked_until.HasValue && account.locked_until.Value > DateTime.UtcNow)
            throw new OtpTooManyAttemptsException();

        var otpLog = await conn.QueryFirstOrDefaultAsync<(string id, string otp_hash, DateTime expires_at, int attempts)>(
            @"SELECT BIN_TO_UUID(id) AS id, otp_hash, expires_at, attempts
              FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone AND purpose = 'LOGIN'
                AND verified_at IS NULL
              ORDER BY sent_at DESC LIMIT 1",
            new { cmd.TenantId, cmd.Phone });

        if (otpLog == default || otpLog.expires_at < DateTime.UtcNow)
            throw new OtpExpiredException();

        if (!_hasher.Verify(cmd.Otp, otpLog.otp_hash))
        {
            var newAttempts = otpLog.attempts + 1;
            await conn.ExecuteAsync(
                "UPDATE diab_his_pat_portal_otp_log SET attempts = @A WHERE id = UUID_TO_BIN(@Id)",
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

        // Mark verified
        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_otp_log SET verified_at = UTC_TIMESTAMP() WHERE id = UUID_TO_BIN(@Id)",
            new { Id = otpLog.id });

        // Reset failed attempts
        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_accounts SET failed_attempts = 0, locked_until = NULL WHERE tenant_id = @TenantId AND phone = @Phone",
            new { cmd.TenantId, cmd.Phone });

        // Get patient info
        var patient = await conn.QueryFirstAsync<(string patient_code, string full_name)>(
            "SELECT patient_code, full_name FROM his_patient WHERE id = UUID_TO_BIN(@Id)",
            new { Id = account.patient_id });

        var token = _jwt.GeneratePortalToken(Guid.Parse(account.patient_id), patient.patient_code, cmd.TenantId, out var jti);

        // Save session
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_sessions
                (id, tenant_id, patient_id, jti, issued_at, expires_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@PatientId), @Jti,
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
            @"SELECT p.patient_code, p.full_name, p.gender,
                     p.dob, p.phone, p.address,
                     i.insurance_number AS bhyt_number
              FROM his_patient p
              LEFT JOIN his_insurance i ON i.patient_id = p.id AND i.is_primary = 1
              WHERE p.id = UUID_TO_BIN(@Id) AND p.tenant_id = @TenantId AND p.deleted_at IS NULL",
            new { Id = q.PatientId.ToString(), q.TenantId });

        if (p == null) throw new PatientNotFoundException();

        return new PortalMeResponse(
            (string)p.patient_code, (string)p.full_name, (string)p.gender,
            DateOnly.FromDateTime((DateTime)p.dob),
            (string)p.phone, (string?)p.address, (string?)p.bhyt_number);
    }
}

// ============================================================
// Portal: Encounters
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
            "SELECT COUNT(*) FROM his_encounter WHERE patient_id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = q.PatientId.ToString(), q.TenantId });

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT BIN_TO_UUID(e.id) AS id, e.encounter_code, e.started_at AS visited_at,
                     u.full_name AS doctor_name, e.chief_complaint, e.status
              FROM his_encounter e
              LEFT JOIN sec_users u ON u.id = e.doctor_id
              WHERE e.patient_id = UUID_TO_BIN(@Id) AND e.tenant_id = @TenantId AND e.deleted_at IS NULL
              ORDER BY e.started_at DESC
              LIMIT @PageSize OFFSET @Offset",
            new { Id = q.PatientId.ToString(), q.TenantId, q.PageSize, Offset = offset });

        var items = new List<PortalEncounterResponse>();
        foreach (var r in rows)
        {
            var diagRows = await conn.QueryAsync<(string icd10, string name)>(
                @"SELECT icd10_code AS icd10, icd10_name AS name
                  FROM his_encounter_diagnosis WHERE encounter_id = UUID_TO_BIN(@Id)",
                new { Id = (string)r.id });

            items.Add(new PortalEncounterResponse(
                Guid.Parse((string)r.id),
                (string)r.encounter_code,
                (DateTime)r.visited_at,
                (string?)r.doctor_name ?? "",
                (string?)r.chief_complaint ?? "",
                diagRows.Select(d => new DiagnosisItem(d.icd10, d.name)).ToList(),
                (string)r.status));
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
            @"SELECT BIN_TO_UUID(a.id) AS id, a.appointment_code, a.status, a.appointment_at,
                     u.full_name AS doctor_name,
                     BIN_TO_UUID(a.source_partner_id) AS source_partner_id,
                     a.partner_reference
              FROM diab_his_sch_appointments a
              LEFT JOIN sec_users u ON u.id = a.doctor_id
              WHERE a.patient_id = UUID_TO_BIN(@PatientId) AND a.tenant_id = @TenantId
              ORDER BY a.appointment_at DESC",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        return rows.Select(r => new PublicAppointmentResponse(
            Guid.Parse((string)r.id), (string)r.appointment_code, (string)r.status,
            (DateTime)r.appointment_at, (string?)r.doctor_name,
            r.source_partner_id != null ? Guid.Parse((string)r.source_partner_id) : null,
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

        // Slot conflict check
        if (cmd.Request.DoctorId.HasValue)
        {
            var conflict = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM diab_his_sch_appointments
                  WHERE tenant_id = @TenantId AND doctor_id = UUID_TO_BIN(@DoctorId)
                    AND ABS(TIMESTAMPDIFF(MINUTE, appointment_at, @AppointmentAt)) < 30
                    AND status != 'CANCELLED'",
                new { cmd.TenantId, DoctorId = cmd.Request.DoctorId.ToString(), cmd.Request.AppointmentAt });
            if (conflict > 0) throw new SlotTakenException();
        }

        var apptId = Guid.NewGuid();
        var code = $"LH{DateTime.UtcNow:yyyyMMdd}{apptId.ToString("N")[..6].ToUpper()}";

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sch_appointments
                (id, tenant_id, appointment_code, patient_id, doctor_id, department_id,
                 appointment_at, note, status, created_at, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @Code, UUID_TO_BIN(@PatientId),
                      @DoctorId, @DeptId, @AppointmentAt, @Note, 'BOOKED', UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Id = apptId.ToString(), cmd.TenantId, Code = code,
                PatientId = cmd.PatientId.ToString(),
                DoctorId = cmd.Request.DoctorId?.ToString(),
                DeptId = cmd.Request.DepartmentId?.ToString(),
                cmd.Request.AppointmentAt,
                Note = cmd.Request.Note
            });

        return new PublicAppointmentResponse(apptId, code, "BOOKED", cmd.Request.AppointmentAt, null, null, null);
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
              WHERE id = UUID_TO_BIN(@Id) AND patient_id = UUID_TO_BIN(@PatientId) AND tenant_id = @TenantId",
            new { Id = cmd.AppointmentId.ToString(), PatientId = cmd.PatientId.ToString(), cmd.TenantId });

        if (appt == default) throw new AppointmentNotFoundException();

        if ((appt.appointment_at - DateTime.UtcNow).TotalHours < 2)
            throw new AppointmentCancelTooLateException();

        await conn.ExecuteAsync(
            "UPDATE diab_his_sch_appointments SET status = 'CANCELLED', updated_at = UTC_TIMESTAMP() WHERE id = UUID_TO_BIN(@Id)",
            new { Id = cmd.AppointmentId.ToString() });
    }
}

public class AppointmentNotFoundException : Exception { public AppointmentNotFoundException() : base("APPOINTMENT_NOT_FOUND") { } }
public class AppointmentCancelTooLateException : Exception { public AppointmentCancelTooLateException() : base("APPOINTMENT_CANCEL_TOO_LATE") { } }
