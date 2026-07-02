using System.Data;
using System.Text;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Bhyt;

// ── List Reconcile Items ──────────────────────────────────────────────────────

public record ListReconcileItemsQuery(int ExportId, string? StatusFilter, int Page, int PageSize)
    : IRequest<Result<PagedResult<ReconcileItemResponse>>>;

public class ListReconcileItemsHandler : IRequestHandler<ListReconcileItemsQuery, Result<PagedResult<ReconcileItemResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListReconcileItemsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<ReconcileItemResponse>>> Handle(ListReconcileItemsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var where = new StringBuilder("WHERE export_id=@eid AND tenant_id=@t");
        var p = new DynamicParameters();
        p.Add("eid", q.ExportId);
        p.Add("t", _tenant.TenantId);

        if (!string.IsNullOrEmpty(q.StatusFilter) && q.StatusFilter != "ALL")
        {
            where.Append(" AND status=@st");
            p.Add("st", q.StatusFilter);
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_int_bhyt_reconcile_items {where}", p);

        p.Add("offset", (q.Page - 1) * q.PageSize);
        p.Add("limit", q.PageSize);

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT * FROM diab_his_int_bhyt_reconcile_items {where} ORDER BY table_no, id LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(DisputeReconcileItemHandler.MapReconcileItem).ToList();
        return Result<PagedResult<ReconcileItemResponse>>.Success(
            new PagedResult<ReconcileItemResponse>(items, q.Page, q.PageSize, total));
    }
}

// ── Get Reconcile Summary ─────────────────────────────────────────────────────

public record GetReconcileSummaryQuery(int ExportId) : IRequest<Result<ReconcileSummaryResponse>>;

public class GetReconcileSummaryHandler : IRequestHandler<GetReconcileSummaryQuery, Result<ReconcileSummaryResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetReconcileSummaryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<ReconcileSummaryResponse>> Handle(GetReconcileSummaryQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var export = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, period_month FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = q.ExportId, t = _tenant.TenantId });

        if (export == null)
            return Result<ReconcileSummaryResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        var agg = await conn.QueryFirstAsync<dynamic>(
            @"SELECT
                COUNT(*) as total_items,
                SUM(CASE WHEN status='APPROVED' THEN 1 ELSE 0 END) as approved_items,
                SUM(CASE WHEN status='REJECTED' THEN 1 ELSE 0 END) as rejected_items,
                SUM(CASE WHEN status='ADJUSTED' THEN 1 ELSE 0 END) as adjusted_items,
                SUM(CASE WHEN status='DISPUTED' THEN 1 ELSE 0 END) as disputed_items,
                COALESCE(SUM(request_amount),0) as total_requested,
                COALESCE(SUM(approved_amount),0) as total_approved,
                COALESCE(SUM(rejected_amount),0) as total_rejected
              FROM diab_his_int_bhyt_reconcile_items
              WHERE export_id=@id AND tenant_id=@t",
            new { id = q.ExportId, t = _tenant.TenantId });

        var byTable = await conn.QueryAsync<dynamic>(
            @"SELECT table_no,
                COALESCE(SUM(request_amount),0)  as requested,
                COALESCE(SUM(approved_amount),0) as approved,
                COALESCE(SUM(rejected_amount),0) as rejected
              FROM diab_his_int_bhyt_reconcile_items
              WHERE export_id=@id AND tenant_id=@t
              GROUP BY table_no ORDER BY table_no",
            new { id = q.ExportId, t = _tenant.TenantId });

        var topRejections = await conn.QueryAsync<dynamic>(
            @"SELECT rejection_code as code, rejection_reason as reason,
                COUNT(*) as count, COALESCE(SUM(rejected_amount),0) as amount
              FROM diab_his_int_bhyt_reconcile_items
              WHERE export_id=@id AND tenant_id=@t AND rejection_code IS NOT NULL
              GROUP BY rejection_code, rejection_reason
              ORDER BY count DESC LIMIT 10",
            new { id = q.ExportId, t = _tenant.TenantId });

        var summary = new ReconcileSummaryResponse(
            ExportId: q.ExportId,
            PeriodMonth: (string)export.period_month,
            TotalItems: (int)agg.total_items,
            ApprovedItems: (int)agg.approved_items,
            RejectedItems: (int)agg.rejected_items,
            AdjustedItems: (int)agg.adjusted_items,
            DisputedItems: (int)agg.disputed_items,
            TotalRequestedAmount: (decimal)agg.total_requested,
            TotalApprovedAmount: (decimal)agg.total_approved,
            TotalRejectedAmount: (decimal)agg.total_rejected,
            ByTable: byTable.Select(r => new ReconcileSummaryByTable(
                (int)r.table_no, (decimal)r.requested, (decimal)r.approved, (decimal)r.rejected)).ToList(),
            TopRejectionReasons: topRejections.Select(r => new ReconcileTopRejection(
                (string)r.code, (string)(r.reason ?? ""), (int)r.count, (decimal)r.amount)).ToList());

        return Result<ReconcileSummaryResponse>.Success(summary);
    }
}
