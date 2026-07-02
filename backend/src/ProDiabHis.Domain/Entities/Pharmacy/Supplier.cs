using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Nha cung cap thuoc. Map bang diab_his_pha_suppliers</summary>
public class Supplier : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? DrugLicense { get; set; }
    public bool IsActive { get; set; } = true;
}
