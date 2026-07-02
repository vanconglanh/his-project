using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Lượt khám bệnh. Maps table cli_visits</summary>
public class Encounter : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string? RoomId { get; set; }
    public string EncounterType { get; set; } = EncounterTypes.FirstVisit;
    public string Status { get; set; } = EncounterStatus.Waiting;
    public string? ReasonForVisit { get; set; }
    public string? ChiefComplaint { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? AlertSentAt { get; set; }
}

public static class EncounterStatus
{
    public const string Waiting = "WAITING";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    public const string Cancelled = "CANCELLED";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Waiting]    = new[] { InProgress, Cancelled },
        [InProgress] = new[] { Done, Cancelled },
        [Done]       = Array.Empty<string>(),
        [Cancelled]  = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

public static class EncounterTypes
{
    public const string FirstVisit    = "FIRST_VISIT";
    public const string FollowUp      = "FOLLOW_UP";
    public const string Emergency     = "EMERGENCY";
    public const string Consultation  = "CONSULTATION";
}
