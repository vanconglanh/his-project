namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Lich su dieu phoi luot kham (doi bac si / doi phong).
/// Map bang diab_his_rcp_ticket_reassignments. GIU NGUYEN ticket_no — khong huy/tao lai ve.
/// </summary>
public class TicketReassignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? FromDoctorId { get; set; }
    public Guid? ToDoctorId { get; set; }
    public Guid? FromRoomId { get; set; }
    public Guid? ToRoomId { get; set; }
    public string ChangeType { get; set; } = ReassignChangeType.Room;
    public string TicketStatusAtChange { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool ScheduleWarningFlag { get; set; }
    public string? WarningMessage { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid? ChangedBy { get; set; }
}

public static class ReassignChangeType
{
    public const string Doctor = "DOCTOR";
    public const string Room = "ROOM";
    public const string Both = "BOTH";

    public static string From(bool doctorChanged, bool roomChanged)
        => doctorChanged && roomChanged ? Both : (doctorChanged ? Doctor : Room);
}

/// <summary>Ket qua kiem tra quyen dieu phoi theo trang thai ve.</summary>
public record ReassignPolicyResult(bool Allowed, string? ErrorCode, string? ErrorMessage)
{
    public static readonly ReassignPolicyResult Ok = new(true, null, null);
}

/// <summary>
/// [G05] Ma tran quyen dieu phoi luot kham theo trang thai ve.
/// WAITING/CALLED  : doi ca bac si va phong.
/// IN_PROGRESS     : CHI doi phong (chuyen phong giua ca).
/// WAITING_CLS     : xu ly nhu IN_PROGRESS (benh nhan dang trong ca kham, cho ket qua CLS).
/// DONE/SKIPPED/CANCELLED : chan hoan toan.
/// </summary>
public static class TicketReassignPolicy
{
    /// <summary>Trang thai ket thuc — khong con dieu phoi duoc.</summary>
    public static bool IsTerminal(string? status)
        => status is TicketStatus.Done or TicketStatus.Skipped or TicketStatus.Cancelled;

    /// <summary>Trang thai dang trong ca kham — chi duoc chuyen phong.</summary>
    public static bool IsInSession(string? status)
        => status is TicketStatus.InProgress or TicketStatus.WaitingCls;

    public static bool CanChangeDoctor(string? status)
        => status is TicketStatus.Waiting or TicketStatus.Called;

    public static bool CanChangeRoom(string? status)
        => status is TicketStatus.Waiting or TicketStatus.Called
                  or TicketStatus.InProgress or TicketStatus.WaitingCls;

    /// <summary>Kiem tra 1 yeu cau dieu phoi co hop le voi trang thai ve hay khong.</summary>
    public static ReassignPolicyResult Check(string? status, bool changingDoctor, bool changingRoom)
    {
        if (IsTerminal(status))
            return new ReassignPolicyResult(false, "TICKET_REASSIGN_FORBIDDEN",
                "Lượt khám đã kết thúc — không thể điều phối");

        if (!changingDoctor && !changingRoom)
            return new ReassignPolicyResult(false, "TICKET_REASSIGN_NO_CHANGE",
                "Không có thay đổi nào để điều phối");

        if (changingDoctor && !CanChangeDoctor(status))
            return new ReassignPolicyResult(false, "TICKET_REASSIGN_DOCTOR_FORBIDDEN",
                "Đang khám — chỉ được chuyển phòng, không đổi bác sĩ");

        if (changingRoom && !CanChangeRoom(status))
            return new ReassignPolicyResult(false, "TICKET_REASSIGN_FORBIDDEN",
                "Lượt khám đã kết thúc — không thể điều phối");

        return ReassignPolicyResult.Ok;
    }
}
