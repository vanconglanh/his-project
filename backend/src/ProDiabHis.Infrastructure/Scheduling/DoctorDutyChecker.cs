using System.Data;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reception.Reassign;

namespace ProDiabHis.Infrastructure.Scheduling;

/// <summary>
/// [G05] Kiem tra lich truc bac si (diab_his_sch_doctor_schedules + diab_his_sch_schedule_blocks).
/// LUU Y MUI GIO: DB luu UTC, lich truc khai theo gio Viet Nam (Asia/Ho_Chi_Minh, UTC+7 co dinh,
/// khong co DST) -> quy doi truoc khi so day_of_week / start_time / end_time.
/// </summary>
public class DoctorDutyChecker : IDoctorDutyChecker
{
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);
    private readonly IDapperConnectionFactory _db;

    public DoctorDutyChecker(IDapperConnectionFactory db) => _db = db;

    public async Task<DoctorDutyStatus> CheckAsync(int tenantId, Guid doctorId, DateTime atUtc, CancellationToken ct = default)
    {
        var localNow = DateTime.SpecifyKind(atUtc, DateTimeKind.Utc) + VnOffset;
        var localDate = localNow.ToString("yyyy-MM-dd");
        var localTime = localNow.ToString("HH:mm:ss");
        // ISO: 1=Thu 2 ... 7=Chu nhat
        var isoDow = localNow.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)localNow.DayOfWeek;
        var label = $"{VnDayLabel(isoDow)}, {localNow:HH:mm}";

        using var conn = _db.CreateConnection();

        var onDuty = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(*) FROM diab_his_sch_doctor_schedules s
               WHERE s.tenant_id = @TenantId
                 AND s.doctor_ref = @DoctorId
                 AND s.enabled = 1
                 AND s.deleted_at IS NULL
                 AND s.day_of_week = @Dow
                 AND @Now BETWEEN s.start_time AND s.end_time
                 AND (s.effective_from IS NULL OR s.effective_from <= @Today)
                 AND (s.effective_to   IS NULL OR s.effective_to   >= @Today)",
            new { TenantId = tenantId, DoctorId = doctorId.ToString(), Dow = isoDow, Now = localTime, Today = localDate },
            cancellationToken: ct));

        var blocked = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(*) FROM diab_his_sch_schedule_blocks b
               WHERE b.tenant_id = @TenantId
                 AND b.doctor_ref = @DoctorId
                 AND b.block_date = @Today
                 AND b.deleted_at IS NULL
                 AND (b.start_time IS NULL OR @Now BETWEEN b.start_time AND b.end_time)",
            new { TenantId = tenantId, DoctorId = doctorId.ToString(), Now = localTime, Today = localDate },
            cancellationToken: ct));

        return new DoctorDutyStatus(onDuty > 0, blocked > 0, label);
    }

    private static string VnDayLabel(int isoDow) => isoDow switch
    {
        1 => "Thứ 2", 2 => "Thứ 3", 3 => "Thứ 4", 4 => "Thứ 5",
        5 => "Thứ 6", 6 => "Thứ 7", _ => "Chủ nhật"
    };
}
