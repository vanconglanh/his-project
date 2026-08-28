namespace ProDiabHis.Application.Common;

/// <summary>Cung cap branch context cho request hien tai (scoped, dang ky canh ITenantProvider)</summary>
public interface IBranchProvider
{
    /// <summary>Chi nhanh dang lam viec. 0 = chua xac dinh</summary>
    int BranchId { get; }

    /// <summary>True khi user co branch.cross_view hoac is_super_admin va khong truyen branchId cu the
    /// -> bo qua filter chi nhanh, xem toan tenant</summary>
    bool IgnoreBranchFilter { get; }

    /// <summary>Danh sach branch_id user duoc gan (tu claim branch_ids)</summary>
    IReadOnlyList<int> AllowedBranchIds { get; }

    void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds);
}
