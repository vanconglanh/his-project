using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;
using System.Data;

namespace ProDiabHis.Application.Appointments;

internal class AppointmentRow
{
    public int Id { get; set; }
    public DateTime AppointmentAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string Source { get; set; } = "";
    public string? PatientRef { get; set; }
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public string? DoctorRef { get; set; }
    public string? DoctorName { get; set; }
    public string? Note { get; set; }
}

/// <summary>Truy van dung chung: join patient_ref -> diab_his_pat_patients,
/// doctor_ref -> diab_his_sec_users (fallback patient_name_temp/patient_phone khi chua co ho so).
/// Ca 2 cot patient_ref/doctor_ref va bang dich cung collation utf8mb4_0900_ai_ci (da introspect DB)
/// nen KHONG can COLLATE ep kieu khi join.</summary>
internal static class AppointmentSql
{
    public const string SelectBase = @"
        SELECT a.id AS Id, a.appointment_at AS AppointmentAt, a.duration_minutes AS DurationMinutes,
               a.status AS Status, a.source AS Source,
               a.patient_ref AS PatientRef,
               COALESCE(pat.full_name, a.patient_name_temp) AS PatientName,
               COALESCE(pat.phone_enc, a.patient_phone) AS PatientPhone,
               a.doctor_ref AS DoctorRef,
               doc.full_name AS DoctorName,
               a.note AS Note
        FROM diab_his_sch_appointments a
        LEFT JOIN diab_his_pat_patients pat ON pat.id = a.patient_ref AND pat.tenant_id = a.tenant_id
        LEFT JOIN diab_his_sec_users doc ON doc.id = a.doctor_ref";

    // Hang muc 6: pat.phone_enc da ma hoa -> phai giai ma truoc khi tra ra API.
    // a.patient_phone (lich hen vang lai, chua co ho so BN) van la plaintext,
    // PiiCrypto.Unprotect tu nhan biet qua marker nen an toan cho ca 2 nguon.
    public static AppointmentResponse ToResponse(AppointmentRow r) => new(
        r.Id, r.AppointmentAt, r.DurationMinutes, r.Status, r.Source,
        r.PatientRef, r.PatientName, PiiCrypto.Unprotect(r.PatientPhone),
        r.DoctorRef, r.DoctorName, r.Note);
}

public class ListAppointmentsQueryHandler : IRequestHandler<ListAppointmentsQuery, PagedResult<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;

    public ListAppointmentsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch)
    { _db = db; _tenant = tenant; _branch = branch; }

    public async Task<PagedResult<AppointmentResponse>> Handle(ListAppointmentsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var branchId = _branch.BranchId;
        var ignoreBranch = _branch.IgnoreBranchFilter;

        var where = "WHERE a.tenant_id = @tenantId AND a.deleted_at IS NULL AND " + BranchSql.Condition("a");
        if (request.From.HasValue) where += " AND a.appointment_at >= @from";
        if (request.To.HasValue) where += " AND a.appointment_at <= @to";
        if (!string.IsNullOrWhiteSpace(request.DoctorRef)) where += " AND a.doctor_ref = @doctorRef";
        if (!string.IsNullOrWhiteSpace(request.Status)) where += " AND a.status = @status";
        if (!string.IsNullOrWhiteSpace(request.Q))
            // Hang muc 6: SDT benh nhan da ma hoa -> tra cuu bang blind index (exact-match).
            // patient_phone cua lich hen vang lai chua ma hoa nen van LIKE duoc.
            where += " AND (COALESCE(pat.full_name, a.patient_name_temp) LIKE @q"
                   + " OR pat.phone_bidx = @phoneBidx OR a.patient_phone LIKE @q)";

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        var offset = (page - 1) * pageSize;

        var countSql = $@"SELECT COUNT(*) FROM diab_his_sch_appointments a
            LEFT JOIN diab_his_pat_patients pat ON pat.id = a.patient_ref AND pat.tenant_id = a.tenant_id
            {where}";

        var listSql = $@"{AppointmentSql.SelectBase}
            {where}
            ORDER BY a.appointment_at ASC
            LIMIT @pageSize OFFSET @offset";

        var qParam = $"%{request.Q}%";
        var parameters = new
        {
            tenantId,
            branchId,
            ignoreBranch,
            from = request.From,
            to = request.To,
            doctorRef = request.DoctorRef,
            status = request.Status,
            q = qParam,
            phoneBidx = PiiCrypto.BlindIndex(request.Q, PiiField.Phone),
            pageSize,
            offset
        };

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await conn.QueryAsync<AppointmentRow>(listSql, parameters);
        var items = rows.Select(AppointmentSql.ToResponse).ToList();

        return new PagedResult<AppointmentResponse>(items, page, pageSize, total);
    }
}

public class GetAppointmentQueryHandler : IRequestHandler<GetAppointmentQuery, Result<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;

    public GetAppointmentQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch)
    { _db = db; _tenant = tenant; _branch = branch; }

    public async Task<Result<AppointmentResponse>> Handle(GetAppointmentQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var sql = $@"{AppointmentSql.SelectBase}
            WHERE a.id = @id AND a.tenant_id = @tenantId AND a.deleted_at IS NULL AND {BranchSql.Condition("a")}";

        var row = await conn.QueryFirstOrDefaultAsync<AppointmentRow>(sql, new
        {
            id = request.Id, tenantId = _tenant.TenantId,
            branchId = _branch.BranchId, ignoreBranch = _branch.IgnoreBranchFilter
        });
        if (row is null)
            return Result<AppointmentResponse>.Failure("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn");

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row));
    }
}

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;
    private readonly IAuditService _audit;

    public CreateAppointmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch, IAuditService audit)
    { _db = db; _tenant = tenant; _branch = branch; _audit = audit; }

    public async Task<Result<AppointmentResponse>> Handle(CreateAppointmentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        // branch_id luon lay tu IBranchProvider (khong trust client). Neu user co cross_view
        // va khong chon branch cu the (BranchId=0) thi khong doan - tra ve BRANCH_REQUIRED.
        if (_branch.BranchId <= 0)
            return Result<AppointmentResponse>.Failure("BRANCH_REQUIRED", "Vui long chon chi nhanh truoc khi tao lich hen");
        var branchId = _branch.BranchId;

        if (!string.IsNullOrWhiteSpace(req.PatientRef))
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pat_patients WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { id = req.PatientRef, tenantId });
            if (exists == 0)
                return Result<AppointmentResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");
        }

        if (!string.IsNullOrWhiteSpace(req.DoctorRef))
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sec_users WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { id = req.DoctorRef, tenantId });
            if (exists == 0)
                return Result<AppointmentResponse>.Failure("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ");
        }

        var duration = req.DurationMinutes ?? 30;
        var source = string.IsNullOrWhiteSpace(req.Source) ? "WALK_IN" : req.Source;

        var insertSql = @"
            INSERT INTO diab_his_sch_appointments
                (tenant_id, branch_id, patient_ref, patient_name_temp, patient_phone, doctor_ref,
                 appointment_at, duration_minutes, status, source, note, created_at, updated_at)
            VALUES
                (@tenantId, @branchId, @patientRef, @patientNameTemp, @patientPhone, @doctorRef,
                 @appointmentAt, @duration, 'PENDING', @source, @note, @now, @now);
            SELECT LAST_INSERT_ID();";

        var now = DateTime.UtcNow;
        var newId = await conn.ExecuteScalarAsync<int>(insertSql, new
        {
            tenantId,
            branchId,
            patientRef = req.PatientRef,
            patientNameTemp = req.PatientNameTemp,
            patientPhone = req.PatientPhone,
            doctorRef = req.DoctorRef,
            appointmentAt = req.AppointmentAt,
            duration,
            source,
            note = req.Note,
            now
        });

        await _audit.LogAsync("CREATE", "Appointment", newId.ToString(), new { req.AppointmentAt, req.PatientRef, req.DoctorRef }, ct);

        var row = await conn.QueryFirstOrDefaultAsync<AppointmentRow>(
            $"{AppointmentSql.SelectBase} WHERE a.id=@id AND a.tenant_id=@tenantId",
            new { id = newId, tenantId });

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row!));
    }
}

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;
    private readonly IAuditService _audit;

    public UpdateAppointmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch, IAuditService audit)
    { _db = db; _tenant = tenant; _branch = branch; _audit = audit; }

    public async Task<Result<AppointmentResponse>> Handle(UpdateAppointmentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var branchId = _branch.BranchId;
        var ignoreBranch = _branch.IgnoreBranchFilter;

        var existsCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_sch_appointments WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL AND {BranchSql.Condition("")}",
            new { id = command.Id, tenantId, branchId, ignoreBranch });
        if (existsCount == 0)
            return Result<AppointmentResponse>.Failure("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn");

        if (!string.IsNullOrWhiteSpace(req.PatientRef))
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pat_patients WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { id = req.PatientRef, tenantId });
            if (exists == 0)
                return Result<AppointmentResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");
        }

        if (!string.IsNullOrWhiteSpace(req.DoctorRef))
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_sec_users WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { id = req.DoctorRef, tenantId });
            if (exists == 0)
                return Result<AppointmentResponse>.Failure("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ");
        }

        var duration = req.DurationMinutes ?? 30;
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            UPDATE diab_his_sch_appointments
            SET patient_ref=@patientRef, patient_name_temp=@patientNameTemp, patient_phone=@patientPhone,
                doctor_ref=@doctorRef, appointment_at=@appointmentAt, duration_minutes=@duration,
                note=@note, updated_at=@now
            WHERE id=@id AND tenant_id=@tenantId",
            new
            {
                id = command.Id,
                tenantId,
                patientRef = req.PatientRef,
                patientNameTemp = req.PatientNameTemp,
                patientPhone = req.PatientPhone,
                doctorRef = req.DoctorRef,
                appointmentAt = req.AppointmentAt,
                duration,
                note = req.Note,
                now
            });

        await _audit.LogAsync("UPDATE", "Appointment", command.Id.ToString(), new { req.AppointmentAt, req.PatientRef, req.DoctorRef }, ct);

        var row = await conn.QueryFirstOrDefaultAsync<AppointmentRow>(
            $"{AppointmentSql.SelectBase} WHERE a.id=@id AND a.tenant_id=@tenantId",
            new { id = command.Id, tenantId });

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row!));
    }
}

public class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand, Result<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;
    private readonly IAuditService _audit;
    private readonly IPackageEntitlementService _packageEntitlement;
    private readonly Microsoft.Extensions.Logging.ILogger<UpdateAppointmentStatusCommandHandler> _logger;

    public UpdateAppointmentStatusCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch,
        IAuditService audit, IPackageEntitlementService packageEntitlement,
        Microsoft.Extensions.Logging.ILogger<UpdateAppointmentStatusCommandHandler> logger)
    { _db = db; _tenant = tenant; _branch = branch; _audit = audit; _packageEntitlement = packageEntitlement; _logger = logger; }

    public async Task<Result<AppointmentResponse>> Handle(UpdateAppointmentStatusCommand command, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var branchId = _branch.BranchId;
        var ignoreBranch = _branch.IgnoreBranchFilter;

        var existsCount = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_sch_appointments WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL AND {BranchSql.Condition("")}",
            new { id = command.Id, tenantId, branchId, ignoreBranch });
        if (existsCount == 0)
            return Result<AppointmentResponse>.Failure("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn");

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            "UPDATE diab_his_sch_appointments SET status=@status, updated_at=@now WHERE id=@id AND tenant_id=@tenantId",
            new { id = command.Id, tenantId, status = command.Status, now });

        await _audit.LogAsync("UPDATE_STATUS", "Appointment", command.Id.ToString(), new { command.Status }, ct);

        var row = await conn.QueryFirstOrDefaultAsync<AppointmentRow>(
            $"{AppointmentSql.SelectBase} WHERE a.id=@id AND a.tenant_id=@tenantId",
            new { id = command.Id, tenantId });

        // FR-1204 (D7) - tru dinh muc "lan kham" (item_type=VISIT) khi check-in thanh cong.
        // Best-effort: khong duoc chan luong tiep don neu goi dinh muc loi/khong co (giong pattern
        // da ap dung o CreatePrescriptionHandler). ConsumeAsync idempotent theo (source_type,
        // source_id, balance_id) nen goi lai (retry, doi trang thai nhieu lan) khong tru trung.
        if (row != null && string.Equals(command.Status, AppointmentStatus.CheckedIn, StringComparison.Ordinal)
            && Guid.TryParse(row.PatientRef, out var patientGuid))
        {
            try
            {
                var visitBalance = await conn.QueryFirstOrDefaultAsync<string?>(
                    @"SELECT b.item_ref_id
                      FROM diab_his_pkg_entitlement_balances b
                      JOIN diab_his_pkg_subscriptions s ON s.id = b.subscription_id
                      WHERE s.tenant_id=@tenantId AND s.patient_id=@patientId AND s.status='active'
                        AND s.expiry_date >= CURDATE()
                        AND b.item_type='VISIT' AND b.remaining_quantity > 0
                      ORDER BY s.expiry_date ASC, s.purchase_date ASC, b.id ASC
                      LIMIT 1",
                    new { tenantId, patientId = patientGuid.ToString() });

                if (visitBalance != null && Guid.TryParse(visitBalance, out var visitItemRefId))
                {
                    await _packageEntitlement.ConsumeAsync(
                        new PackageCoverageRequest(
                            patientGuid, "APPOINTMENT", AppointmentIdToGuid(command.Id),
                            new[] { new PackageCoverageLineRequest(PackageItemType.VISIT, visitItemRefId, 1) },
                            null, branchId > 0 ? branchId : null),
                        ct);
                }
            }
            catch (PackageBalanceConflictException ex)
            {
                _logger.LogWarning(ex, "PackageEntitlement conflict khi check-in appointment {Id}, bo qua tru dinh muc lan nay", command.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Khong the tru dinh muc goi khi check-in appointment {Id}, bo qua (khong chan tiep don)", command.Id);
            }
        }

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row!));
    }

    /// <summary>
    /// diab_his_sch_appointments.id la INT (auto-increment), nhung IPackageEntitlementService.SourceId
    /// yeu cau Guid (chuan theo cac nguon khac deu dung CHAR36). Sinh Guid tat dinh (deterministic)
    /// tu int id de dam bao idempotency_key on dinh giua cac lan goi cho cung 1 appointment.
    /// </summary>
    private static Guid AppointmentIdToGuid(int id) => new Guid(id, 0, 0, new byte[8]);
}

public class ListDoctorOptionsQueryHandler : IRequestHandler<ListDoctorOptionsQuery, List<OptionDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;

    public ListDoctorOptionsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch)
    { _db = db; _tenant = tenant; _branch = branch; }

    public async Task<List<OptionDto>> Handle(ListDoctorOptionsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        // Bac si duoc gan qua diab_his_sec_user_branches (N-N) -> uu tien loc theo bang nay;
        // fallback u.branch_id (chi nhanh mac dinh) cho du lieu cu chua co dong user_branches.
        var sql = $@"
            SELECT DISTINCT u.id AS Value, u.full_name AS Label
            FROM diab_his_sec_users u
            JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id AND ur.tenant_id = @tenantId
            JOIN diab_his_sec_roles r ON r.id = ur.role_id AND r.code = 'bac_si'
            LEFT JOIN diab_his_sec_user_branches ub ON ub.user_id = u.id AND ub.deleted_at IS NULL
            WHERE u.tenant_id = @tenantId AND u.deleted_at IS NULL
              AND (@ignoreBranch = 1
                   OR ub.branch_id = @branchId
                   OR (ub.branch_id IS NULL AND {BranchSql.Condition("u")}))
            ORDER BY u.full_name ASC";

        var rows = await conn.QueryAsync<OptionDto>(sql, new
        {
            tenantId = _tenant.TenantId,
            branchId = _branch.BranchId,
            ignoreBranch = _branch.IgnoreBranchFilter
        });
        return rows.ToList();
    }
}

public class ListPatientOptionsQueryHandler : IRequestHandler<ListPatientOptionsQuery, List<PatientOptionDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListPatientOptionsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<List<PatientOptionDto>> Handle(ListPatientOptionsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var q = string.IsNullOrWhiteSpace(request.Q) ? "%" : $"%{request.Q}%";

        var sql = @"
            SELECT id AS Value, full_name AS Label, phone_enc AS Phone
            FROM diab_his_pat_patients
            WHERE tenant_id = @tenantId AND deleted_at IS NULL
              AND (full_name LIKE @q OR code LIKE @q OR phone_bidx = @phoneBidx)
            ORDER BY full_name ASC
            LIMIT 20";

        // Hang muc 6: SDT da ma hoa -> tra cuu bang blind index (exact-match), khong con LIKE
        var phoneBidx = PiiCrypto.BlindIndex(request.Q, PiiField.Phone);
        var rows = await conn.QueryAsync<PatientOptionDto>(sql, new { tenantId = _tenant.TenantId, q, phoneBidx });
        return rows.Select(r => r with { Phone = PiiCrypto.Unprotect(r.Phone) }).ToList();
    }
}
