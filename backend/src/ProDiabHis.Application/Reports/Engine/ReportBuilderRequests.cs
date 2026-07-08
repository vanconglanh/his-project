namespace ProDiabHis.Application.Reports.Engine;

// ---- Request DTO cho endpoint Report Builder (Api layer bind qua JSON snake_case toan cuc) ---- //

public record ReportDefinitionColumnRequest(string Field, string Label, string? Agg, bool IsSubtotal = false);

public record ReportDefinitionFilterRequest(string Field, string Op, List<string?> Value);

public record ReportDefinitionSortRequest(string Field, bool Desc);

public record ReportDefinitionKpiRequest(string Label, string Field, string Agg);

public record ReportDefinitionChartRequest(string Type, List<string> Dims, string Measure);

public record ReportDefinitionBodyRequest(
    List<ReportDefinitionColumnRequest> Columns,
    List<ReportDefinitionFilterRequest>? Filters,
    List<string>? GroupBy,
    List<ReportDefinitionSortRequest>? Sort,
    List<ReportDefinitionKpiRequest>? Kpis);

public record SaveReportDefinitionRequest(
    string Title,
    string DatasetKey,
    ReportDefinitionBodyRequest Definition,
    ReportDefinitionChartRequest? Chart,
    string? ViewType,
    string? Visibility);

public record PreviewReportDefinitionRequest(
    string DatasetKey,
    ReportDefinitionBodyRequest Definition,
    ReportDefinitionChartRequest? Chart);

/// <summary>Chuyen doi request DTO (Api) -> ReportDefinitionInput (Application) — parse Agg tu chuoi,
/// nem REPORT_DEFINITION_INVALID (400) neu sai token thay vi de loi binding JSON chung chung.</summary>
public static class ReportBuilderRequestMapper
{
    public static ReportDefinitionInput ToInput(string title, string datasetKey, ReportDefinitionBodyRequest def,
        ReportDefinitionChartRequest? chart, string? viewType, string? visibility)
    {
        if (def is null || def.Columns is null || def.Columns.Count == 0)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Báo cáo phải có ít nhất 1 cột");

        var columns = def.Columns.Select(c => new ReportDefinitionColumn(
            c.Field, c.Label,
            string.IsNullOrWhiteSpace(c.Agg) ? null : ReportAggregationCodes.FromCode(c.Agg),
            c.IsSubtotal)).ToList();

        var filters = (def.Filters ?? new()).Select(f => new ReportDefinitionFilter(f.Field, f.Op, f.Value ?? new())).ToList();
        var groupBy = def.GroupBy ?? new();
        var sort = (def.Sort ?? new()).Select(s => new ReportDefinitionSort(s.Field, s.Desc)).ToList();
        var kpis = (def.Kpis ?? new()).Select(k => new ReportDefinitionKpi(k.Label, k.Field, ReportAggregationCodes.FromCode(k.Agg))).ToList();

        var chartDto = chart is null ? null : new ReportDefinitionChart(chart.Type, chart.Dims, chart.Measure);

        var viewTypeEnum = string.Equals(viewType, "CHART", StringComparison.OrdinalIgnoreCase)
            ? ReportViewType.Chart : ReportViewType.Table;

        if (viewTypeEnum == ReportViewType.Chart && chartDto is null)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Báo cáo dạng biểu đồ (CHART) phải kèm cấu hình 'chart'");

        var visibilityEnum = string.Equals(visibility, "PRIVATE", StringComparison.OrdinalIgnoreCase)
            ? ReportVisibility.Private : ReportVisibility.Tenant;

        return new ReportDefinitionInput(title, datasetKey, columns, filters, groupBy, sort, kpis, chartDto, viewTypeEnum, visibilityEnum);
    }
}
