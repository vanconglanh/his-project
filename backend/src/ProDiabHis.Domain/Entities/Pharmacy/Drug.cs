using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Danh muc thuoc. Map bang diab_his_pha_drugs</summary>
public class Drug : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? BrandName { get; set; }
    public string? DrugForm { get; set; }
    public string? Strength { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? AtcCode { get; set; }
    public string? DrugCategory { get; set; }
    public bool IsControlled { get; set; }
    public bool IsAntibiotic { get; set; }
    public bool RequiresRx { get; set; } = true;
    public decimal SellPrice { get; set; }
    public decimal? BhytPrice { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}
