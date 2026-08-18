namespace ProDiabHis.Domain.Entities;

/// <summary>Queue ticket tiep don benh nhan. Map bang diab_his_rcp_queue_tickets</summary>
public class ReceptionTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid RoomId { get; set; }
    public Guid? DoctorId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public DateOnly TicketDate { get; set; }
    public string Status { get; set; } = TicketStatus.Waiting;
    public string Priority { get; set; } = TicketPriority.Normal;
    public string? ReasonForVisit { get; set; }
    public string? Note { get; set; }
    public string? CancelReason { get; set; }
    public string? ServicePackagesJson { get; set; }
    /// <summary>Phong da nha khi chuyen sang WAITING_CLS; dung de quay lai IN_PROGRESS</summary>
    public Guid? ReleasedRoomId { get; set; }
    /// <summary>Thoi diem chuyen sang cho ket qua CLS</summary>
    public DateTime? WaitingClsAt { get; set; }
    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public DateTime? CalledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ── G05: dieu phoi kham ──
    /// <summary>So lan ve nay da bi dieu phoi (doi bac si / doi phong)</summary>
    public int ReassignCount { get; set; }
    /// <summary>Bac si ket thuc ca — chot cong, set 1 lan khi ve chuyen sang DONE</summary>
    public Guid? FinishedByDoctorId { get; set; }
}

public static class TicketStatus
{
    public const string Waiting = "WAITING";
    public const string Called = "CALLED";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    public const string Skipped = "SKIPPED";
    public const string Cancelled = "CANCELLED";
    /// <summary>Cho ket qua CLS - benh nhan roi phong, phong duoc nha cho ca ke tiep</summary>
    public const string WaitingCls = "WAITING_CLS";

    /// <summary>Kiem tra transition hop le theo state machine</summary>
    public static bool CanTransition(string current, string next)
    {
        return (current, next) switch
        {
            (Waiting, Called) => true,
            (Waiting, Skipped) => true,
            (Waiting, Cancelled) => true,
            (Called, InProgress) => true,
            (Called, Cancelled) => true,
            (Called, Skipped) => true,
            (InProgress, Done) => true,
            (InProgress, Cancelled) => true,
            // G01/G02 - cho ket qua CLS (nha phong) va quay lai phong kham
            (InProgress, WaitingCls) => true,
            (WaitingCls, InProgress) => true,
            (WaitingCls, Done) => true,
            (WaitingCls, Skipped) => true,
            (WaitingCls, Cancelled) => true,
            _ => false
        };
    }
}

public static class TicketPriority
{
    public const string Normal = "NORMAL";
    public const string Priority = "PRIORITY";
    public const string Emergency = "EMERGENCY";
}
