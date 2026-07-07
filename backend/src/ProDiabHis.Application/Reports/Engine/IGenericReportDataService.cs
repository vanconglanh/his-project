namespace ProDiabHis.Application.Reports.Engine;

/// <summary>1 nhom du lieu (group-by) trong ket qua bao cao — vd nhom theo nhan vien thu ngan.</summary>
public record ReportGroupResult(
    string Key,
    string Label,
    int Count,
    IReadOnlyList<IDictionary<string, object?>> Rows,
    IReadOnlyDictionary<string, decimal> Subtotals);

/// <summary>1 KPI da tinh gia tri, san sang tra ve FE.</summary>
public record ReportKpiResult(string Label, string Tint, decimal Value, bool IsMoney);

/// <summary>Ket qua tra ve cho 1 lan goi du lieu bao cao (dung chung cho JSON grid + PDF + Excel).</summary>
public record ReportDataResult(
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportGroupResult>? Groups,
    IReadOnlyList<IDictionary<string, object?>>? Rows,
    IReadOnlyDictionary<string, decimal> Totals,
    IReadOnlyList<ReportKpiResult> Kpis,
    int TotalRows);

public interface IGenericReportDataService
{
    /// <summary>
    /// Chay descriptor.BuildQuery voi tenant_id + filters, group theo GroupByKey (neu co),
    /// tinh subtotal/grand-total/KPI. Validate khoang ngay &lt;= 366 ngay.
    /// </summary>
    Task<ReportDataResult> GetDataAsync(
        string reportCode,
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<string, string?> filters,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
