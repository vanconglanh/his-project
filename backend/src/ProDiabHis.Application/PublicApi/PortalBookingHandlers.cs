using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Portal booking: danh sach bac si co lich lam viec (fallback: bac si role BacSi)
// ============================================================
public record GetPortalBookingDoctorsQuery(int TenantId) : IRequest<List<PortalDoctorOptionResponse>>;

public class GetPortalBookingDoctorsHandler : IRequestHandler<GetPortalBookingDoctorsQuery, List<PortalDoctorOptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalBookingDoctorsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PortalDoctorOptionResponse>> Handle(GetPortalBookingDoctorsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.QueryAsync<(string id, string full_name)>(
            @"SELECT DISTINCT u.id, u.full_name
              FROM diab_his_sch_doctor_schedules s
              JOIN diab_his_sec_users u ON u.id = s.doctor_ref
              WHERE s.tenant_id = @TenantId AND s.enabled = 1 AND s.deleted_at IS NULL AND u.is_active = 1
              ORDER BY u.full_name",
            new { q.TenantId });

        if (rows.Any())
            return rows.Select(r => new PortalDoctorOptionResponse(Guid.Parse(r.id), r.full_name)).ToList();

        // Fallback: chua co lich lam viec cau hinh -> lay tat ca bac si dang hoat dong
        var fallback = await conn.QueryAsync<(string id, string full_name)>(
            @"SELECT DISTINCT u.id, u.full_name
              FROM diab_his_sec_users u
              JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id
              JOIN diab_his_sec_roles r ON r.id = ur.role_id
              WHERE u.tenant_id = @TenantId AND u.is_active = 1 AND r.code = 'BacSi'
              ORDER BY u.full_name",
            new { q.TenantId });

        return fallback.Select(r => new PortalDoctorOptionResponse(Guid.Parse(r.id), r.full_name)).ToList();
    }
}

// ============================================================
// Portal booking: sinh danh sach slot trong ngay cho 1 bac si
// ============================================================
public record GetPortalBookingSlotsQuery(int TenantId, Guid DoctorRef, DateOnly Date) : IRequest<List<PortalSlotResponse>>;

public class GetPortalBookingSlotsHandler : IRequestHandler<GetPortalBookingSlotsQuery, List<PortalSlotResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalBookingSlotsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PortalSlotResponse>> Handle(GetPortalBookingSlotsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var doctorRef = q.DoctorRef.ToString();
        var dateStr = q.Date.ToString("yyyy-MM-dd");
        int isoDayOfWeek = q.Date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)q.Date.DayOfWeek;

        var schedules = (await conn.QueryAsync<dynamic>(
            @"SELECT start_time, end_time, slot_minutes, max_per_slot
              FROM diab_his_sch_doctor_schedules
              WHERE tenant_id = @TenantId AND doctor_ref = @DoctorRef AND day_of_week = @Dow
                AND enabled = 1 AND deleted_at IS NULL
                AND (effective_from IS NULL OR effective_from <= @Date)
                AND (effective_to IS NULL OR effective_to >= @Date)",
            new { q.TenantId, DoctorRef = doctorRef, Dow = isoDayOfWeek, Date = dateStr })).ToList();

        if (schedules.Count == 0) return new List<PortalSlotResponse>();

        var blocks = (await conn.QueryAsync<dynamic>(
            @"SELECT start_time, end_time FROM diab_his_sch_schedule_blocks
              WHERE tenant_id = @TenantId AND doctor_ref = @DoctorRef AND block_date = @Date AND deleted_at IS NULL",
            new { q.TenantId, DoctorRef = doctorRef, Date = dateStr })).ToList();

        var takenTimes = (await conn.QueryAsync<DateTime>(
            @"SELECT appointment_at FROM diab_his_sch_appointments
              WHERE tenant_id = @TenantId AND doctor_ref = @DoctorRef
                AND DATE(appointment_at) = @Date AND status <> 'CANCELLED' AND deleted_at IS NULL",
            new { q.TenantId, DoctorRef = doctorRef, Date = dateStr })).ToHashSet();

        var result = new List<PortalSlotResponse>();

        foreach (var s in schedules)
        {
            TimeSpan start = (TimeSpan)s.start_time;
            TimeSpan end = (TimeSpan)s.end_time;
            int slotMinutes = (int)s.slot_minutes;

            for (var t = start; t + TimeSpan.FromMinutes(slotMinutes) <= end; t += TimeSpan.FromMinutes(slotMinutes))
            {
                var slotAt = q.Date.ToDateTime(TimeOnly.MinValue) + t;

                bool blocked = blocks.Any(b =>
                {
                    if (b.start_time == null && b.end_time == null) return true; // block ca ngay
                    TimeSpan? bs = b.start_time != null ? (TimeSpan)b.start_time : (TimeSpan?)null;
                    TimeSpan? be = b.end_time != null ? (TimeSpan)b.end_time : (TimeSpan?)null;
                    if (bs.HasValue && be.HasValue) return t >= bs.Value && t < be.Value;
                    return false;
                });

                bool taken = takenTimes.Contains(slotAt);

                result.Add(new PortalSlotResponse(slotAt, !blocked && !taken));
            }
        }

        return result;
    }
}
