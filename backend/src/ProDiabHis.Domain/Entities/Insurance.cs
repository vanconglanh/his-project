namespace ProDiabHis.Domain.Entities;

/// <summary>The BHYT / bao hiem. Map bang pat_insurance</summary>
public class Insurance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string Type { get; set; } = "BHYT";
    public string CardNoEnc { get; set; } = string.Empty;
    public string? CardNoMasked { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public string? HospitalCode { get; set; }
    public int? CoveragePercent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
