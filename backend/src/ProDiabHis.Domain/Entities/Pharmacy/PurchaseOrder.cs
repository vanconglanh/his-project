using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Don dat hang nhap duoc. Map bang diab_his_pha_purchase_orders</summary>
public class PurchaseOrder : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string PoNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
}

public static class PurchaseOrderStatus
{
    public const string Draft = "DRAFT";
    public const string Sent = "SENT";
    public const string Partial = "PARTIAL";
    public const string Received = "RECEIVED";
    public const string Cancelled = "CANCELLED";
}
