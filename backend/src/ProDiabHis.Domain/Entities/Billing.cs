using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Hoa don. Map bang diab_his_bil_billing</summary>
public class Billing : BaseEntity
{
    public int TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string? BillNo { get; set; }
    public string Payer { get; set; } = "SELF";
    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal BhytAmount { get; set; }
    public decimal PatientPayable { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = BillingStatus.Draft;
    public string? RightRoute { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public string? Note { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public ICollection<BillingItem> Items { get; set; } = new List<BillingItem>();
}

public static class BillingStatus
{
    public const string Draft = "DRAFT";
    public const string Finalized = "FINALIZED";
    public const string PartialPaid = "PARTIAL_PAID";
    public const string Paid = "PAID";
    public const string Void = "VOID";
}

/// <summary>Dong trong hoa don. Map bang diab_his_bil_billing_items</summary>
public class BillingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BillingId { get; set; }
    public int TenantId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public Guid? RefId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
    public bool BhytApplicable { get; set; }
    public decimal BhytAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Billing? Billing { get; set; }
}
