namespace ProDiabHis.Domain.Entities;

/// <summary>Metadata file tren MinIO. Map bang fil_files</summary>
public class FileMeta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? TenantId { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Category { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
