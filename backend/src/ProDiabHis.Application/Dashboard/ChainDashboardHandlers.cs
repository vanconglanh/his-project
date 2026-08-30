using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Dashboard;

// ---- Queries ----

public record GetBranchRankingQuery(DateOnly From, DateOnly To) : IRequest<Result<BranchRankingResponse>>;

public record GetBranchDetailQuery(int BranchId, DateOnly From, DateOnly To) : IRequest<Result<BranchDetailResponse>>;

public static class ChainDashboardErrors
{
    public const string BranchAccessDenied = "BRANCH_ACCESS_DENIED";
}

/// <summary>
/// BR-33/BR-93: quy dinh S1/S2/S3 tinh danh sach chi nhanh duoc phep xem tren man hinh CHI DOC
/// (dashboard/bao cao). Tach thanh ham thuan de unit test khong can DB that.
///   - S3 (IgnoreBranchFilter=true): null = khong gioi han (xem tat ca chi nhanh cua tenant).
///   - S2 (AllowedBranchIds nhieu hon 1): tra danh sach allowed.
///   - S1 (AllowedBranchIds rong hoac 1 phan tu): chi 1 chi nhanh dang hoat dong.
/// </summary>
public static class BranchScopeResolver
{
    // hasCrossView: user co quyen branch.cross_view (S3) — xem TAT CA chi nhanh tenant BAT KE dang chon
    // chi nhanh nao qua X-Branch-Id. Phai dua vao QUYEN (entitlement), khong dua vao IgnoreBranchFilter
    // (co bi tat khi user chon 1 chi nhanh cu the) — neu khong admin se chi thay 1 chi nhanh dang chon.
    public static IReadOnlyList<int>? ResolveAllowedBranchIds(IBranchProvider branchProvider, bool hasCrossView = false)
    {
        if (hasCrossView || branchProvider.IgnoreBranchFilter) return null;
        if (branchProvider.AllowedBranchIds.Count > 0) return branchProvider.AllowedBranchIds.ToList();
        return new List<int> { branchProvider.BranchId };
    }

    /// <summary>BR-93: user S1 (chinh xac 1 chi nhanh, khong co IgnoreBranchFilter) khong duoc phep
    /// thay so voi chi nhanh khac - dung de tra 403 khi drill-down sang branch ngoai scope.</summary>
    public static bool IsBranchAllowed(IBranchProvider branchProvider, int branchId, bool hasCrossView = false)
    {
        if (hasCrossView || branchProvider.IgnoreBranchFilter) return true;
        var allowed = ResolveAllowedBranchIds(branchProvider);
        return allowed != null && allowed.Contains(branchId);
    }
}

public class GetBranchRankingHandler : IRequestHandler<GetBranchRankingQuery, Result<BranchRankingResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IPermissionChecker _permissionChecker;

    public GetBranchRankingHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IPermissionChecker permissionChecker)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider; _permissionChecker = permissionChecker;
    }

    public async Task<Result<BranchRankingResponse>> Handle(GetBranchRankingQuery q, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var hasCrossView = _permissionChecker.HasPermission("branch.cross_view");
        var allowed = BranchScopeResolver.ResolveAllowedBranchIds(_branchProvider, hasCrossView);

        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        // Tong so chi nhanh cua tenant (BR-92/BR-93: S1 khong lo tong that neu > pham vi cua ho).
        var totalBranchCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE tenant_id = @tenantId AND deleted_at IS NULL",
            new { tenantId });

        var branchFilterSql = allowed != null ? "AND b.id IN @allowed" : "";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        p.Add("from", q.From.ToDateTime(TimeOnly.MinValue));
        p.Add("to", q.To.ToDateTime(TimeOnly.MaxValue));
        if (allowed != null) p.Add("allowed", allowed);

        var days = q.To.DayNumber - q.From.DayNumber + 1;
        var prevFrom = q.From.AddDays(-days);
        var prevTo = q.From.AddDays(-1);
        p.Add("prevFrom", prevFrom.ToDateTime(TimeOnly.MinValue));
        p.Add("prevTo", prevTo.ToDateTime(TimeOnly.MaxValue));

        // BR-86: doanh thu tinh theo billing.branch_id (noi cung cap dich vu), KHONG phai noi thu tien.
        var sql = $@"
            SELECT
                b.id AS branchId,
                b.name AS branchName,
                COALESCE(rev.revenue, 0) AS revenue,
                COALESCE(enc.encounter_count, 0) AS encounterCount,
                COALESCE(np.new_patient_count, 0) AS newPatientCount,
                COALESCE(prevRev.revenue, 0) AS prevRevenue
            FROM diab_his_sys_branches b
            LEFT JOIN (
                SELECT branch_id, SUM(patient_payable) AS revenue
                FROM diab_his_bil_billing
                WHERE tenant_id = @tenantId AND status <> 'VOID' AND deleted_at IS NULL
                  AND created_at BETWEEN @from AND @to
                GROUP BY branch_id
            ) rev ON rev.branch_id = b.id
            LEFT JOIN (
                SELECT branch_id, SUM(patient_payable) AS revenue
                FROM diab_his_bil_billing
                WHERE tenant_id = @tenantId AND status <> 'VOID' AND deleted_at IS NULL
                  AND created_at BETWEEN @prevFrom AND @prevTo
                GROUP BY branch_id
            ) prevRev ON prevRev.branch_id = b.id
            LEFT JOIN (
                SELECT branch_id, COUNT(*) AS encounter_count
                FROM diab_his_enc_encounters
                WHERE tenant_id = @tenantId AND deleted_at IS NULL
                  AND created_at BETWEEN @from AND @to
                GROUP BY branch_id
            ) enc ON enc.branch_id = b.id
            LEFT JOIN (
                SELECT e.branch_id, COUNT(DISTINCT e.patient_id) AS new_patient_count
                FROM diab_his_enc_encounters e
                WHERE e.tenant_id = @tenantId AND e.deleted_at IS NULL
                  AND e.created_at BETWEEN @from AND @to
                  AND NOT EXISTS (
                      SELECT 1 FROM diab_his_enc_encounters e2
                      WHERE e2.tenant_id = e.tenant_id AND e2.patient_id = e.patient_id
                        AND e2.deleted_at IS NULL AND e2.created_at < @from
                  )
                GROUP BY e.branch_id
            ) np ON np.branch_id = b.id
            WHERE b.tenant_id = @tenantId AND b.deleted_at IS NULL {branchFilterSql}
            ORDER BY revenue DESC";

        var rows = (await conn.QueryAsync<dynamic>(sql, p)).ToList();

        var items = rows.Select(r =>
        {
            decimal revenue = (decimal)r.revenue;
            int encCount = Convert.ToInt32(r.encounterCount);
            decimal prevRevenue = (decimal)r.prevRevenue;
            decimal? pctChange = prevRevenue > 0 ? Math.Round((revenue - prevRevenue) / prevRevenue * 100, 2) : null;
            return new BranchRankingRow(
                (int)r.branchId, (string)r.branchName, revenue, encCount,
                encCount > 0 ? Math.Round(revenue / encCount, 2) : 0m,
                Convert.ToInt32(r.newPatientCount),
                0m, // TODO: ty le huy hen - can bang lich hen (sch_appointments) map trang thai CANCELLED, chua co trong pham vi dot nay
                pctChange);
        }).ToList();

        var includedNames = items.Select(i => i.BranchName).ToList();
        var meta = new BranchScopeMeta(
            IncludedBranchCount: items.Count,
            // BR-93: S1 (khong co quyen cross_view/group_view) khong duoc lo tong that -> included = total.
            TotalBranchCount: allowed != null && !_branchProvider.IgnoreBranchFilter && allowed.Count <= 1
                ? items.Count : totalBranchCount,
            IncludedBranchNames: includedNames);

        return Result<BranchRankingResponse>.Success(new BranchRankingResponse(items, meta));
    }
}

public class GetBranchDetailHandler : IRequestHandler<GetBranchDetailQuery, Result<BranchDetailResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IPermissionChecker _permissionChecker;

    public GetBranchDetailHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IPermissionChecker permissionChecker)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider; _permissionChecker = permissionChecker;
    }

    public async Task<Result<BranchDetailResponse>> Handle(GetBranchDetailQuery q, CancellationToken ct)
    {
        // BR-91/AC-6.1.2 + AC-3.2.1: drill-down phai kiem tra branchId nam trong scope truoc,
        // KHONG duoc cap S3 roi loc UI (BR-33). cross_view (S3) duoc phep moi chi nhanh tenant.
        if (!BranchScopeResolver.IsBranchAllowed(_branchProvider, q.BranchId, _permissionChecker.HasPermission("branch.cross_view")))
            return Result<BranchDetailResponse>.Failure(ChainDashboardErrors.BranchAccessDenied,
                "Không có quyền truy cập dữ liệu chi nhánh này");

        var tenantId = _currentUser.TenantId!.Value;
        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        var branchName = await conn.ExecuteScalarAsync<string?>(
            "SELECT name FROM diab_his_sys_branches WHERE id = @branchId AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { branchId = q.BranchId, tenantId });
        if (branchName == null)
            return Result<BranchDetailResponse>.Failure(ChainDashboardErrors.BranchAccessDenied, "Không tìm thấy chi nhánh");

        var sql = @"
            SELECT
                e.doctor_id AS doctorId,
                u.full_name AS doctorName,
                COALESCE(SUM(bl.patient_payable), 0) AS revenue,
                COUNT(DISTINCT e.id) AS encounterCount
            FROM diab_his_enc_encounters e
            LEFT JOIN diab_his_sec_users u ON u.id = e.doctor_id
            LEFT JOIN diab_his_bil_billing bl
                ON bl.encounter_id = e.id AND bl.tenant_id = e.tenant_id AND bl.status <> 'VOID' AND bl.deleted_at IS NULL
            WHERE e.tenant_id = @tenantId AND e.branch_id = @branchId AND e.deleted_at IS NULL
              AND e.doctor_id IS NOT NULL
              AND e.created_at BETWEEN @from AND @to
            GROUP BY e.doctor_id, u.full_name
            ORDER BY revenue DESC";

        var rows = await conn.QueryAsync<dynamic>(sql, new
        {
            tenantId,
            branchId = q.BranchId,
            from = q.From.ToDateTime(TimeOnly.MinValue),
            to = q.To.ToDateTime(TimeOnly.MaxValue)
        });

        var doctors = rows.Select(r =>
        {
            decimal revenue = (decimal)r.revenue;
            int encCount = Convert.ToInt32(r.encounterCount);
            return new DoctorKpiRow(
                Guid.Parse((string)r.doctorId), (string?)r.doctorName ?? "(Không rõ)",
                revenue, encCount, encCount > 0 ? Math.Round(revenue / encCount, 2) : 0m);
        }).ToList();

        return Result<BranchDetailResponse>.Success(new BranchDetailResponse(q.BranchId, branchName, doctors));
    }
}
