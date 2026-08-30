using ProDiabHis.Domain.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>
/// Override gia + an/hien THUOC theo pham vi BRANCH hoac GROUP.
/// Map bang diab_his_pha_drug_branch_prices (migration 9185) — song song bang dich vu
/// diab_his_bil_service_branch_prices, giu nguyen logic da test.
/// Logic resolve gia 3 tang + an/hien nam o IBranchPriceResolver, KHONG lam o entity nay.
/// DrugId dang string vi diab_his_pha_drugs.ID co the la INT (legacy) hoac CHAR(36) UUID.
/// </summary>
public class DrugBranchPrice : IAuditTimestamps
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public string DrugId { get; set; } = string.Empty;

    /// <summary>BRANCH hoac GROUP - xem <see cref="PriceOverrideScope"/></summary>
    public string Scope { get; set; } = PriceOverrideScope.Branch;

    public int? BranchId { get; set; }
    public int? GroupId { get; set; }
    public decimal Price { get; set; }

    /// <summary>1=hien, 0=an thuoc khoi chi nhanh/nhom nay (du thuoc van ton tai o tenant)</summary>
    public bool IsActive { get; set; } = true;

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
