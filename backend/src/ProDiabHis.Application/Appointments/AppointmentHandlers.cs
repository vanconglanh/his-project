using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
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
               COALESCE(pat.phone, a.patient_phone) AS PatientPhone,
               a.doctor_ref AS DoctorRef,
               doc.full_name AS DoctorName,
               a.note AS Note
        FROM diab_his_sch_appointments a
        LEFT JOIN diab_his_pat_patients pat ON pat.id = a.patient_ref AND pat.tenant_id = a.tenant_id
        LEFT JOIN diab_his_sec_users doc ON doc.id = a.doctor_ref";

    public static AppointmentResponse ToResponse(AppointmentRow r) => new(
        r.Id, r.AppointmentAt, r.DurationMinutes, r.Status, r.Source,
        r.PatientRef, r.PatientName, r.PatientPhone, r.DoctorRef, r.DoctorName, r.Note);
}

public class ListAppointmentsQueryHandler : IRequestHandler<ListAppointmentsQuery, PagedResult<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListAppointmentsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<PagedResult<AppointmentResponse>> Handle(ListAppointmentsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var where = "WHERE a.tenant_id = @tenantId AND a.deleted_at IS NULL";
        if (request.From.HasValue) where += " AND a.appointment_at >= @from";
        if (request.To.HasValue) where += " AND a.appointment_at <= @to";
        if (!string.IsNullOrWhiteSpace(request.DoctorRef)) where += " AND a.doctor_ref = @doctorRef";
        if (!string.IsNullOrWhiteSpace(request.Status)) where += " AND a.status = @status";
        if (!string.IsNullOrWhiteSpace(request.Q))
            where += " AND (COALESCE(pat.full_name, a.patient_name_temp) LIKE @q OR COALESCE(pat.phone, a.patient_phone) LIKE @q)";

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
            from = request.From,
            to = request.To,
            doctorRef = request.DoctorRef,
            status = request.Status,
            q = qParam,
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

    public GetAppointmentQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<AppointmentResponse>> Handle(GetAppointmentQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var sql = $@"{AppointmentSql.SelectBase}
            WHERE a.id = @id AND a.tenant_id = @tenantId AND a.deleted_at IS NULL";

        var row = await conn.QueryFirstOrDefaultAsync<AppointmentRow>(sql, new { id = request.Id, tenantId = _tenant.TenantId });
        if (row is null)
            return Result<AppointmentResponse>.Failure("APPOINTMENT_NOT_FOUND", "Không tìm thấy lịch hẹn");

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row));
    }
}

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public CreateAppointmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<AppointmentResponse>> Handle(CreateAppointmentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

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
                (tenant_id, patient_ref, patient_name_temp, patient_phone, doctor_ref,
                 appointment_at, duration_minutes, status, source, note, created_at, updated_at)
            VALUES
                (@tenantId, @patientRef, @patientNameTemp, @patientPhone, @doctorRef,
                 @appointmentAt, @duration, 'PENDING', @source, @note, @now, @now);
            SELECT LAST_INSERT_ID();";

        var now = DateTime.UtcNow;
        var newId = await conn.ExecuteScalarAsync<int>(insertSql, new
        {
            tenantId,
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
    private readonly IAuditService _audit;

    public UpdateAppointmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<AppointmentResponse>> Handle(UpdateAppointmentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var existsCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sch_appointments WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id = command.Id, tenantId });
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
    private readonly IAuditService _audit;

    public UpdateAppointmentStatusCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<AppointmentResponse>> Handle(UpdateAppointmentStatusCommand command, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var existsCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sch_appointments WHERE id=@id AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { id = command.Id, tenantId });
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

        return Result<AppointmentResponse>.Success(AppointmentSql.ToResponse(row!));
    }
}

public class ListDoctorOptionsQueryHandler : IRequestHandler<ListDoctorOptionsQuery, List<OptionDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListDoctorOptionsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<List<OptionDto>> Handle(ListDoctorOptionsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var sql = @"
            SELECT DISTINCT u.id AS Value, u.full_name AS Label
            FROM diab_his_sec_users u
            JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id AND ur.tenant_id = @tenantId
            JOIN diab_his_sec_roles r ON r.id = ur.role_id AND r.code = 'bac_si'
            WHERE u.tenant_id = @tenantId AND u.deleted_at IS NULL
            ORDER BY u.full_name ASC";

        var rows = await conn.QueryAsync<OptionDto>(sql, new { tenantId = _tenant.TenantId });
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
            SELECT id AS Value, full_name AS Label, phone AS Phone
            FROM diab_his_pat_patients
            WHERE tenant_id = @tenantId AND deleted_at IS NULL
              AND (full_name LIKE @q OR code LIKE @q OR phone LIKE @q)
            ORDER BY full_name ASC
            LIMIT 20";

        var rows = await conn.QueryAsync<PatientOptionDto>(sql, new { tenantId = _tenant.TenantId, q });
        return rows.ToList();
    }
}
