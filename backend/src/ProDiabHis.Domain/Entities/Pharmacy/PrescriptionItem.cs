using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Chi tiet tung thuoc trong don thuoc. Map bang diab_his_pha_prescription_items</summary>
public class PrescriptionItem : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? DrugStrength { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string? Frequency { get; set; }
    public int? DurationDays { get; set; }
    public string? Route { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool BhytApplicable { get; set; }
    public string? Note { get; set; }

    public Prescription? Prescription { get; set; }
}
