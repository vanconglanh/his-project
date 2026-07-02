using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Phieu cap phat thuoc. Map bang diab_his_pha_dispenses</summary>
public class Dispense : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid DispensedBy { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public string ItemsJson { get; set; } = "[]";
    public string? Note { get; set; }
}
