namespace ProDiabHis.Application.Common;

/// <summary>
/// Helper sinh dieu kien SQL loc branch cho Dapper (read path) theo dung cong thuc
/// dung o EF Core Global Query Filter (xem docs/erd/branch-multi-chi-nhanh.md muc 5.3/5.4).
/// Quy uoc: neu IgnoreBranchFilter=true (super admin / branch.cross_view khong truyen branchId)
///   thi bo qua loc; nguoc lai chi thay du lieu cua branch hien tai HOAC du lieu branch_id IS NULL
///   (du lieu chung / du lieu cu truoc khi tach chi nhanh, giai doan migrate).
/// KHONG duoc lay @branchId/@ignoreBranch tu input client — luon lay tu IBranchProvider.
/// </summary>
public static class BranchSql
{
    /// <summary>
    /// Tra ve mau dieu kien SQL, vd BranchSql.Condition("a") =>
    /// "(@ignoreBranch = 1 OR a.branch_id IS NULL OR a.branch_id = @branchId)"
    /// Ghep vao WHERE bang " AND " thu cong.
    /// </summary>
    public static string Condition(string alias, string column = "branch_id")
    {
        var prefix = string.IsNullOrEmpty(alias) ? "" : $"{alias}.";
        return $"(@ignoreBranch = 1 OR {prefix}{column} IS NULL OR {prefix}{column} = @branchId)";
    }

    /// <summary>Tra ve object chua @branchId/@ignoreBranch de merge vao Dapper param (anonymous object
    /// khong the merge truc tiep -> dung DynamicParameters o noi goi neu can nhieu tham so).</summary>
    public static (int branchId, bool ignoreBranch) Params(IBranchProvider branchProvider) =>
        (branchProvider.BranchId, branchProvider.IgnoreBranchFilter);
}
