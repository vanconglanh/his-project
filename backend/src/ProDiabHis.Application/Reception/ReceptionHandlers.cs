using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using System.Text.Json;

namespace ProDiabHis.Application.Reception;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, Result<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CheckInCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<ReceptionTicketResponse>> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        using var conn = _db.CreateConnection();

        // Validate patient
        var patient = await conn.QueryFirstOrDefaultAsync(
            "SELECT id, code, full_name, gender, date_of_birth FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = req.PatientId.ToString(), TenantId = _tenant.TenantId });
        if (patient is null)
            return Result<ReceptionTicketResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        // Validate room
        var room = await conn.QueryFirstOrDefaultAsync(
            "SELECT id, name, code AS room_code, capacity AS max_per_day FROM diab_his_sys_rooms WHERE id=@Id AND (tenant_id=@TenantId OR tenant_id IS NULL) AND deleted_at IS NULL",
            new { Id = req.RoomId.ToString(), TenantId = _tenant.TenantId });
        if (room is null)
            return Result<ReceptionTicketResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var patId = ((object)patient.id).ToString()!;

        // Check duplicate check-in today
        var dupCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_rcp_queue_tickets WHERE tenant_id=@TenantId AND patient_id=@PatId AND room_id=@RoomId AND ticket_date=@Date AND status NOT IN ('CANCELLED') AND deleted_at IS NULL",
            new { TenantId = _tenant.TenantId, PatId = patId, RoomId = req.RoomId.ToString(), Date = today });
        if (dupCount > 0)
            return Result<ReceptionTicketResponse>.Failure("RECEPTION_DUPLICATE_CHECKIN", "Bệnh nhân đã được tiếp đón hôm nay tại phòng này");

        // Check room capacity
        var todayCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_rcp_queue_tickets WHERE tenant_id=@TenantId AND room_id=@RoomId AND ticket_date=@Date AND status NOT IN ('CANCELLED') AND deleted_at IS NULL",
            new { TenantId = _tenant.TenantId, RoomId = req.RoomId.ToString(), Date = today });
        if (todayCount >= (int)room.max_per_day)
            return Result<ReceptionTicketResponse>.Failure("RECEPTION_ROOM_FULL", "Phòng khám đã đạt giới hạn lượt khám tối đa");

        // Generate ticket_no (so thu tu trong ngay, theo phong): bang thuc te dung ticket_no/ticket_date,
        // khong co cot queue_number (xem 0022_create_reception_queue.sql)
        var maxNo = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(CAST(ticket_no AS UNSIGNED)), 0) FROM diab_his_rcp_queue_tickets WHERE tenant_id=@TenantId AND room_id=@RoomId AND ticket_date=@Date AND deleted_at IS NULL",
            new { TenantId = _tenant.TenantId, RoomId = req.RoomId.ToString(), Date = today });
        var queueNumber = maxNo + 1;
        var ticketNo = $"{queueNumber:D3}";

        var newId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var priority = req.Priority ?? TicketPriority.Normal;

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_rcp_queue_tickets
                (id, tenant_id, patient_id, room_id, ticket_no, ticket_date, status, priority, reason_for_visit, note, created_at, updated_at)
              VALUES (@Id, @TenantId, @PatId, @RoomId, @TicketNo, @TicketDate, 'WAITING', @Priority, @ReasonForVisit, @Note, @Now, @Now)",
            new
            {
                Id = newId, TenantId = _tenant.TenantId, PatId = patId, RoomId = req.RoomId.ToString(),
                TicketNo = ticketNo, TicketDate = today, Priority = priority, ReasonForVisit = req.ReasonForVisit,
                Note = req.Note, Now = now
            });

        await _audit.LogAsync("CHECKIN", "ReceptionTicket", newId, new { ticketNo, roomId = req.RoomId }, cancellationToken);

        return Result<ReceptionTicketResponse>.Success(new ReceptionTicketResponse(
            Guid.Parse(newId),
            _tenant.TenantId,
            req.PatientId,
            new PatientSummaryDto(req.PatientId, (string)patient.code, (string)patient.full_name,
                patient.date_of_birth is DateTime dob ? DateOnly.FromDateTime(dob) : null,
                (string?)patient.gender, null),
            ticketNo,
            req.RoomId,
            (string)room.name,
            null, null,
            new List<ServicePackageDto>(),
            req.ReasonForVisit,
            TicketStatus.Waiting,
            priority,
            now,
            null, null, null,
            _currentUser.UserId,
            req.Note));
    }
}

public class CallTicketCommandHandler : IRequestHandler<CallTicketCommand, Result<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBackgroundJobEnqueuer _jobs;

    public CallTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBackgroundJobEnqueuer jobs)
    {
        _db = db;
        _tenant = tenant;
        _jobs = jobs;
    }

    public async Task<Result<ReceptionTicketResponse>> Handle(CallTicketCommand command, CancellationToken cancellationToken)
    {
        var result = await TicketTransitionHelper.TransitionTicket(_db, _tenant, command.TicketId, TicketStatus.Called, cancellationToken);

        // Bao cho benh nhan gan den luot (fire-and-forget qua Hangfire, khong chan response)
        if (result.IsSuccess && result.Value!.RoomId != Guid.Empty)
        {
            _jobs.EnqueueQueueTurnNotify(result.Value.RoomId.ToString(), _tenant.TenantId, command.TicketId.ToString());
        }

        return result;
    }
}

public class SkipTicketCommandHandler : IRequestHandler<SkipTicketCommand, Result<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public SkipTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ReceptionTicketResponse>> Handle(SkipTicketCommand command, CancellationToken cancellationToken)
        => await TicketTransitionHelper.TransitionTicket(_db, _tenant, command.TicketId, TicketStatus.Skipped, cancellationToken);
}

public class CancelTicketCommandHandler : IRequestHandler<CancelTicketCommand, Result<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public CancelTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ReceptionTicketResponse>> Handle(CancelTicketCommand command, CancellationToken cancellationToken)
        => await TicketTransitionHelper.TransitionTicket(_db, _tenant, command.TicketId, TicketStatus.Cancelled, cancellationToken, command.Reason);
}

// Shared state machine transition
internal static class ReceptionTicketTransition
{
    // expose as module-level so handlers can call it
}

// Helper (static local func pattern not available cross-class — use extension method)
public static class TicketTransitionHelper
{
    public static async Task<Result<ReceptionTicketResponse>> TransitionTicket(
        IDapperConnectionFactory db,
        ITenantProvider tenant,
        Guid ticketId,
        string newStatus,
        CancellationToken ct,
        string? cancelReason = null)
    {
        using var conn = db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT id, status, patient_id, room_id, doctor_id, ticket_no, note, created_at FROM diab_his_rcp_queue_tickets WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = ticketId.ToString(), TenantId = tenant.TenantId });

        if (row is null)
            return Result<ReceptionTicketResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy phiếu tiếp đón");

        var currentStatus = (string)row.status;
        if (!TicketStatus.CanTransition(currentStatus, newStatus))
            return Result<ReceptionTicketResponse>.Failure("RECEPTION_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái từ {currentStatus} sang {newStatus}");

        var now = DateTime.UtcNow;
        var calledAt = newStatus == TicketStatus.Called ? now : (DateTime?)null;
        var startedAt = newStatus == TicketStatus.InProgress ? now : (DateTime?)null;
        var finishedAt = newStatus == TicketStatus.Done ? now : (DateTime?)null;

        await conn.ExecuteAsync(
            "UPDATE diab_his_rcp_queue_tickets SET status=@Status, note=COALESCE(@CancelReason, note), updated_at=@Now WHERE id=@Id",
            new { Status = newStatus, CancelReason = cancelReason, Now = now, Id = ticketId.ToString() });

        // Fetch room name
        var roomName = await conn.ExecuteScalarAsync<string>(
            "SELECT name FROM diab_his_sys_rooms WHERE id=@Id", new { Id = (string)row.room_id });

        // Fetch doctor name
        string? doctorName = null;
        if (row.doctor_id is not null)
        {
            var docFullName = await conn.ExecuteScalarAsync<string>(
                "SELECT full_name FROM sec_users WHERE id=@Id", new { Id = (string)row.doctor_id });
            doctorName = docFullName is not null ? $"BS. {docFullName}" : null;
        }

        var patId2 = Guid.Parse(((object)row.patient_id).ToString()!);
        var roomId2 = row.room_id != null ? Guid.Parse(((object)row.room_id).ToString()!) : (Guid?)null;
        return Result<ReceptionTicketResponse>.Success(new ReceptionTicketResponse(
            ticketId,
            tenant.TenantId,
            patId2,
            null,
            (string?)row.ticket_no ?? "000",
            roomId2 ?? Guid.Empty,
            roomName,
            row.doctor_id is not null ? Guid.Parse(((object)row.doctor_id).ToString()!) : null,
            doctorName,
            new List<ServicePackageDto>(),
            null,
            newStatus,
            "NORMAL",
            (DateTime)row.created_at,
            null, null, null, null,
            (string?)row.note));
    }
}

public class ListQueueQueryHandler : IRequestHandler<ListQueueQuery, List<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListQueueQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<ReceptionTicketResponse>> Handle(ListQueueQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var date = (request.Date ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd");

        // diab_his_rcp_queue_tickets: id, tenant_id, patient_id, ticket_no, ticket_date, room_id, status, note, created_at
        var where = "WHERE t.tenant_id=@TenantId AND t.ticket_date=@Date AND t.deleted_at IS NULL";
        if (request.RoomId.HasValue) where += " AND t.room_id=@RoomId";
        if (!string.IsNullOrEmpty(request.Status)) where += " AND t.status=@Status";

        var sql = $@"
            SELECT t.id, t.tenant_id, t.patient_id, p.code, p.full_name, p.gender, p.date_of_birth,
                   t.ticket_no as ticket_no, t.room_id, r.name as room_name,
                   t.doctor_id as doctor_id, u.full_name as doctor_full_name,
                   t.reason_for_visit as reason_for_visit, t.note, t.status, t.priority as priority,
                   t.checked_in_at as checked_in_at, t.called_at as called_at, t.started_at as started_at,
                   t.finished_at as finished_at, t.created_by as created_by
            FROM diab_his_rcp_queue_tickets t
            LEFT JOIN pat_patients p ON t.patient_id = p.id
            LEFT JOIN diab_his_sys_rooms r ON t.room_id = r.id
            LEFT JOIN sec_users u ON t.doctor_id = u.id
            {where}
            ORDER BY t.ticket_no ASC";

        var rows = await conn.QueryAsync(sql, new
        {
            TenantId = _tenant.TenantId,
            Date = date,
            RoomId = request.RoomId?.ToString(),
            Status = request.Status
        });

        return rows.Select(MapTicketRow).ToList();
    }

    internal static ReceptionTicketResponse MapTicketRow(dynamic r)
    {
        PatientSummaryDto? ps = null;
        if (r.code is not null)
        {
            DateOnly? dob = r.date_of_birth is DateTime d ? DateOnly.FromDateTime(d) : null;
            ps = new PatientSummaryDto(
                Guid.Parse(((object)r.patient_id).ToString()!),
                (string)r.code, (string)r.full_name, dob, (string?)r.gender, null);
        }

        return new ReceptionTicketResponse(
            Guid.Parse(((object)r.id).ToString()!),
            r.tenant_id is not null ? (int?)r.tenant_id : null,
            Guid.Parse(((object)r.patient_id).ToString()!),
            ps,
            (string)r.ticket_no,
            Guid.Parse(((object)r.room_id).ToString()!),
            (string?)r.room_name,
            r.doctor_id is not null ? Guid.Parse(((object)r.doctor_id).ToString()!) : null,
            r.doctor_full_name is not null ? $"BS. {(string)r.doctor_full_name}" : null,
            new List<ServicePackageDto>(),
            (string?)r.reason_for_visit,
            (string)r.status,
            (string)r.priority,
            (DateTime)r.checked_in_at,
            r.called_at is DateTime ca ? ca : null,
            r.started_at is DateTime sa ? sa : null,
            r.finished_at is DateTime fa ? fa : null,
            r.created_by is not null ? Guid.Parse(((object)r.created_by).ToString()!) : null,
            (string?)r.note);
    }
}

public class ListRoomsQueryHandler : IRequestHandler<ListRoomsQuery, List<RoomResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListRoomsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<List<RoomResponse>> Handle(ListRoomsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var sql = @"
            SELECT r.id, r.name, r.code AS room_code, r.capacity AS max_per_day,
                   NULL AS doctor_id, NULL AS doctor_name,
                   COUNT(CASE WHEN t.status='WAITING' THEN 1 END) as waiting_count
            FROM diab_his_sys_rooms r
            LEFT JOIN diab_his_rcp_queue_tickets t ON t.room_id=r.id AND DATE(t.created_at)=@Today AND t.deleted_at IS NULL
            WHERE (r.tenant_id=@TenantId OR r.tenant_id IS NULL) AND r.deleted_at IS NULL AND r.is_active=1
            GROUP BY r.id, r.name, r.code, r.capacity";

        var rows = await conn.QueryAsync(sql, new { TenantId = _tenant.TenantId, Today = today });
        return rows.Select(r => new RoomResponse(
            ((object)r.id).ToString()!,
            (string)r.name,
            (string)r.room_code,
            r.doctor_id is not null ? new DoctorOnDutyDto(
                ((object)r.doctor_id).ToString()!,
                (string)r.doctor_name) : null,
            (int)r.max_per_day,
            r.waiting_count == null ? 0 : (int)(long)r.waiting_count
        )).ToList();
    }
}

public class GetReceptionStatsQueryHandler : IRequestHandler<GetReceptionStatsQuery, ReceptionStatsDto>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetReceptionStatsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ReceptionStatsDto> Handle(GetReceptionStatsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var date = (request.Date ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd");

        var sql = @"
            SELECT
                COUNT(*) as total,
                SUM(CASE WHEN status='WAITING' THEN 1 ELSE 0 END) as waiting,
                SUM(CASE WHEN status='IN_PROGRESS' THEN 1 ELSE 0 END) as in_progress,
                SUM(CASE WHEN status='DONE' THEN 1 ELSE 0 END) as done,
                SUM(CASE WHEN status='SKIPPED' THEN 1 ELSE 0 END) as skipped,
                SUM(CASE WHEN status='CANCELLED' THEN 1 ELSE 0 END) as cancelled,
                0 as avg_wait
            FROM diab_his_rcp_queue_tickets
            WHERE tenant_id=@TenantId AND DATE(created_at)=@Date AND deleted_at IS NULL";

        var row = await conn.QueryFirstAsync(sql, new { TenantId = _tenant.TenantId, Date = date });
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.Today);

        return new ReceptionStatsDto(
            targetDate,
            (int)(row.total ?? 0),
            (int)(row.waiting ?? 0),
            (int)(row.in_progress ?? 0),
            (int)(row.done ?? 0),
            (int)(row.skipped ?? 0),
            (int)(row.cancelled ?? 0),
            row.avg_wait is not null ? Math.Round((double)row.avg_wait, 1) : 0);
    }
}

public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<ReceptionTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetTicketQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ReceptionTicketResponse>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT t.id, t.tenant_id, t.patient_id, p.code, p.full_name, p.gender, p.date_of_birth,
                   t.ticket_no as ticket_no, t.room_id, r.name as room_name,
                   t.doctor_id as doctor_id, u.full_name as doctor_full_name,
                   t.reason_for_visit as reason_for_visit, t.note, t.status, t.priority as priority,
                   t.checked_in_at as checked_in_at, t.called_at as called_at, t.started_at as started_at,
                   t.finished_at as finished_at, t.created_by as created_by
            FROM diab_his_rcp_queue_tickets t
            LEFT JOIN pat_patients p ON t.patient_id = p.id
            LEFT JOIN diab_his_sys_rooms r ON t.room_id = r.id
            LEFT JOIN sec_users u ON t.doctor_id = u.id
            WHERE t.id=@Id AND t.tenant_id=@TenantId AND t.deleted_at IS NULL";

        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = request.TicketId.ToString(), TenantId = _tenant.TenantId });
        if (row is null)
            return Result<ReceptionTicketResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy phiếu tiếp đón");

        return Result<ReceptionTicketResponse>.Success(ListQueueQueryHandler.MapTicketRow(row));
    }
}
