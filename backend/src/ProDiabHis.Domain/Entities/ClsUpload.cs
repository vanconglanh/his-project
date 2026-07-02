namespace ProDiabHis.Domain.Entities;

/// <summary>Metadata tai lieu CLS upload. Map bang diab_his_fil_cls_uploads</summary>
public class ClsUpload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? TenantId { get; set; }
    public int PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string DocType { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Note { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
