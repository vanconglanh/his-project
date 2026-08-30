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

    // Migration 9110: cho XML Bang 2 QD 3176 - CHUA co nghiep vu nhap lieu, luon NULL cho toi
    // khi co module quan ly dau thau / danh muc dang ky thuoc.
    public string? SoDangKy { get; set; }
    public string? MaNhaThau { get; set; }

    // Migration 9180: duong dung chuan hoa (nguon cho XML 4210 Bang 2 DUONG_DUNG) va
    // ma thuoc theo danh muc BHYT (thay ma noi bo khi xuat XML Bang 2).
    public string? Route { get; set; }
    public string? BhytCode { get; set; }
}
