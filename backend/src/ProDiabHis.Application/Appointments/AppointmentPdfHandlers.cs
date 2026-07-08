using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Appointments;

public record GetAppointmentSlipPdfQuery(int AppointmentId) : IRequest<Result<byte[]>>;

internal class AppointmentSlipRow
{
    public int Id { get; set; }
    public DateTime AppointmentAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public string? DoctorName { get; set; }
    public int? DepartmentId { get; set; }
    public string? Note { get; set; }
}

public class GetAppointmentSlipPdfQueryHandler : IRequestHandler<GetAppointmentSlipPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAppointmentSlipPdfBuilder _builder;

    public GetAppointmentSlipPdfQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAppointmentSlipPdfBuilder builder)
    { _db = db; _tenant = tenant; _builder = builder; }

    public async Task<Result<byte[]>> Handle(GetAppointmentSlipPdfQuery q, CancellationToken ct)
    {
        using var conn = (System.Data.IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        // Ghi chu (bug tien nhiem #4): diab_his_sch_appointments dung schema INT-id legacy,
        // cot patient_id/doctor_id la INT khong tuong thich kieu voi diab_his_pat_patients.id /
        // diab_his_sec_users.id (char(36)) nen KHONG join truc tiep duoc qua patient_id/doctor_id.
        // Giai phap: dung 2 cot GUID moi patient_ref/doctor_ref (migration
        // 9038_sch_appointments_add_guid_refs.sql) de LEFT JOIN lay ten that khi co; neu
        // patient_ref/doctor_ref con NULL (du lieu cu chua map duoc) thi fallback ve
        // patient_name_temp/patient_phone nhu truoc.
        var row = await conn.QueryFirstOrDefaultAsync<AppointmentSlipRow>(
            @"SELECT a.id AS Id, a.appointment_at AS AppointmentAt, a.duration_minutes AS DurationMinutes,
                     a.status AS Status,
                     COALESCE(pat.full_name, a.patient_name_temp) AS PatientName,
                     COALESCE(pat.phone, a.patient_phone) AS PatientPhone,
                     doc.full_name AS DoctorName,
                     a.department_id AS DepartmentId, a.note AS Note
              FROM diab_his_sch_appointments a
              LEFT JOIN diab_his_pat_patients pat ON pat.id = a.patient_ref AND pat.tenant_id = a.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = a.doctor_ref
              WHERE a.id = @id AND a.tenant_id = @tenantId AND a.deleted_at IS NULL",
            new { id = q.AppointmentId, tenantId });

        if (row == null)
            return Result<byte[]>.Failure("APPOINTMENT_NOT_FOUND", "Không tìm thấy giấy hẹn tái khám");

        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });
        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var data = new AppointmentSlipData(
            lh, row.Id, row.AppointmentAt, row.DurationMinutes, row.Status,
            string.IsNullOrWhiteSpace(row.PatientName) ? "—" : row.PatientName,
            row.PatientPhone, row.DoctorName, row.DepartmentId, row.Note);

        var pdf = _builder.Build(data);
        return Result<byte[]>.Success(pdf);
    }
}
