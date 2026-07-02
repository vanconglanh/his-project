namespace ProDiabHis.Domain.Entities.Bhyt;

/// <summary>File ket qua giam dinh BHYT upload tu cong BHYT</summary>
public class BhytReconcileUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public int ExportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ParsedAt { get; set; }
    public string ParseStatus { get; set; } = BhytReconcileParseStatus.Pending;
    public string? ParseError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public static class BhytReconcileParseStatus
{
    public const string Pending = "PENDING";
    public const string Parsing = "PARSING";
    public const string Parsed  = "PARSED";
    public const string Failed  = "FAILED";
}
