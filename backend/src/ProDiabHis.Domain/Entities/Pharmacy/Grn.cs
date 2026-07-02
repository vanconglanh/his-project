using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Phieu nhap kho (Goods Receipt Note). Map bang diab_his_pha_grn</summary>
public class Grn : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string GrnNo { get; set; } = string.Empty;
    public Guid? PoId { get; set; }
    public Guid SupplierId { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal TotalAmount { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string? Note { get; set; }
}
