using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Reception.Reassign;

/// <summary>
/// [G05] Dieu phoi luot kham: doi bac si / doi phong / chuyen phong giua ca.
/// GIU NGUYEN ticket_no + ticket_date + id ve (khong huy-tao-lai) de thong ke cong bac si dung.
/// Ghi 1 dong lich su vao diab_his_rcp_ticket_reassignments + audit action REASSIGN.
/// </summary>
public class ReassignTicketCommandHandler : IRequestHandler<ReassignTicketCommand, Result<ReassignTicketResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IDoctorDutyChecker _duty;

    public ReassignTicketCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, IDoctorDutyChecker duty)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; _duty = duty; }

    public async Task<Result<ReassignTicketResponse>> Handle(ReassignTicketCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var tenantId = _tenant.TenantId;

        if (req is null || string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < 5)
            return Result<ReassignTicketResponse>.Failure("TICKET_REASSIGN_REASON_REQUIRED",
                "Bắt buộc nhập lý do điều phối");

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        // 1) Khoa ve trong transaction (chong 2 le tan dieu phoi cung luc)
        var ticket = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(
            @"SELECT id, status, patient_id, room_id, doctor_id, ticket_no, ticket_date, reassign_count
                FROM diab_his_rcp_queue_tickets
               WHERE id = @Id AND tenant_id = @Tid AND deleted_at IS NULL
               FOR UPDATE",
            new { Id = cmd.TicketId.ToString(), Tid = tenantId }, tx, cancellationToken: ct));

        if (ticket is null)
            return Result<ReassignTicketResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy phiếu tiếp đón");

        var status = (string)ticket.status;
        var currentDoctor = ticket.doctor_id is null ? null : ((object)ticket.doctor_id).ToString();
        var currentRoom = ticket.room_id is null ? null : ((object)ticket.room_id).ToString();
        var patientId = ((object)ticket.patient_id).ToString()!;
        var ticketNo = (string?)ticket.ticket_no ?? "000";
        var reassignCount = ticket.reassign_count is null ? 0 : Convert.ToInt32(ticket.reassign_count);

        var newDoctor = req.DoctorId?.ToString();
        var newRoom = req.RoomId?.ToString();
        var changingDoctor = newDoctor is not null && !EqualsId(newDoctor, currentDoctor);
        var changingRoom = newRoom is not null && !EqualsId(newRoom, currentRoom);

        // 2) Ma tran quyen dieu phoi theo trang thai ve
        var policy = TicketReassignPolicy.Check(status, changingDoctor, changingRoom);
        if (!policy.Allowed)
            return Result<ReassignTicketResponse>.Failure(policy.ErrorCode!, policy.ErrorMessage!,
                new { ticketId = cmd.TicketId, status });

        // 3) Dang trong ca kham -> chi BS chu ca hoac admin duoc chuyen phong
        if (TicketReassignPolicy.IsInSession(status) && !IsAdmin())
        {
            var me = _user.UserId?.ToString();
            if (currentDoctor is not null && (me is null || !EqualsId(me, currentDoctor)))
                return Result<ReassignTicketResponse>.Failure("TICKET_REASSIGN_NOT_OWNER",
                    "Chỉ bác sĩ đang khám ca này được chuyển phòng");
        }

        var warnings = new List<ReassignWarningDto>();

        // 4) Validate phong dich
        dynamic? room = null;
        if (changingRoom)
        {
            room = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(
                @"SELECT id, code, name, capacity FROM diab_his_sys_rooms
                   WHERE id = @Id AND tenant_id = @Tid AND deleted_at IS NULL",
                new { Id = newRoom, Tid = tenantId }, tx, cancellationToken: ct));
            if (room is null)
                return Result<ReassignTicketResponse>.Failure("ROOM_NOT_FOUND", "Không tìm thấy phòng khám");

            // Canh bao qua tai phong (KHONG chan — benh nhan da trong hang doi)
            var ticketDateStr = ticket.ticket_date is DateTime td ? td.ToString("yyyy-MM-dd") : (string?)ticket.ticket_date;
            var occupied = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(*) FROM diab_his_rcp_queue_tickets
                   WHERE tenant_id = @Tid AND room_id = @RoomId AND ticket_date = @Date
                     AND status NOT IN ('CANCELLED','WAITING_CLS','DONE','SKIPPED')
                     AND deleted_at IS NULL AND id <> @Id",
                new { Tid = tenantId, RoomId = newRoom, Date = ticketDateStr, Id = cmd.TicketId.ToString() },
                tx, cancellationToken: ct));
            var capacity = Convert.ToInt32(room.capacity);
            if (occupied >= capacity)
                warnings.Add(new ReassignWarningDto("ROOM_OVER_CAPACITY",
                    $"Phòng {room.name} đã đạt giới hạn ({occupied}/{capacity}) — vẫn tiếp tục điều phối"));
        }

        // 5) Validate bac si dich + canh bao lich truc
        dynamic? doctor = null;
        var scheduleWarning = false;
        if (changingDoctor)
        {
            doctor = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(
                @"SELECT u.id, u.full_name
                    FROM diab_his_sec_users u
                    JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id AND ur.tenant_id = @Tid
                    JOIN diab_his_sec_roles r ON r.id = ur.role_id AND r.code = 'bac_si'
                   WHERE u.id = @Id AND u.tenant_id = @Tid AND u.deleted_at IS NULL
                   LIMIT 1",
                new { Id = newDoctor, Tid = tenantId }, tx, cancellationToken: ct));
            if (doctor is null)
                return Result<ReassignTicketResponse>.Failure("DOCTOR_NOT_FOUND", "Không tìm thấy bác sĩ");

            var duty = await SafeCheckDutyAsync(tenantId, req.DoctorId!.Value, ct);
            if (duty is not null)
            {
                var name = (string?)doctor.full_name ?? "";
                if (!duty.OnDuty)
                {
                    scheduleWarning = true;
                    warnings.Add(new ReassignWarningDto("DOCTOR_NOT_ON_DUTY",
                        $"Bác sĩ {name} không có lịch trực trong khung giờ này ({duty.LocalTimeLabel})"));
                }
                if (duty.Blocked)
                {
                    scheduleWarning = true;
                    warnings.Add(new ReassignWarningDto("DOCTOR_ON_LEAVE_BLOCK",
                        $"Bác sĩ {name} đang nghỉ/bận theo lịch ({duty.LocalTimeLabel})"));
                }
            }
        }

        var now = DateTime.UtcNow;
        var targetDoctor = changingDoctor ? newDoctor : currentDoctor;
        var targetRoom = changingRoom ? newRoom : currentRoom;
        var uid = _user.UserId?.ToString();

        // 6) Cap nhat ve — GIU NGUYEN ticket_no / ticket_date / id
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE diab_his_rcp_queue_tickets
                 SET doctor_id = @DoctorId,
                     room_id   = @RoomId,
                     reassign_count = reassign_count + 1,
                     updated_at = @Now, updated_by = @Uid
               WHERE id = @Id AND tenant_id = @Tid",
            new { DoctorId = targetDoctor, RoomId = targetRoom, Now = now, Uid = uid,
                  Id = cmd.TicketId.ToString(), Tid = tenantId }, tx, cancellationToken: ct));

        // 7) Dong bo sang luot kham dang mo cua benh nhan (neu da admit)
        var enc = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(
            @"SELECT id, status FROM diab_his_enc_encounters
               WHERE tenant_id = @Tid AND patient_id = @Pid AND deleted_at IS NULL
                 AND status IN ('WAITING','IN_PROGRESS')
               ORDER BY created_at DESC LIMIT 1",
            new { Tid = tenantId, Pid = patientId }, tx, cancellationToken: ct));

        string? encounterId = null;
        if (enc is not null)
        {
            encounterId = ((object)enc.id).ToString();
            await conn.ExecuteAsync(new CommandDefinition(
                @"UPDATE diab_his_enc_encounters
                     SET doctor_id = @DoctorId, room_id = @RoomId, updated_at = @Now, updated_by = @Uid
                   WHERE id = @Id AND tenant_id = @Tid AND status IN ('WAITING','IN_PROGRESS')",
                new { DoctorId = targetDoctor, RoomId = targetRoom, Now = now, Uid = uid,
                      Id = encounterId, Tid = tenantId }, tx, cancellationToken: ct));
            warnings.Add(new ReassignWarningDto("ENCOUNTER_SYNCED",
                "Đã đồng bộ bác sĩ/phòng sang lượt khám đang mở"));
        }

        // 8) Ghi lich su dieu phoi
        var changeType = ReassignChangeType.From(changingDoctor, changingRoom);
        var reassignId = Guid.NewGuid();
        var warningMessage = warnings.Count == 0 ? null : string.Join(" | ", warnings.Select(w => w.Message));

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO diab_his_rcp_ticket_reassignments
                (id, tenant_id, ticket_id, encounter_id, from_doctor_id, to_doctor_id,
                 from_room_id, to_room_id, change_type, ticket_status_at_change, reason,
                 schedule_warning_flag, warning_message, acknowledged_warning,
                 changed_at, changed_by, created_at, created_by, updated_at)
              VALUES (@Id, @Tid, @TicketId, @EncounterId, @FromDoctor, @ToDoctor,
                      @FromRoom, @ToRoom, @ChangeType, @StatusAtChange, @Reason,
                      @WarnFlag, @WarnMsg, @Ack, @Now, @Uid, @Now, @Uid, @Now)",
            new
            {
                Id = reassignId.ToString(), Tid = tenantId, TicketId = cmd.TicketId.ToString(),
                EncounterId = encounterId, FromDoctor = currentDoctor, ToDoctor = targetDoctor,
                FromRoom = currentRoom, ToRoom = targetRoom, ChangeType = changeType,
                StatusAtChange = status, Reason = req.Reason.Trim(),
                WarnFlag = scheduleWarning ? 1 : 0, WarnMsg = warningMessage,
                Ack = req.AcknowledgeScheduleWarning ? 1 : 0, Now = now, Uid = uid
            }, tx, cancellationToken: ct));

        tx.Commit();

        await _audit.LogAsync("REASSIGN", "ReceptionTicket", cmd.TicketId.ToString(),
            AuditSeverity.WARN, false, null,
            new
            {
                ticketNo,
                statusAtChange = status,
                changeType,
                fromDoctorId = currentDoctor,
                toDoctorId = targetDoctor,
                fromRoomId = currentRoom,
                toRoomId = targetRoom,
                encounterId,
                reason = req.Reason.Trim(),
                warnings = warnings.Select(w => w.Code).ToArray()
            }, ct);

        // 9) Resolve ten hien thi cho response
        var doctorName = doctor is not null
            ? (string?)doctor.full_name
            : await GetDoctorNameAsync(conn, tenantId, targetDoctor, ct);

        ReassignRoomDto? roomDto = null;
        if (targetRoom is not null)
        {
            if (room is not null)
                roomDto = new ReassignRoomDto(Guid.Parse(targetRoom), (string?)room.code, (string?)room.name);
            else
            {
                var r = await conn.QueryFirstOrDefaultAsync(new CommandDefinition(
                    "SELECT code, name FROM diab_his_sys_rooms WHERE id=@Id AND tenant_id=@Tid",
                    new { Id = targetRoom, Tid = tenantId }, cancellationToken: ct));
                roomDto = new ReassignRoomDto(Guid.Parse(targetRoom),
                    r is null ? null : (string?)r.code,
                    r is null ? null : (string?)r.name);
            }
        }

        DateOnly? ticketDateOut = ticket.ticket_date is DateTime dOut ? DateOnly.FromDateTime(dOut) : null;

        return Result<ReassignTicketResponse>.Success(new ReassignTicketResponse(
            cmd.TicketId,
            ticketNo,
            ticketDateOut,
            status,
            encounterId is null ? (Guid?)null : Guid.Parse(encounterId),
            targetDoctor is null ? null : new ReassignPersonDto(Guid.Parse(targetDoctor), doctorName),
            roomDto,
            reassignCount + 1,
            changeType,
            reassignId,
            now,
            warnings));
    }

    private async Task<DoctorDutyStatus?> SafeCheckDutyAsync(int tenantId, Guid doctorId, CancellationToken ct)
    {
        // Canh bao lich truc chi la thong tin phu — khong duoc lam fail nghiep vu dieu phoi.
        try { return await _duty.CheckAsync(tenantId, doctorId, DateTime.UtcNow, ct); }
        catch { return null; }
    }

    private static async Task<string?> GetDoctorNameAsync(IDbConnection conn, int tenantId, string? doctorId, CancellationToken ct)
    {
        if (doctorId is null) return null;
        return await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT full_name FROM diab_his_sec_users WHERE id=@Id AND tenant_id=@Tid",
            new { Id = doctorId, Tid = tenantId }, cancellationToken: ct));
    }

    private bool IsAdmin() =>
        _user.Roles.Any(r =>
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quan tri", StringComparison.OrdinalIgnoreCase))
        || _user.RoleCodes.Any(c => c.Equals("admin", StringComparison.OrdinalIgnoreCase));

    private static bool EqualsId(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>[G05] Lich su dieu phoi cua 1 ve (changed_at ASC).</summary>
public class ListTicketReassignmentsQueryHandler
    : IRequestHandler<ListTicketReassignmentsQuery, Result<List<TicketReassignmentHistoryDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListTicketReassignmentsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<List<TicketReassignmentHistoryDto>>> Handle(ListTicketReassignmentsQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        using var conn = _db.CreateConnection();

        var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM diab_his_rcp_queue_tickets WHERE id=@Id AND tenant_id=@Tid AND deleted_at IS NULL",
            new { Id = q.TicketId.ToString(), Tid = tenantId }, cancellationToken: ct));
        if (exists == 0)
            return Result<List<TicketReassignmentHistoryDto>>.Failure("TICKET_NOT_FOUND", "Không tìm thấy phiếu tiếp đón");

        var rows = await conn.QueryAsync(new CommandDefinition(
            @"SELECT ra.id, ra.ticket_id, ra.encounter_id, ra.change_type, ra.ticket_status_at_change,
                     ra.from_doctor_id, fd.full_name AS from_doctor_name,
                     ra.to_doctor_id,   td.full_name AS to_doctor_name,
                     ra.from_room_id,   fr.name AS from_room_name,
                     ra.to_room_id,     tr.name AS to_room_name,
                     ra.reason, ra.schedule_warning_flag, ra.warning_message,
                     ra.changed_at, ra.changed_by, cu.full_name AS changed_by_name
                FROM diab_his_rcp_ticket_reassignments ra
                LEFT JOIN diab_his_sec_users fd ON fd.id = ra.from_doctor_id
                LEFT JOIN diab_his_sec_users td ON td.id = ra.to_doctor_id
                LEFT JOIN diab_his_sys_rooms fr ON fr.id = ra.from_room_id
                LEFT JOIN diab_his_sys_rooms tr ON tr.id = ra.to_room_id
                LEFT JOIN diab_his_sec_users cu ON cu.id = ra.changed_by
               WHERE ra.tenant_id = @Tid AND ra.ticket_id = @Id AND ra.deleted_at IS NULL
               ORDER BY ra.changed_at ASC",
            new { Tid = tenantId, Id = q.TicketId.ToString() }, cancellationToken: ct));

        var list = rows.Select(r => new TicketReassignmentHistoryDto(
            ParseGuid(r.id) ?? Guid.Empty,
            ParseGuid(r.ticket_id) ?? Guid.Empty,
            ParseGuid(r.encounter_id),
            (string)r.change_type,
            (string)r.ticket_status_at_change,
            ParseGuid(r.from_doctor_id), (string?)r.from_doctor_name,
            ParseGuid(r.to_doctor_id), (string?)r.to_doctor_name,
            ParseGuid(r.from_room_id), (string?)r.from_room_name,
            ParseGuid(r.to_room_id), (string?)r.to_room_name,
            (string)r.reason,
            Convert.ToInt32(r.schedule_warning_flag) == 1,
            (string?)r.warning_message,
            (DateTime)r.changed_at,
            ParseGuid(r.changed_by), (string?)r.changed_by_name)).ToList();

        return Result<List<TicketReassignmentHistoryDto>>.Success(list);
    }

    private static Guid? ParseGuid(object? v)
    {
        var s = v?.ToString();
        return string.IsNullOrEmpty(s) ? null : Guid.Parse(s);
    }
}
