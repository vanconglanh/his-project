using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Lab order. Maps diab_his_cli_lab_orders</summary>
public class LabOrder : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? SampleType { get; set; }
    public string Priority { get; set; } = ClsPriority.Normal;
    public string Status { get; set; } = LabOrderStatus.Ordered;
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public string? OrderedBy { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? LabPartnerId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Rad order. Maps diab_his_cli_rad_orders</summary>
public class RadOrder : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string? BodyPart { get; set; }
    public bool Contrast { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string Priority { get; set; } = ClsPriority.Normal;
    public string Status { get; set; } = RadOrderStatus.Ordered;
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public string? OrderedBy { get; set; }
    public string? Note { get; set; }
}

public static class LabOrderStatus
{
    public const string Ordered     = "ordered";
    public const string SampleTaken = "sample_taken";
    public const string Processing  = "processing";
    public const string Done        = "done";
    public const string Cancelled   = "cancelled";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Ordered]     = new[] { SampleTaken, Cancelled },
        [SampleTaken] = new[] { Processing },
        [Processing]  = new[] { Done },
        [Done]        = Array.Empty<string>(),
        [Cancelled]   = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

public static class RadOrderStatus
{
    public const string Ordered    = "ordered";
    public const string Scheduled  = "scheduled";
    public const string InProgress = "in_progress";
    public const string Done       = "done";
    public const string Cancelled  = "cancelled";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Ordered]    = new[] { Scheduled, Cancelled },
        [Scheduled]  = new[] { InProgress, Cancelled },
        [InProgress] = new[] { Done, Cancelled },
        [Done]       = Array.Empty<string>(),
        [Cancelled]  = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

public static class ClsPriority
{
    public const string Normal = "NORMAL";
    public const string Urgent = "URGENT";
    public const string Stat   = "STAT";
}
