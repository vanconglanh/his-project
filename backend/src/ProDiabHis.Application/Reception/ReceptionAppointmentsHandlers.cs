using Dapper;
using MediatR;
using ProDiabHis.Application.Appointments;
using ProDiabHis.Application.Common;
using System.Data;

namespace ProDiabHis.Application.Reception;

// BM-33 (Lark): Lich hen (module Appointments) va Tiep don (module Reception) la 2 model du lieu
// hoan toan doc lap - lich hen CONFIRMED trong ngay khong tung hien thi ben Tiep don. Bo sung
// endpoint doc rieng de FE Tiep don liet ke cac lich hen CONFIRMED hom nay (gio VN), tu do le tan
// co the "tiep don tu lich hen" (xem CheckInCommandHandler.AppointmentId) hoac danh dau No-show.

/// <summary>Lich hen CONFIRMED hom nay (gio VN) - phuc vu man Tiep don</summary>
public record AppointmentTodayDto(
    int AppointmentId,
    string? PatientRef,
    string? PatientName,
    string? PatientPhone,
    string? DoctorRef,
    string? DoctorName,
    DateTime AppointmentAt,
    string? Note);

public record ListTodayConfirmedAppointmentsQuery() : IRequest<List<AppointmentTodayDto>>;

public class ListTodayConfirmedAppointmentsQueryHandler
    : IRequestHandler<ListTodayConfirmedAppointmentsQuery, List<AppointmentTodayDto>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;

    public ListTodayConfirmedAppointmentsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch)
    {
        _db = db;
        _tenant = tenant;
        _branch = branch;
    }

    public async Task<List<AppointmentTodayDto>> Handle(ListTodayConfirmedAppointmentsQuery request, CancellationToken cancellationToken)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var today = VnDate.Today.ToString("yyyy-MM-dd");

        // appointment_at la thoi diem hen (nhap theo gio VN tu luc tao), khong phai audit
        // timestamp UTC, nen so sanh DATE() truc tiep - khong can quy doi +7h nhu VnDate.FromUtc.
        var sql = $@"{AppointmentSql.SelectBase}
            WHERE a.tenant_id = @tenantId AND a.deleted_at IS NULL AND a.status = 'CONFIRMED'
              AND DATE(a.appointment_at) = @today AND {BranchSql.Condition("a")}
            ORDER BY a.appointment_at ASC";

        var rows = await conn.QueryAsync<AppointmentRowForReception>(sql, new
        {
            tenantId = _tenant.TenantId,
            today,
            branchId = _branch.BranchId,
            ignoreBranch = _branch.IgnoreBranchFilter
        });

        return rows.Select(r => new AppointmentTodayDto(
            r.Id, r.PatientRef, r.PatientName, PiiCrypto.Unprotect(r.PatientPhone),
            r.DoctorRef, r.DoctorName, r.AppointmentAt, r.Note)).ToList();
    }
}

/// <summary>Ban sao nhe cua AppointmentRow (internal, khac assembly namespace) de Dapper map dung cot
/// tu AppointmentSql.SelectBase ma khong phai public hoa AppointmentRow.</summary>
internal class AppointmentRowForReception
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
