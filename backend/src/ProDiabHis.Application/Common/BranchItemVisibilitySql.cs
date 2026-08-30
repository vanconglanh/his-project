namespace ProDiabHis.Application.Common;

/// <summary>
/// Sinh dieu kien SQL loc AN/HIEN item (dich vu/thuoc) theo chi nhanh cho Dapper (read path).
/// Dung chung cho danh sach dich vu va thuoc de tranh trung logic (mirror thu tu uu tien cua
/// IBranchPriceResolver): override BRANCH quyet dinh truoc, khong co override BRANCH thi xet
/// override GROUP cua nhom chi nhanh.
///
/// Quy tac AN 1 item tai chi nhanh B (ngay D):
///   - Co dong override scope=BRANCH, branch=B, dang hieu luc, is_active=0  => AN
///   - Hoac: KHONG co dong override scope=BRANCH nao (bat ke is_active) dang hieu luc cho B,
///           MA co dong scope=GROUP (nhom cua B) dang hieu luc, is_active=0            => AN
///   - Con lai => HIEN (ke ca khong co override, hoac override is_active=1)
///
/// Params can add o noi goi: @vTenantId, @vBranchId, @vAsOf. Chi ghep dieu kien khi thuc su
/// co branch context (branchId > 0 va khong ignore filter) — nguoc lai HIEN tat ca.
/// </summary>
public static class BranchItemVisibilitySql
{
    /// <param name="overrideTable">Bang override, vd diab_his_bil_service_branch_prices.</param>
    /// <param name="itemColumn">Cot item trong bang override, vd service_id / drug_id.</param>
    /// <param name="itemIdExpr">Bieu thuc id item cua bang chinh, vd "s.id" hoac "d.ID".</param>
    public static string VisibleCondition(string overrideTable, string itemColumn, string itemIdExpr)
    {
        var effWindow = "bp.effective_from <= @vAsOf AND (bp.effective_to IS NULL OR bp.effective_to >= @vAsOf)";
        return $@"NOT (
    EXISTS (SELECT 1 FROM {overrideTable} bp
            WHERE bp.tenant_id = @vTenantId AND bp.{itemColumn} = {itemIdExpr}
              AND bp.scope = 'BRANCH' AND bp.branch_id = @vBranchId AND bp.deleted_at IS NULL
              AND bp.is_active = 0 AND {effWindow})
    OR (
        NOT EXISTS (SELECT 1 FROM {overrideTable} bp
                    WHERE bp.tenant_id = @vTenantId AND bp.{itemColumn} = {itemIdExpr}
                      AND bp.scope = 'BRANCH' AND bp.branch_id = @vBranchId AND bp.deleted_at IS NULL
                      AND {effWindow})
        AND EXISTS (SELECT 1 FROM {overrideTable} bp
                    JOIN diab_his_sys_branches br ON br.id = @vBranchId AND br.tenant_id = @vTenantId
                    WHERE bp.tenant_id = @vTenantId AND bp.{itemColumn} = {itemIdExpr}
                      AND bp.scope = 'GROUP' AND bp.group_id = br.group_id AND bp.deleted_at IS NULL
                      AND bp.is_active = 0 AND {effWindow})
    )
)";
    }
}
