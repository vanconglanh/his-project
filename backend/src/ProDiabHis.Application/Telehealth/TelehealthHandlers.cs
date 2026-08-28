using System.Security.Cryptography;
using System.Text;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Telehealth.Integration;

namespace ProDiabHis.Application.Telehealth;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════
public record CheckTelehealthEligibilityQuery(Guid PatientId)
    : IRequest<Result<TelehealthEligibilityResponse>>;

public record LinkDocosanAccountCommand(Guid PatientId, LinkDocosanAccountRequest Request)
    : IRequest<Result<LinkDocosanAccountResponse>>;

public record CreateTelehealthAppointmentCommand(Guid PatientId, CreateTelehealthAppointmentRequest Request)
    : IRequest<Result<TelehealthSessionResponse>>;

public record GetTelehealthSessionQuery(Guid PatientId, Guid SessionId)
    : IRequest<Result<TelehealthSessionResponse>>;

public record GetTelehealthJoinLinkQuery(Guid PatientId, Guid SessionId)
    : IRequest<Result<TelehealthJoinLinkResponse>>;

// ═══════════════════════════════════════════════
// FR-801: kiem tra dieu kien dat lich tu van tu xa
// ═══════════════════════════════════════════════
public class CheckTelehealthEligibilityQueryHandler
    : IRequestHandler<CheckTelehealthEligibilityQuery, Result<TelehealthEligibilityResponse>>
{
    public const string SettingKeyRequiredDays = "telehealth.direct_visit_required_days";
    public const int DefaultRequiredDays = 180;

    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ISettingsProvider _settings;

    public CheckTelehealthEligibilityQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, ISettingsProvider settings)
    { _db = db; _tenant = tenant; _settings = settings; }

    public async Task<Result<TelehealthEligibilityResponse>> Handle(CheckTelehealthEligibilityQuery q, CancellationToken ct)
        => Result<TelehealthEligibilityResponse>.Success(
            await CheckAsync(_db, _tenant.TenantId, q.PatientId, _settings, ct));

    /// <summary>Logic dung chung — goi lai o buoc tao session (khong tin ket qua tu client).</summary>
    public static async Task<TelehealthEligibilityResponse> CheckAsync(
        IDapperConnectionFactory dbFactory, int tenantId, Guid patientId, ISettingsProvider settings, CancellationToken ct)
    {
        using var conn = dbFactory.CreateConnection();

        var requiredDays = await settings.GetIntAsync(SettingKeyRequiredDays, DefaultRequiredDays, ct);

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = patientId.ToString(), TId = tenantId });

        if (patient is null)
            return new TelehealthEligibilityResponse(false, "PATIENT_NOT_FOUND", null, null, requiredDays);

        var lastVisit = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, started_at, finished_at, created_at
            FROM diab_his_enc_encounters
            WHERE tenant_id=@TId AND patient_id=@PId AND deleted_at IS NULL
              AND status='DONE' AND telehealth_session_id IS NULL
            ORDER BY COALESCE(finished_at, started_at, created_at) DESC
            LIMIT 1",
            new { TId = tenantId, PId = patientId.ToString() });

        if (lastVisit is null)
            return new TelehealthEligibilityResponse(false, "TELEHEALTH_NOT_ELIGIBLE", null, null, requiredDays);

        DateTime lastDate = (DateTime?)lastVisit.finished_at ?? (DateTime?)lastVisit.started_at ?? (DateTime)lastVisit.created_at;
        var daysSince = (DateTime.UtcNow - lastDate).TotalDays;

        if (daysSince > requiredDays)
            return new TelehealthEligibilityResponse(
                false, "TELEHEALTH_NOT_ELIGIBLE", Guid.Parse((string)lastVisit.id), lastDate, requiredDays);

        return new TelehealthEligibilityResponse(true, null, Guid.Parse((string)lastVisit.id), lastDate, requiredDays);
    }
}

// ═══════════════════════════════════════════════
// Link / dang ky tai khoan Docosan cho benh nhan
// ═══════════════════════════════════════════════
public class LinkDocosanAccountCommandHandler
    : IRequestHandler<LinkDocosanAccountCommand, Result<LinkDocosanAccountResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IEncryptionService _enc;
    private readonly IDocosanClient _client;
    private readonly IAuditService _audit;
    private readonly ILogger<LinkDocosanAccountCommandHandler> _logger;

    public LinkDocosanAccountCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        IEncryptionService enc, IDocosanClient client, IAuditService audit,
        ILogger<LinkDocosanAccountCommandHandler> logger)
    { _db = db; _tenant = tenant; _enc = enc; _client = client; _audit = audit; _logger = logger; }

    public async Task<Result<LinkDocosanAccountResponse>> Handle(LinkDocosanAccountCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, full_name, phone, gender FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.PatientId.ToString(), TId = _tenant.TenantId });
        if (patient is null)
            return Result<LinkDocosanAccountResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        string? phone = (string?)patient.phone;
        if (string.IsNullOrWhiteSpace(phone))
            return Result<LinkDocosanAccountResponse>.Failure("TELEHEALTH_PATIENT_PHONE_MISSING", "Bệnh nhân chưa có số điện thoại để liên kết tài khoản Docosan");

        // Goi register-internal: idempotent phia Docosan theo SDT -> vua dang ky moi (neu chua co)
        // vua lay lai access_token (neu da co) — nen KHONG can goi IsUserExistAsync truoc.
        var regResult = await _client.RegisterInternalUserAsync(new DocosanRegisterUserRequest(
            Email: cmd.Request.Email,
            PhoneNumber: phone,
            DisplayName: cmd.Request.DisplayName ?? (string)patient.full_name,
            Gender: cmd.Request.Gender ?? (string?)patient.gender,
            Language: "vi"), ct);

        if (!regResult.Success || string.IsNullOrWhiteSpace(regResult.AccessToken))
        {
            _logger.LogWarning("Docosan register-internal that bai cho patient {PatientId}: {Code}", cmd.PatientId, regResult.ErrorCode);
            return Result<LinkDocosanAccountResponse>.Failure(
                "TELEHEALTH_PROVIDER_UNAVAILABLE", "Không kết nối được hệ thống Docosan, vui lòng thử lại");
        }

        var phoneHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(phone))).ToLowerInvariant();
        var accessTokenEnc = _enc.Encrypt(regResult.AccessToken);
        // Docosan hien tai (theo mobile app) khong tra ve thoi han cu the -> mac dinh coi TTL 24h,
        // job sync/link se refresh lai qua register-internal khi het han hoac gap 401.
        var tokenExpiresAt = DateTime.UtcNow.AddHours(24);
        var now = DateTime.UtcNow;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_int_docosan_patient_mapping WHERE tenant_id=@TId AND patient_id=@PId AND environment=@Env",
            new { TId = _tenant.TenantId, PId = cmd.PatientId.ToString(), Env = DocosanEnvironment.Current });

        if (existing is null)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_int_docosan_patient_mapping
                    (id, tenant_id, patient_id, docosan_user_id, phone_number_hash, access_token_enc, token_expires_at, environment, created_at, updated_at)
                VALUES
                    (UUID(), @TId, @PId, @UserId, @PhoneHash, @Token, @Exp, @Env, @Now, @Now)",
                new
                {
                    TId = _tenant.TenantId, PId = cmd.PatientId.ToString(), UserId = regResult.DocosanUserId,
                    PhoneHash = phoneHash, Token = accessTokenEnc, Exp = tokenExpiresAt,
                    Env = DocosanEnvironment.Current, Now = now
                });
        }
        else
        {
            await conn.ExecuteAsync(@"
                UPDATE diab_his_int_docosan_patient_mapping
                SET docosan_user_id=@UserId, phone_number_hash=@PhoneHash, access_token_enc=@Token,
                    token_expires_at=@Exp, updated_at=@Now
                WHERE id=@Id",
                new
                {
                    Id = (string)existing.id, UserId = regResult.DocosanUserId, PhoneHash = phoneHash,
                    Token = accessTokenEnc, Exp = tokenExpiresAt, Now = now
                });
        }

        await _audit.LogAsync("LINK_DOCOSAN_ACCOUNT", "Patient", cmd.PatientId.ToString(),
            new { docosan_user_id = regResult.DocosanUserId }, ct);

        return Result<LinkDocosanAccountResponse>.Success(
            new LinkDocosanAccountResponse(true, regResult.DocosanUserId, tokenExpiresAt));
    }
}

/// <summary>Moi truong Docosan hien hanh (staging|production), khop voi cau hinh Docosan:Environment.</summary>
public static class DocosanEnvironment
{
    public static string Current { get; set; } = "production";
}

// ═══════════════════════════════════════════════
// Tao phien tu van tu xa (FR-802) — validate FR-801 truoc
// ═══════════════════════════════════════════════
public class CreateTelehealthAppointmentCommandHandler
    : IRequestHandler<CreateTelehealthAppointmentCommand, Result<TelehealthSessionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ISettingsProvider _settings;
    private readonly IEncryptionService _enc;
    private readonly IDocosanClient _client;
    private readonly IAuditService _audit;
    private readonly ILogger<CreateTelehealthAppointmentCommandHandler> _logger;

    public CreateTelehealthAppointmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ISettingsProvider settings, IEncryptionService enc, IDocosanClient client, IAuditService audit,
        ILogger<CreateTelehealthAppointmentCommandHandler> logger)
    { _db = db; _tenant = tenant; _settings = settings; _enc = enc; _client = client; _audit = audit; _logger = logger; }

    public async Task<Result<TelehealthSessionResponse>> Handle(CreateTelehealthAppointmentCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        // 1) FR-801: validate lai o server, khong tin client
        var eligibility = await CheckTelehealthEligibilityQueryHandler.CheckAsync(_db, _tenant.TenantId, cmd.PatientId, _settings, ct);
        if (!eligibility.Eligible)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_NOT_ELIGIBLE",
                "Bệnh nhân chưa từng khám trực tiếp trong thời hạn quy định",
                new { eligibility.LastInPersonEncounterDate, eligibility.RequiredWithinDays });

        // 2) Resolve mapping bac si / phong kham / dich vu
        var doctorMap = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_doctor_mapping
            WHERE tenant_id=@TId AND user_id=@UId AND environment=@Env AND is_active=1 AND deleted_at IS NULL",
            new { TId = _tenant.TenantId, UId = cmd.Request.DoctorUserId.ToString(), Env = DocosanEnvironment.Current });
        if (doctorMap is null)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_DOCTOR_NOT_MAPPED", "Bác sĩ chưa được liên kết với hệ thống Docosan");

        var serviceMap = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_service_mapping
            WHERE tenant_id=@TId AND his_service_id=@SId AND environment=@Env
              AND docosan_service_type='telemedicine' AND is_active=1 AND deleted_at IS NULL",
            new { TId = _tenant.TenantId, SId = cmd.Request.HisServiceId.ToString(), Env = DocosanEnvironment.Current });
        if (serviceMap is null)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_SERVICE_NOT_CONFIGURED", "Chưa cấu hình dịch vụ tư vấn từ xa trên Docosan");

        var clinicMap = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_clinic_mapping
            WHERE tenant_id=@TId AND environment=@Env AND is_active=1 AND deleted_at IS NULL
            ORDER BY branch_id IS NULL LIMIT 1",
            new { TId = _tenant.TenantId, Env = DocosanEnvironment.Current });
        if (clinicMap is null)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_CLINIC_NOT_MAPPED", "Phòng khám chưa được liên kết với hệ thống Docosan");

        // 3) Dam bao token benh nhan con hieu luc
        var patientMap = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_patient_mapping
            WHERE tenant_id=@TId AND patient_id=@PId AND environment=@Env",
            new { TId = _tenant.TenantId, PId = cmd.PatientId.ToString(), Env = DocosanEnvironment.Current });

        if (patientMap is null || patientMap.access_token_enc is null ||
            ((DateTime?)patientMap.token_expires_at) < DateTime.UtcNow)
        {
            return Result<TelehealthSessionResponse>.Failure(
                "TELEHEALTH_ACCOUNT_NOT_LINKED",
                "Bệnh nhân chưa liên kết tài khoản Docosan hoặc token đã hết hạn, vui lòng liên kết lại");
        }

        string patientToken;
        try { patientToken = _enc.Decrypt(Encoding.UTF8.GetString((byte[])patientMap.access_token_enc)); }
        catch
        {
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_ACCOUNT_NOT_LINKED", "Lỗi giải mã token, vui lòng liên kết lại tài khoản Docosan");
        }

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT full_name, phone FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId",
            new { Id = cmd.PatientId.ToString(), TId = _tenant.TenantId });

        // 4) Goi Docosan tao lich (create-order-partner => mode=telemedicine)
        var bookingReq = new DocosanCreateBookingRequest(
            DocosanClinicId: (int)clinicMap.docosan_clinic_id,
            DocosanDoctorId: (int)doctorMap.docosan_doctor_id,
            DocosanServiceId: (int)serviceMap.docosan_service_id,
            ScheduledStart: cmd.Request.ScheduledStart,
            Symptom: cmd.Request.Symptom,
            PatientPhone: (string?)patient?.phone,
            PatientName: (string?)patient?.full_name);

        DocosanAppointmentDto apt;
        try
        {
            apt = await _client.CreateOrderPartnerAsync(bookingReq, patientToken, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi Docosan create-order-partner cho patient {PatientId}", cmd.PatientId);
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_PROVIDER_UNAVAILABLE", "Không kết nối được hệ thống Docosan, vui lòng thử lại");
        }

        if (!apt.Success || apt.AppointmentId is null)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_PROVIDER_UNAVAILABLE", "Không kết nối được hệ thống Docosan, vui lòng thử lại");

        // R1: kiem lai mode tra ve — neu khong phai telemedicine thi huy ngay
        if (!string.Equals(apt.Mode, "telemedicine", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Docosan tra ve mode khac telemedicine ({Mode}) cho appointment {AptId} — huy ngay", apt.Mode, apt.AppointmentId);
            try { await _client.CancelAppointmentAsync(apt.AppointmentId.Value, "Sai loai lich (khong phai telemedicine)", patientToken, ct); }
            catch { /* best-effort */ }
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_SERVICE_NOT_CONFIGURED", "Chưa cấu hình dịch vụ tư vấn từ xa trên Docosan");
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var scheduledStart = apt.ScheduledStart ?? cmd.Request.ScheduledStart;
        var scheduledEnd = apt.ScheduledEnd;
        var joinUrlEnc = !string.IsNullOrWhiteSpace(apt.AppointmentLink) ? _enc.Encrypt(apt.AppointmentLink) : null;
        var joinUrlExpiresAt = !string.IsNullOrWhiteSpace(apt.AppointmentLink) ? now.AddMinutes(120) : (DateTime?)null;
        var hisStatus = MapHisStatus(apt.Status);

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_tel_sessions
                (id, tenant_id, branch_id, patient_id, doctor_user_id, docosan_appointment_id, docosan_telemedicine_id,
                 docosan_clinic_id, docosan_doctor_id, docosan_mode, docosan_status, his_status,
                 scheduled_start, scheduled_end, join_url_enc, join_url_expires_at, symptom, payment_status,
                 eligibility_encounter_id, last_synced_at, created_at, updated_at)
            VALUES
                (@Id, @TId, @BranchId, @PatientId, @DoctorUserId, @AptId, @TeleId,
                 @ClinicId, @DoctorId, @Mode, @DoStatus, @HisStatus,
                 @Start, @End, @Join, @JoinExp, @Symptom, @Pay,
                 @EligEnc, @Now, @Now, @Now)",
            new
            {
                Id = id.ToString(), TId = _tenant.TenantId, BranchId = (int?)clinicMap.branch_id,
                PatientId = cmd.PatientId.ToString(), DoctorUserId = cmd.Request.DoctorUserId.ToString(),
                AptId = apt.AppointmentId, TeleId = apt.TeleMedicineId,
                ClinicId = (int)clinicMap.docosan_clinic_id, DoctorId = (int)doctorMap.docosan_doctor_id,
                Mode = apt.Mode ?? "telemedicine", DoStatus = apt.Status ?? "request", HisStatus = hisStatus,
                Start = scheduledStart, End = scheduledEnd, Join = joinUrlEnc, JoinExp = joinUrlExpiresAt,
                Symptom = cmd.Request.Symptom, Pay = apt.PaymentStatus,
                EligEnc = eligibility.LastInPersonEncounterId?.ToString(), Now = now
            });

        await _audit.LogAsync("CREATE_TELEHEALTH_SESSION", "TelehealthSession", id.ToString(),
            new { docosan_appointment_id = apt.AppointmentId }, ct);

        return Result<TelehealthSessionResponse>.Success(new TelehealthSessionResponse(
            id, cmd.PatientId, cmd.Request.DoctorUserId, apt.Status ?? "request", hisStatus,
            scheduledStart, scheduledEnd, apt.PaymentStatus, now));
    }

    public static string MapHisStatus(string? docosanStatus) => docosanStatus switch
    {
        "approve" => "CONFIRMED",
        "reject" => "CANCELLED",
        "on-hold" => "PENDING",
        "request" => "PENDING",
        _ => "PENDING"
    };
}

// ═══════════════════════════════════════════════
// Chi tiet phien
// ═══════════════════════════════════════════════
public class GetTelehealthSessionQueryHandler
    : IRequestHandler<GetTelehealthSessionQuery, Result<TelehealthSessionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetTelehealthSessionQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<TelehealthSessionResponse>> Handle(GetTelehealthSessionQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_tel_sessions
            WHERE id=@Id AND tenant_id=@TId AND patient_id=@PId AND deleted_at IS NULL",
            new { Id = q.SessionId.ToString(), TId = _tenant.TenantId, PId = q.PatientId.ToString() });

        if (row is null)
            return Result<TelehealthSessionResponse>.Failure("TELEHEALTH_SESSION_NOT_FOUND", "Không tìm thấy phiên tư vấn từ xa");

        return Result<TelehealthSessionResponse>.Success(new TelehealthSessionResponse(
            Guid.Parse((string)row.id), Guid.Parse((string)row.patient_id),
            row.doctor_user_id is not null ? Guid.Parse((string)row.doctor_user_id) : null,
            (string)row.docosan_status, (string)row.his_status,
            (DateTime)row.scheduled_start, (DateTime?)row.scheduled_end,
            (string?)row.payment_status, (DateTime?)row.last_synced_at));
    }
}

// ═══════════════════════════════════════════════
// Lay link vao phong (giai ma, kiem TTL)
// ═══════════════════════════════════════════════
public class GetTelehealthJoinLinkQueryHandler
    : IRequestHandler<GetTelehealthJoinLinkQuery, Result<TelehealthJoinLinkResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IEncryptionService _enc;
    private readonly IAuditService _audit;

    public GetTelehealthJoinLinkQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        IEncryptionService enc, IAuditService audit)
    { _db = db; _tenant = tenant; _enc = enc; _audit = audit; }

    public async Task<Result<TelehealthJoinLinkResponse>> Handle(GetTelehealthJoinLinkQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_tel_sessions
            WHERE id=@Id AND tenant_id=@TId AND patient_id=@PId AND deleted_at IS NULL",
            new { Id = q.SessionId.ToString(), TId = _tenant.TenantId, PId = q.PatientId.ToString() });

        if (row is null)
            return Result<TelehealthJoinLinkResponse>.Failure("TELEHEALTH_SESSION_NOT_FOUND", "Không tìm thấy phiên tư vấn từ xa");

        if (row.join_url_enc is null || (DateTime?)row.join_url_expires_at < DateTime.UtcNow)
            return Result<TelehealthJoinLinkResponse>.Failure("TELEHEALTH_JOIN_LINK_EXPIRED",
                "Liên kết vào phòng đã hết hạn, hệ thống sẽ tự làm mới trong lần đồng bộ tiếp theo");

        var url = _enc.Decrypt(Encoding.UTF8.GetString((byte[])row.join_url_enc));
        await _audit.LogAsync("VIEW_TELEHEALTH_JOIN_LINK", "TelehealthSession", (string)row.id, null, ct);

        return Result<TelehealthJoinLinkResponse>.Success(
            new TelehealthJoinLinkResponse(url, (DateTime)row.join_url_expires_at));
    }
}
