using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Dich vu / bang gia. Map bang diab_his_bil_services</summary>
public class BillingService : BaseEntity
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int VatRate { get; set; }
    public string? BhytCode { get; set; }
    public decimal? BhytMaxAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Goi kham. Map bang diab_his_bil_service_packages</summary>
public class ServicePackage : BaseEntity
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ServicePackageItem> Items { get; set; } = new List<ServicePackageItem>();
}

/// <summary>Item trong goi kham. Map bang diab_his_bil_service_package_items</summary>
public class ServicePackageItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PackageId { get; set; }
    public Guid ServiceId { get; set; }
    public int Quantity { get; set; } = 1;

    public ServicePackage? Package { get; set; }
    public BillingService? Service { get; set; }
}
