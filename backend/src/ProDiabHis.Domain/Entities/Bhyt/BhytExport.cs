using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Bhyt;

/// <summary>Ky xuat ho so BHYT theo QD 4750/QD-BYT (1 ban ghi = 1 ky thang)</summary>
public class BhytExport : ISoftDelete
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string PeriodMonth { get; set; } = string.Empty;         // YYYY-MM
    public string? ScopeFilterJson { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = BhytExportStatus.Draft;
    public int EncounterCount { get; set; }
    public decimal TotalRequestedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public decimal TotalRejectedAmount { get; set; }
    public string? XmlFilePath { get; set; }
    public string? ResponseMessage { get; set; }
    public string? BhytReference { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ResponseAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public static class BhytExportStatus
{
    public const string Draft              = "DRAFT";
    public const string Generated          = "GENERATED";
    public const string Validated          = "VALIDATED";
    public const string Signed             = "SIGNED";
    public const string Submitted          = "SUBMITTED";
    public const string Approved           = "APPROVED";
    public const string PartiallyRejected  = "PARTIALLY_REJECTED";
    public const string Rejected           = "REJECTED";

    /// <summary>Cac trang thai khong the thay doi (sau khi da SUBMITTED)</summary>
    public static readonly IReadOnlySet<string> LockedStatuses = new HashSet<string>
    {
        Submitted, Approved, PartiallyRejected, Rejected
    };

    public static bool IsLocked(string status) => LockedStatuses.Contains(status);
}
