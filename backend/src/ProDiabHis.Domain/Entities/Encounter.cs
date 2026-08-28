using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Lượt khám bệnh. Maps table cli_visits</summary>
public class Encounter : BaseEntity, ITenantScoped, IBranchScoped
{
    public int TenantId { get; set; }
    public int? BranchId { get; set; }
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

    // ── G03: khoa benh an sau khi ket thuc kham ──
    /// <summary>Thoi diem benh an bi khoa (set khi dong ca / huy ca).</summary>
    public DateTime? LockedAt { get; set; }
    /// <summary>Nguoi thao tac lam khoa benh an.</summary>
    public Guid? LockedBy { get; set; }
    /// <summary>So lan da dinh chinh (denormalize de list nhanh).</summary>
    public int AmendmentCount { get; set; }

    /// <summary>Benh an da khoa (DONE hoac CANCELLED) — moi du lieu lam sang READ-ONLY.</summary>
    public bool IsLocked => EncounterStatus.IsLockedStatus(Status);

    /// <summary>FR-803: khac null = luot kham phat sinh tu phien tu van tu xa Docosan (FK mem -> diab_his_tel_sessions.id)</summary>
    public string? TelehealthSessionId { get; set; }
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

    /// <summary>Trang thai terminal — benh an bi khoa, chi sua qua ADDENDUM.</summary>
    public static bool IsLockedStatus(string? status)
        => status == Done || status == Cancelled;

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
