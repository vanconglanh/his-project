using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Application.Scheduling;

// ============================================================
// Admin: quan ly lich lam viec bac si (diab_his_sch_doctor_schedules) + block nghi
// ============================================================

public record ListDoctorSchedulesQuery(int TenantId, Guid? DoctorRef) : IRequest<List<DoctorScheduleResponse>>;

public class ListDoctorSchedulesHandler : IRequestHandler<ListDoctorSchedulesQuery, List<DoctorScheduleResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public ListDoctorSchedulesHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<DoctorScheduleResponse>> Handle(ListDoctorSchedulesQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE tenant_id = @TenantId AND deleted_at IS NULL";
        if (q.DoctorRef.HasValue) where += " AND doctor_ref = @DoctorRef";

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, doctor_ref, day_of_week, start_time, end_time, slot_minutes, max_per_slot,
                      effective_from, effective_to, enabled
               FROM diab_his_sch_doctor_schedules
               {where}
               ORDER BY doctor_ref, day_of_week, start_time",
            new { q.TenantId, DoctorRef = q.DoctorRef?.ToString() });

        return rows.Select(r => (DoctorScheduleResponse)MapRow(r)).ToList();
    }

    internal static DoctorScheduleResponse MapRow(dynamic r)
    {
        DateTime? effFrom = r.effective_from;
        DateTime? effTo = r.effective_to;
        bool enabled = Convert.ToBoolean(r.enabled);
        DateOnly? effectiveFrom = effFrom.HasValue ? DateOnly.FromDateTime(effFrom.Value) : (DateOnly?)null;
        DateOnly? effectiveTo = effTo.HasValue ? DateOnly.FromDateTime(effTo.Value) : (DateOnly?)null;

        return new DoctorScheduleResponse(
            (int)r.id,
            Guid.Parse((string)r.doctor_ref),
            (int)r.day_of_week,
            TimeOnly.FromTimeSpan((TimeSpan)r.start_time),
            TimeOnly.FromTimeSpan((TimeSpan)r.end_time),
            (int)r.slot_minutes,
            (int)r.max_per_slot,
            effectiveFrom,
            effectiveTo,
            enabled);
    }
}

public record CreateDoctorScheduleCommand(int TenantId, DoctorScheduleUpsertRequest Request) : IRequest<DoctorScheduleResponse>;

public class CreateDoctorScheduleHandler : IRequestHandler<CreateDoctorScheduleCommand, DoctorScheduleResponse>
{
    private readonly IDapperConnectionFactory _db;
    public CreateDoctorScheduleHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<DoctorScheduleResponse> Handle(CreateDoctorScheduleCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var req = cmd.Request;

        var newId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_sch_doctor_schedules
                (tenant_id, doctor_ref, day_of_week, start_time, end_time, slot_minutes, max_per_slot,
                 effective_from, effective_to, enabled, created_at, updated_at)
              VALUES (@TenantId, @DoctorRef, @DayOfWeek, @StartTime, @EndTime, @SlotMinutes, @MaxPerSlot,
                      @EffectiveFrom, @EffectiveTo, @Enabled, UTC_TIMESTAMP(), UTC_TIMESTAMP());
              SELECT LAST_INSERT_ID();",
            new
            {
                cmd.TenantId, DoctorRef = req.DoctorRef.ToString(), DayOfWeek = req.DayOfWeek,
                StartTime = req.StartTime.ToTimeSpan(), EndTime = req.EndTime.ToTimeSpan(),
                req.SlotMinutes, req.MaxPerSlot,
                EffectiveFrom = req.EffectiveFrom?.ToString("yyyy-MM-dd"),
                EffectiveTo = req.EffectiveTo?.ToString("yyyy-MM-dd"),
                req.Enabled
            });

        return new DoctorScheduleResponse(
            newId, req.DoctorRef, req.DayOfWeek, req.StartTime, req.EndTime,
            req.SlotMinutes, req.MaxPerSlot, req.EffectiveFrom, req.EffectiveTo, req.Enabled);
    }
}

public record UpdateDoctorScheduleCommand(int Id, int TenantId, DoctorScheduleUpsertRequest Request) : IRequest<Result<DoctorScheduleResponse>>;

public class UpdateDoctorScheduleHandler : IRequestHandler<UpdateDoctorScheduleCommand, Result<DoctorScheduleResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public UpdateDoctorScheduleHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<DoctorScheduleResponse>> Handle(UpdateDoctorScheduleCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var req = cmd.Request;

        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_sch_doctor_schedules SET
                doctor_ref = @DoctorRef, day_of_week = @DayOfWeek, start_time = @StartTime, end_time = @EndTime,
                slot_minutes = @SlotMinutes, max_per_slot = @MaxPerSlot,
                effective_from = @EffectiveFrom, effective_to = @EffectiveTo, enabled = @Enabled,
                updated_at = UTC_TIMESTAMP()
              WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new
            {
                cmd.Id, cmd.TenantId, DoctorRef = req.DoctorRef.ToString(), DayOfWeek = req.DayOfWeek,
                StartTime = req.StartTime.ToTimeSpan(), EndTime = req.EndTime.ToTimeSpan(),
                req.SlotMinutes, req.MaxPerSlot,
                EffectiveFrom = req.EffectiveFrom?.ToString("yyyy-MM-dd"),
                EffectiveTo = req.EffectiveTo?.ToString("yyyy-MM-dd"),
                req.Enabled
            });

        if (affected == 0)
            return Result<DoctorScheduleResponse>.Failure("SCHEDULE_NOT_FOUND", "Không tìm thấy lịch làm việc");

        return Result<DoctorScheduleResponse>.Success(new DoctorScheduleResponse(
            cmd.Id, req.DoctorRef, req.DayOfWeek, req.StartTime, req.EndTime,
            req.SlotMinutes, req.MaxPerSlot, req.EffectiveFrom, req.EffectiveTo, req.Enabled));
    }
}

public record DeleteDoctorScheduleCommand(int Id, int TenantId) : IRequest<Result>;

public class DeleteDoctorScheduleHandler : IRequestHandler<DeleteDoctorScheduleCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    public DeleteDoctorScheduleHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result> Handle(DeleteDoctorScheduleCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE diab_his_sch_doctor_schedules SET deleted_at = UTC_TIMESTAMP() WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { cmd.Id, cmd.TenantId });

        if (affected == 0)
            return Result.Failure("SCHEDULE_NOT_FOUND", "Không tìm thấy lịch làm việc");

        return Result.Success();
    }
}

// ============================================================
// Admin: block nghi/khoa gio
// ============================================================

public record ListScheduleBlocksQuery(int TenantId, Guid? DoctorRef) : IRequest<List<ScheduleBlockResponse>>;

public class ListScheduleBlocksHandler : IRequestHandler<ListScheduleBlocksQuery, List<ScheduleBlockResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public ListScheduleBlocksHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<ScheduleBlockResponse>> Handle(ListScheduleBlocksQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE tenant_id = @TenantId AND deleted_at IS NULL";
        if (q.DoctorRef.HasValue) where += " AND doctor_ref = @DoctorRef";

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, doctor_ref, block_date, start_time, end_time, reason
               FROM diab_his_sch_schedule_blocks
               {where}
               ORDER BY block_date DESC",
            new { q.TenantId, DoctorRef = q.DoctorRef?.ToString() });

        return rows.Select(r => (ScheduleBlockResponse)MapBlockRow(r)).ToList();
    }

    internal static ScheduleBlockResponse MapBlockRow(dynamic r)
    {
        TimeSpan? startTime = r.start_time;
        TimeSpan? endTime = r.end_time;
        string? reason = r.reason;
        TimeOnly? startTimeOnly = startTime.HasValue ? TimeOnly.FromTimeSpan(startTime.Value) : (TimeOnly?)null;
        TimeOnly? endTimeOnly = endTime.HasValue ? TimeOnly.FromTimeSpan(endTime.Value) : (TimeOnly?)null;

        return new ScheduleBlockResponse(
            (int)r.id, Guid.Parse((string)r.doctor_ref), DateOnly.FromDateTime((DateTime)r.block_date),
            startTimeOnly, endTimeOnly, reason);
    }
}

public record CreateScheduleBlockCommand(int TenantId, ScheduleBlockCreateRequest Request) : IRequest<ScheduleBlockResponse>;

public class CreateScheduleBlockHandler : IRequestHandler<CreateScheduleBlockCommand, ScheduleBlockResponse>
{
    private readonly IDapperConnectionFactory _db;
    public CreateScheduleBlockHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ScheduleBlockResponse> Handle(CreateScheduleBlockCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var req = cmd.Request;

        var newId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_sch_schedule_blocks
                (tenant_id, doctor_ref, block_date, start_time, end_time, reason, created_at)
              VALUES (@TenantId, @DoctorRef, @BlockDate, @StartTime, @EndTime, @Reason, UTC_TIMESTAMP());
              SELECT LAST_INSERT_ID();",
            new
            {
                cmd.TenantId, DoctorRef = req.DoctorRef.ToString(),
                BlockDate = req.BlockDate.ToString("yyyy-MM-dd"),
                StartTime = req.StartTime?.ToTimeSpan(), EndTime = req.EndTime?.ToTimeSpan(),
                req.Reason
            });

        return new ScheduleBlockResponse(newId, req.DoctorRef, req.BlockDate, req.StartTime, req.EndTime, req.Reason);
    }
}

public record DeleteScheduleBlockCommand(int Id, int TenantId) : IRequest<Result>;

public class DeleteScheduleBlockHandler : IRequestHandler<DeleteScheduleBlockCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    public DeleteScheduleBlockHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result> Handle(DeleteScheduleBlockCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE diab_his_sch_schedule_blocks SET deleted_at = UTC_TIMESTAMP() WHERE id = @Id AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { cmd.Id, cmd.TenantId });

        if (affected == 0)
            return Result.Failure("SCHEDULE_BLOCK_NOT_FOUND", "Không tìm thấy block lịch");

        return Result.Success();
    }
}
