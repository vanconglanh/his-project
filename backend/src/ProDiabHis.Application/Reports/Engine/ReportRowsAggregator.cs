namespace ProDiabHis.Application.Reports.Engine;

/// <summary>
/// Gop logic tinh group/subtotal/grand-total/KPI tu 1 tap dong (rows) + descriptor — dung chung boi
/// <see cref="GenericReportDataService"/> (request HTTP) va Report Schedule dispatch job (Hangfire,
/// khong co HttpContext) de tranh lap code tinh toan.
/// </summary>
public static class ReportRowsAggregator
{
    public static decimal SumColumn(IEnumerable<IDictionary<string, object?>> src, string key)
        => src.Sum(row => ReportValueConverter.ToDecimal(row.TryGetValue(key, out var v) ? v : null));

    /// <summary>Tinh ReportDataResult day du (khong phan trang — dung cho export/schedule) tu 1 tap rows tho.</summary>
    public static ReportDataResult BuildFull(ReportDescriptor descriptor, List<IDictionary<string, object?>> rows)
    {
        var subtotalKeys = descriptor.Columns.Where(c => c.IsGroupSubtotal).Select(c => c.Key).ToList();

        List<ReportGroupResult>? groups = null;
        List<IDictionary<string, object?>>? flatRows = null;

        if (!string.IsNullOrWhiteSpace(descriptor.GroupByKey))
        {
            groups = rows
                .GroupBy(r => r.TryGetValue(descriptor.GroupByKey, out var gv) && gv != null ? gv.ToString() ?? "—" : "—")
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
            flatRows = rows;
        }

        var grandTotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(rows, k));
        var kpiResults = descriptor.Kpis.Select(k => new ReportKpiResult(k.Label, k.Tint, k.Compute(rows), k.IsMoney)).ToList();

        return new ReportDataResult(descriptor.Columns, groups, flatRows, grandTotals, kpiResults, rows.Count);
    }
}
