namespace ProDiabHis.Domain.Entities;

/// <summary>Di ung cua benh nhan. Map bang pat_allergies</summary>
public class Allergy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateOnly? OnsetDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
