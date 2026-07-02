namespace ProDiabHis.Domain.Entities.Bhyt;

/// <summary>1 dong du lieu trong Bang N cua ho so BHYT</summary>
public class BhytExportItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ExportId { get; set; }
    public int TableNo { get; set; }                 // 1-5
    public int RecordIndex { get; set; }
    public string? RowDataJson { get; set; }         // JSON cua dong du lieu (BhytTable1Row..5Row)
    public string? SourceEncounterId { get; set; }  // UUID encounter
    public string? SourceBillingId { get; set; }    // UUID billing
    public string? MaLienKet { get; set; }
    public decimal RequestAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? RejectionCode { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
