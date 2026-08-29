using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Override gia dich vu theo pham vi BRANCH hoac GROUP (E/Dot3 - BR-70..BR-76).
/// Map bang diab_his_bil_service_branch_prices (migration 9152).
/// Logic resolve gia 3 tang (BRANCH override -> GROUP override -> gia goc TENANT
/// trong diab_his_bil_services) nam o IServicePriceResolver, KHONG lam o entity nay.
/// </summary>
public class ServiceBranchPrice : IAuditTimestamps
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid ServiceId { get; set; }

    /// <summary>BRANCH hoac GROUP - xem <see cref="PriceOverrideScope"/></summary>
    public string Scope { get; set; } = PriceOverrideScope.Branch;

    public int? BranchId { get; set; }
    public int? GroupId { get; set; }
    public decimal Price { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public static class PriceOverrideScope
{
    public const string Branch = "BRANCH";
    public const string Group = "GROUP";
}

/// <summary>Nguon gia da ap dung cho 1 dong hoa don - snapshot BR-73 (cot price_source).</summary>
public static class PriceSource
{
    public const string Tenant = "TENANT";
    public const string Group = "GROUP";
    public const string Branch = "BRANCH";
}
