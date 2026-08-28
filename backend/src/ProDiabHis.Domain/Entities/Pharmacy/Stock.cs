using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Ton kho thuoc theo lo. Map bang diab_his_pha_stock</summary>
public class Stock : BaseEntity, ITenantScoped, IBranchScoped
{
    public int TenantId { get; set; }
    /// <summary>R1: ton kho tach theo chi nhanh. NULL = du lieu truoc khi tach chi nhanh (giai doan migrate)</summary>
    public int? BranchId { get; set; }
    public Guid DrugId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? MfgDate { get; set; }
    public DateOnly ExpDate { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public string? Location { get; set; }
}
