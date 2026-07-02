using Dapper;
using MediatR;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Public Patient: Register
// ============================================================
public record RegisterPatientCommand(
    PublicRegisterPatientRequest Request,
    Guid PartnerId,
    int TenantId
) : IRequest<PublicPatientResponse>;

public class RegisterPatientHandler : IRequestHandler<RegisterPatientCommand, PublicPatientResponse>
{
    private readonly IDapperConnectionFactory _db;

    public RegisterPatientHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PublicPatientResponse> Handle(RegisterPatientCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        // Check existing by phone + tenant
        var existing = await conn.QueryFirstOrDefaultAsync<(string patient_code, string full_name, DateTime created_at)>(
            @"SELECT p.patient_code, p.full_name, p.created_at
              FROM his_patient p
              WHERE p.tenant_id = @TenantId AND p.phone = @Phone AND p.deleted_at IS NULL
              LIMIT 1",
            new { cmd.TenantId, Phone = cmd.Request.Phone });

        if (existing != default)
            return new PublicPatientResponse(existing.patient_code, existing.full_name, existing.created_at);

        var id = Guid.NewGuid();
        var patientCode = $"BN{DateTime.UtcNow:yyyyMMdd}{id.ToString("N")[..6].ToUpper()}";

        await conn.ExecuteAsync(
            @"INSERT INTO his_patient (id, tenant_id, patient_code, full_name, gender, dob, phone, address, email, created_at, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @PatientCode, @FullName, @Gender, @Dob, @Phone, @Address, @Email, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Id = id.ToString(),
                cmd.TenantId,
                PatientCode = patientCode,
                cmd.Request.FullName,
                cmd.Request.Gender,
                Dob = cmd.Request.Dob.ToString("yyyy-MM-dd"),
                Phone = cmd.Request.Phone,
                Address = cmd.Request.Address,
                Email = cmd.Request.Email
            });

        return new PublicPatientResponse(patientCode, cmd.Request.FullName, DateTime.UtcNow);
    }
}

// ============================================================
// Public Appointment: Book
// ============================================================
public record BookAppointmentCommand(
    PublicAppointmentBookRequest Request,
    Guid PartnerId,
    int TenantId
) : IRequest<PublicAppointmentResponse>;

public class BookAppointmentHandler : IRequestHandler<BookAppointmentCommand, PublicAppointmentResponse>
{
    private readonly IDapperConnectionFactory _db;

    public BookAppointmentHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PublicAppointmentResponse> Handle(BookAppointmentCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        // Check slot conflict (same doctor + same 30-min window)
        int conflict = 0;
        if (cmd.Request.DoctorId.HasValue)
        {
            conflict = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM diab_his_sch_appointments
                  WHERE tenant_id = @TenantId AND doctor_id = UUID_TO_BIN(@DoctorId)
                    AND ABS(TIMESTAMPDIFF(MINUTE, appointment_at, @AppointmentAt)) < 30
                    AND status NOT IN ('CANCELLED')",
                new { cmd.TenantId, DoctorId = cmd.Request.DoctorId.ToString(), cmd.Request.AppointmentAt });
        }

        if (conflict > 0)
            throw new SlotTakenException();

        // Auto-create patient if not exists
        var patientRow = await conn.QueryFirstOrDefaultAsync<(byte[] id, string patient_code)>(
            @"SELECT id, patient_code FROM his_patient
              WHERE tenant_id = @TenantId AND phone = @Phone AND deleted_at IS NULL LIMIT 1",
            new { cmd.TenantId, Phone = cmd.Request.PatientPhone });

        Guid patientId;
        if (patientRow == default)
        {
            patientId = Guid.NewGuid();
            var pc = $"BN{DateTime.UtcNow:yyyyMMdd}{patientId.ToString("N")[..6].ToUpper()}";
            await conn.ExecuteAsync(
                @"INSERT INTO his_patient (id, tenant_id, patient_code, full_name, phone, gender, created_at, updated_at)
                  VALUES (UUID_TO_BIN(@Id), @TenantId, @Pc, @FullName, @Phone, 'O', UTC_TIMESTAMP(), UTC_TIMESTAMP())",
                new { Id = patientId.ToString(), cmd.TenantId, Pc = pc, cmd.Request.PatientName, Phone = cmd.Request.PatientPhone });
        }
        else
        {
            patientId = new Guid(patientRow.id);
        }

        var apptId = Guid.NewGuid();
        var apptCode = $"LH{DateTime.UtcNow:yyyyMMdd}{apptId.ToString("N")[..6].ToUpper()}";

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_sch_appointments
                (id, tenant_id, appointment_code, patient_id, doctor_id, department_id, service_id,
                 appointment_at, note, status, source_partner_id, partner_reference, created_at, updated_at)
              VALUES
                (UUID_TO_BIN(@Id), @TenantId, @Code, UUID_TO_BIN(@PatientId),
                 @DoctorId, @DeptId, @ServiceId,
                 @AppointmentAt, @Note, 'BOOKED',
                 UUID_TO_BIN(@PartnerId), @PartnerRef,
                 UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Id = apptId.ToString(),
                cmd.TenantId,
                Code = apptCode,
                PatientId = patientId.ToString(),
                DoctorId = cmd.Request.DoctorId?.ToString(),
                DeptId = cmd.Request.DepartmentId?.ToString(),
                ServiceId = cmd.Request.ServiceId?.ToString(),
                cmd.Request.AppointmentAt,
                Note = cmd.Request.Note,
                PartnerId = cmd.PartnerId.ToString(),
                PartnerRef = cmd.Request.PartnerReference
            });

        // Fetch doctor name
        string? doctorName = null;
        if (cmd.Request.DoctorId.HasValue)
        {
            doctorName = await conn.ExecuteScalarAsync<string>(
                "SELECT full_name FROM sec_users WHERE id = UUID_TO_BIN(@Id) LIMIT 1",
                new { Id = cmd.Request.DoctorId.ToString() });
        }

        return new PublicAppointmentResponse(
            apptId, apptCode, "BOOKED",
            cmd.Request.AppointmentAt, doctorName,
            cmd.PartnerId, cmd.Request.PartnerReference);
    }
}

public class SlotTakenException : Exception
{
    public SlotTakenException() : base("APPOINTMENT_SLOT_TAKEN") { }
}

// ============================================================
// Public Appointment: Get by ID
// ============================================================
public record GetPublicAppointmentQuery(Guid Id, int TenantId) : IRequest<PublicAppointmentResponse?>;

public class GetPublicAppointmentHandler : IRequestHandler<GetPublicAppointmentQuery, PublicAppointmentResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public GetPublicAppointmentHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PublicAppointmentResponse?> Handle(GetPublicAppointmentQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT BIN_TO_UUID(a.id) AS id, a.appointment_code, a.status, a.appointment_at,
                     u.full_name AS doctor_name,
                     BIN_TO_UUID(a.source_partner_id) AS source_partner_id,
                     a.partner_reference
              FROM diab_his_sch_appointments a
              LEFT JOIN sec_users u ON u.id = a.doctor_id
              WHERE a.id = UUID_TO_BIN(@Id) AND a.tenant_id = @TenantId",
            new { Id = q.Id.ToString(), q.TenantId });

        if (row == null) return null;

        return new PublicAppointmentResponse(
            Guid.Parse((string)row.id),
            (string)row.appointment_code,
            (string)row.status,
            (DateTime)row.appointment_at,
            (string?)row.doctor_name,
            row.source_partner_id != null ? Guid.Parse((string)row.source_partner_id) : null,
            (string?)row.partner_reference
        );
    }
}

// ============================================================
// Visit Lookup: Request OTP
// ============================================================
public record RequestVisitOtpCommand(string PatientCode, int TenantId, Guid PartnerId) : IRequest;

public class RequestVisitOtpHandler : IRequestHandler<RequestVisitOtpCommand>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ISmsGateway _sms;
    private readonly IPasswordHasher _hasher;

    public RequestVisitOtpHandler(IDapperConnectionFactory db, ISmsGateway sms, IPasswordHasher hasher)
    {
        _db = db;
        _sms = sms;
        _hasher = hasher;
    }

    public async Task Handle(RequestVisitOtpCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var patient = await conn.QueryFirstOrDefaultAsync<(string phone, int dummy)>(
            @"SELECT phone FROM his_patient WHERE patient_code = @Code AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Code = cmd.PatientCode, cmd.TenantId });

        if (patient == default)
            throw new PatientNotFoundException();

        var otp = GenerateOtp();
        var hash = _hasher.Hash(otp);
        var logId = Guid.NewGuid();

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_portal_otp_log
                (id, tenant_id, phone, otp_hash, purpose, sent_at, expires_at, attempts)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @Phone, @Hash, 'LOOKUP', UTC_TIMESTAMP(),
                      DATE_ADD(UTC_TIMESTAMP(), INTERVAL 5 MINUTE), 0)",
            new { Id = logId.ToString(), cmd.TenantId, Phone = patient.Item1, Hash = hash });

        await _sms.SendAsync(patient.Item1, $"[Pro-Diab] Ma OTP tra cuu lich kham: {otp}. Het han sau 5 phut.", cancellationToken);
    }

    private static string GenerateOtp()
        => Random.Shared.Next(100000, 999999).ToString();
}

public class PatientNotFoundException : Exception
{
    public PatientNotFoundException() : base("PATIENT_NOT_FOUND") { }
}

// ============================================================
// Visit Lookup: Verify OTP
// ============================================================
public record VerifyVisitOtpCommand(string PatientCode, string Otp, int TenantId) : IRequest<LookupTokenResponse>;

public class VerifyVisitOtpHandler : IRequestHandler<VerifyVisitOtpCommand, LookupTokenResponse>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ProDiabHis.Application.Auth.IJwtService _jwt;
    private readonly IPasswordHasher _hasher;

    public VerifyVisitOtpHandler(IDapperConnectionFactory db, Auth.IJwtService jwt, IPasswordHasher hasher)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<LookupTokenResponse> Handle(VerifyVisitOtpCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var patient = await conn.QueryFirstOrDefaultAsync<(string patient_code, string phone)>(
            @"SELECT patient_code, phone FROM his_patient
              WHERE patient_code = @Code AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Code = cmd.PatientCode, cmd.TenantId });

        if (patient == default) throw new PatientNotFoundException();

        var otpLog = await conn.QueryFirstOrDefaultAsync<(Guid id, string otp_hash, DateTime expires_at, int attempts)>(
            @"SELECT BIN_TO_UUID(id) AS id, otp_hash, expires_at, attempts
              FROM diab_his_pat_portal_otp_log
              WHERE tenant_id = @TenantId AND phone = @Phone AND purpose = 'LOOKUP'
                AND verified_at IS NULL
              ORDER BY sent_at DESC LIMIT 1",
            new { cmd.TenantId, Phone = patient.phone });

        if (otpLog == default || otpLog.expires_at < DateTime.UtcNow)
            throw new OtpExpiredException();

        if (!_hasher.Verify(cmd.Otp, otpLog.otp_hash))
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_pat_portal_otp_log SET attempts = attempts + 1 WHERE id = UUID_TO_BIN(@Id)",
                new { Id = otpLog.id.ToString() });

            if (otpLog.attempts + 1 >= 5)
                throw new OtpTooManyAttemptsException();
            throw new OtpInvalidException();
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pat_portal_otp_log SET verified_at = UTC_TIMESTAMP() WHERE id = UUID_TO_BIN(@Id)",
            new { Id = otpLog.id.ToString() });

        var token = _jwt.GenerateLookupToken(patient.patient_code, cmd.TenantId, 600);
        return new LookupTokenResponse(token, 600);
    }
}

public class OtpInvalidException : Exception { public OtpInvalidException() : base("PORTAL_OTP_INVALID") { } }
public class OtpExpiredException : Exception { public OtpExpiredException() : base("PORTAL_OTP_EXPIRED") { } }
public class OtpTooManyAttemptsException : Exception { public OtpTooManyAttemptsException() : base("PORTAL_OTP_TOO_MANY_ATTEMPTS") { } }
