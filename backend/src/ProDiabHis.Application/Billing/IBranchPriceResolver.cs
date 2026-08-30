namespace ProDiabHis.Application.Billing;

/// <summary>Loai item ap dung override gia + an/hien theo chi nhanh.</summary>
public enum PriceItemType
{
    Service,
    Drug
}

/// <summary>
/// Ket qua resolve gia + trang thai hien thi cua 1 item (dich vu/thuoc) tai 1 chi nhanh.
/// PriceSource: TENANT|GROUP|BRANCH. IsActive=false khi item bi an o chi nhanh do (co dong
/// override is_active=0 duoc ap dung). Khi resolve ve gia goc TENANT thi IsActive luon true.
/// </summary>
public record ResolvedItemPrice(decimal Price, string PriceSource, Guid? PriceOverrideId, bool IsActive);

/// <summary>
/// Tang resolve GIA + AN/HIEN dung chung cho ca DICH VU va THUOC (khong trung logic).
/// Thu tu uu tien 3 tang (giong dich vu goc BR-70): override BRANCH con hieu luc >
/// override GROUP (theo nhom cua branch) con hieu luc > gia goc TENANT.
/// Doc bang override bang Dapper theo dung tenant/branch/ngay hieu luc.
/// itemId truyen dang chuoi vi drug.ID co the la INT (legacy) hoac CHAR(36) UUID;
/// service.id luon CHAR(36) — .ToString() la du.
/// </summary>
public interface IBranchPriceResolver
{
    Task<ResolvedItemPrice?> ResolveAsync(
        int tenantId, PriceItemType itemType, string itemId, int? branchId, DateOnly asOfDate,
        CancellationToken ct = default);
}
