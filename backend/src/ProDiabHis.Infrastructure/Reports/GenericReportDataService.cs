using System.Data;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Thuc thi ReportDescriptor.BuildQuery voi tenant_id + filters, group theo GroupByKey (neu co),
/// tinh subtotal/grand-total/KPI o tang application (khong lap SQL rieng cho tung bao cao).
/// </summary>
public class GenericReportDataService : IGenericReportDataService
{
    private readonly IReportRegistry _registry;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GenericReportDataService(IReportRegistry registry, IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _registry = registry;
        _db = db;
        _tenant = tenant;
    }

    public async Task<ReportDataResult> GetDataAsync(
        string reportCode,
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<string, string?> filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var descriptor = _registry.GetByCode(reportCode)
            ?? throw new ReportValidationException("REPORT_NOT_FOUND", $"Không tìm thấy báo cáo '{reportCode}'");

        if (from > to)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

        if (to.DayNumber - from.DayNumber > 366)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", "Khoảng thời gian báo cáo không được vượt quá 366 ngày");

        page = Math.Max(1, page);
        // Tran tren 5000 — khop voi LIMIT trong descriptor.BuildQuery (an toan bo nho khi export toan bo).
        pageSize = pageSize <= 0 ? 100 : Math.Min(pageSize, 5000);

        var ctx = new ReportQueryContext(_tenant.TenantId, from, to, filters);
        var (sql, parameters) = descriptor.BuildQuery(ctx);

        using var conn = (IDbConnection)_db.CreateConnection();
        var rawRows = await conn.QueryAsync(sql, parameters);

        var rows = rawRows
            .Select(r =>
            {
                var src = (IDictionary<string, object>)r;
                var dict = new Dictionary<string, object?>(src.Count);
                foreach (var kv in src) dict[kv.Key] = kv.Value;
                return (IDictionary<string, object?>)dict;
            })
            .ToList();

        var subtotalKeys = descriptor.Columns.Where(c => c.IsGroupSubtotal).Select(c => c.Key).ToList();

        decimal SumColumn(IEnumerable<IDictionary<string, object?>> src, string key)
            => src.Sum(row => ReportValueConverter.ToDecimal(row.TryGetValue(key, out var v) ? v : null));

        List<ReportGroupResult>? groups = null;
        List<IDictionary<string, object?>>? flatRows = null;

        if (!string.IsNullOrWhiteSpace(descriptor.GroupByKey))
        {
            groups = rows
                .GroupBy(r => r.TryGetValue(descriptor.GroupByKey, out var gv) && gv != null
                    ? gv.ToString() ?? "—"
                    : "—")
                .Select(g =>
                {
                    var subtotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(g, k));
                    return new ReportGroupResult(g.Key, g.Key, g.Count(), g.ToList(), subtotals);
                })
                .OrderBy(g => g.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            // Bao cao khong group: phan trang tren tap dong (grand total van tinh tren toan bo tap ket qua).
            flatRows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        var grandTotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(rows, k));

        var kpiResults = descriptor.Kpis
            .Select(k => new ReportKpiResult(k.Label, k.Tint, k.Compute(rows), k.IsMoney))
            .ToList();

        return new ReportDataResult(descriptor.Columns, groups, flatRows, grandTotals, kpiResults, rows.Count);
    }
}
