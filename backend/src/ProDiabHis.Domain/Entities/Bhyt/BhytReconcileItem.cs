namespace ProDiabHis.Domain.Entities.Bhyt;

/// <summary>Chi tiet 1 dong ket qua doi soat giam dinh BHYT</summary>
public class BhytReconcileItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid UploadId { get; set; }
    public int ExportId { get; set; }
    public int? ExportItemId { get; set; }
    public int TableNo { get; set; }
    public string MaLienKet { get; set; } = string.Empty;
    public decimal RequestAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public string? RejectionCode { get; set; }
    public string? RejectionReason { get; set; }
    public string Status { get; set; } = BhytReconcileItemStatus.Approved;
    public string? DisputeReason { get; set; }
    public string? DisputeEvidencePath { get; set; }
    public DateTime? DisputedAt { get; set; }
    public Guid? DisputedBy { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedBy { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class BhytReconcileItemStatus
{
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Adjusted = "ADJUSTED";
    public const string Disputed = "DISPUTED";
    public const string Accepted = "ACCEPTED";
}
