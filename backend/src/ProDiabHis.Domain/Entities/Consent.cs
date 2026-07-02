namespace ProDiabHis.Domain.Entities;

/// <summary>Van ban dong y cua benh nhan. Map bang pat_consents</summary>
public class Consent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public string? SignedBy { get; set; }
    public Guid? DocumentFileId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
