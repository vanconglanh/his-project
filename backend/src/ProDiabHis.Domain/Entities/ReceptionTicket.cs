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
    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    public DateTime? CalledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public static class TicketStatus
{
    public const string Waiting = "WAITING";
    public const string Called = "CALLED";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    public const string Skipped = "SKIPPED";
    public const string Cancelled = "CANCELLED";

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
