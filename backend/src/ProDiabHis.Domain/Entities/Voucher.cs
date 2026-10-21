using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Nhom ap dung cua voucher (BM-95).</summary>
public static class VoucherScope
{
    public const string Exam = "EXAM";
    public const string Lab = "LAB";
    public const string Imaging = "IMAGING";
    public const string Drug = "DRUG";
    public const string All = "ALL";

    public static readonly string[] Valid = { Exam, Lab, Imaging, Drug, All };
}

/// <summary>Voucher giam gia theo %. Map bang diab_his_bil_vouchers</summary>
public class Voucher : BaseEntity
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountPercent { get; set; }
    public string AppliesTo { get; set; } = VoucherScope.All;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Lich su dung voucher. Map bang diab_his_bil_voucher_usages</summary>
public class VoucherUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid VoucherId { get; set; }
    public Guid BillingId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public Guid? UsedBy { get; set; }
}
