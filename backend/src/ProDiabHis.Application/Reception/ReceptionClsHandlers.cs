using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Reception;

// ────────────── DTO ──────────────
public record WaitClsTicketRequest(Guid? ClsRoundId, string? Note);

public record ResumeTicketRequest(Guid? RoomId);

public record TicketClsStatusResponse(
    Guid Id,
    string Status,
    string StatusLabel,
    Guid? RoomId,
    Guid? ReleasedRoomId,
    DateTime? WaitingClsAt);

// ────────────── Commands ──────────────
/// <summary>IN_PROGRESS -> WAITING_CLS: nha phong de goi benh nhan ke tiep</summary>
public record WaitClsTicketCommand(Guid TicketId, WaitClsTicketRequest Request)
    : IRequest<Result<TicketClsStatusResponse>>;

/// <summary>WAITING_CLS -> IN_PROGRESS: quay lai phong kham khi co ket qua CLS</summary>
public record ResumeTicketCommand(Guid TicketId, ResumeTicketRequest? Request, bool Force)
    : IRequest<Result<TicketClsStatusResponse>>;

// ────────────────────────────────────────────────
// WAIT-CLS
// ────────────────────────────────────────────────
public class WaitClsTicketCommandHandler : IRequestHandler<WaitClsTicketCommand, Result<TicketClsStatusResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public WaitClsTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<TicketClsStatusResponse>> Handle(WaitClsTicketCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status, room_id FROM diab_his_rcp_queue_tickets WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.TicketId.ToString(), TId = tid });
        if (row is null)
            return Result<TicketClsStatusResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy vé khám");

        var current = (string)row.status;
        if (!TicketStatus.CanTransition(current, TicketStatus.WaitingCls))
            return Result<TicketClsStatusResponse>.Failure("TICKET_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái vé từ {current} sang {TicketStatus.WaitingCls}");

        var roomId = (string?)row.room_id;
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            UPDATE diab_his_rcp_queue_tickets
               SET status='WAITING_CLS', released_room_id=room_id, waiting_cls_at=@Now,
                   note=COALESCE(@Note, note), updated_at=@Now, updated_by=@Uid
             WHERE id=@Id AND tenant_id=@TId",
            new { Now = now, Note = cmd.Request?.Note, Uid = _user.UserId?.ToString(), Id = cmd.TicketId.ToString(), TId = tid });

        await _audit.LogAsync("WAIT_CLS", "ReceptionTicket", cmd.TicketId.ToString(),
            new { releasedRoomId = roomId, clsRoundId = cmd.Request?.ClsRoundId }, ct);

        return Result<TicketClsStatusResponse>.Success(new TicketClsStatusResponse(
            cmd.TicketId, TicketStatus.WaitingCls, "Chờ kết quả CLS",
            string.IsNullOrEmpty(roomId) ? null : Guid.Parse(roomId),
            string.IsNullOrEmpty(roomId) ? null : Guid.Parse(roomId),
            now));
    }
}

// ────────────────────────────────────────────────
// RESUME
// ────────────────────────────────────────────────
public class ResumeTicketCommandHandler : IRequestHandler<ResumeTicketCommand, Result<TicketClsStatusResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public ResumeTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<TicketClsStatusResponse>> Handle(ResumeTicketCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, status, room_id, released_room_id, ticket_date
              FROM diab_his_rcp_queue_tickets WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.TicketId.ToString(), TId = tid });
        if (row is null)
            return Result<TicketClsStatusResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy vé khám");

        var current = (string)row.status;
        if (!TicketStatus.CanTransition(current, TicketStatus.InProgress))
            return Result<TicketClsStatusResponse>.Failure("TICKET_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái vé từ {current} sang {TicketStatus.InProgress}");

        var targetRoom = cmd.Request?.RoomId?.ToString()
                         ?? (string?)row.released_room_id
                         ?? (string?)row.room_id;
        if (string.IsNullOrEmpty(targetRoom))
            return Result<TicketClsStatusResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        // Kiem tra suc chua phong (ve WAITING_CLS khong tinh vao suc chua)
        var room = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, capacity FROM diab_his_sys_rooms WHERE id=@Id AND (tenant_id=@TId OR tenant_id IS NULL) AND deleted_at IS NULL",
            new { Id = targetRoom, TId = tid });
        if (room is null)
            return Result<TicketClsStatusResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

        var ticketDate = row.ticket_date is DateTime d ? d.ToString("yyyy-MM-dd") : (string?)row.ticket_date;
        var occupied = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_rcp_queue_tickets
              WHERE tenant_id=@TId AND room_id=@RoomId AND ticket_date=@Date
                AND status NOT IN ('CANCELLED', 'WAITING_CLS') AND deleted_at IS NULL AND id<>@Id",
            new { TId = tid, RoomId = targetRoom, Date = ticketDate, Id = cmd.TicketId.ToString() });

        var capacity = Convert.ToInt32(room.capacity);
        if (occupied >= capacity && !cmd.Force)
            return Result<TicketClsStatusResponse>.Failure("ROOM_CAPACITY_EXCEEDED",
                "Phòng khám đã đạt giới hạn lượt khám tối đa",
                new { roomId = targetRoom, occupied, capacity });

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(@"
            UPDATE diab_his_rcp_queue_tickets
               SET status='IN_PROGRESS', room_id=@RoomId, released_room_id=NULL, waiting_cls_at=NULL,
                   started_at=COALESCE(started_at, @Now), updated_at=@Now, updated_by=@Uid
             WHERE id=@Id AND tenant_id=@TId",
            new { RoomId = targetRoom, Now = now, Uid = _user.UserId?.ToString(), Id = cmd.TicketId.ToString(), TId = tid });

        await _audit.LogAsync("RESUME_FROM_CLS", "ReceptionTicket", cmd.TicketId.ToString(),
            new { roomId = targetRoom, occupied, capacity, forced = cmd.Force && occupied >= capacity }, ct);

        return Result<TicketClsStatusResponse>.Success(new TicketClsStatusResponse(
            cmd.TicketId, TicketStatus.InProgress, "Đang khám",
            Guid.Parse(targetRoom), null, null));
    }
}
