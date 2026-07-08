namespace ProDiabHis.Application.Reports.Engine;

// ---- Request DTO cho endpoint Dashboard tuy bien (Api layer bind qua JSON snake_case toan cuc) ---- //

public record ReportDashboardWidgetRequest(
    string ReportCode,
    string Title,
    string WidgetType,
    int W = 4,
    int H = 3,
    int X = 0,
    int Y = 0);

public record SaveReportDashboardRequest(
    string Title,
    List<ReportDashboardWidgetRequest> Widgets,
    string? Visibility);

/// <summary>Chuyen doi request DTO (Api) -> ReportDashboardInput (Application) — validate widget_type/so luong
/// widget, nem REPORT_DASHBOARD_INVALID (400) thay vi de loi binding JSON chung chung.</summary>
public static class ReportDashboardRequestMapper
{
    public const int MaxWidgets = 12;

    public static ReportDashboardInput ToInput(string title, List<ReportDashboardWidgetRequest>? widgets, string? visibility)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ReportValidationException("REPORT_DASHBOARD_INVALID", "Dashboard phải có tiêu đề");

        if (widgets is null || widgets.Count == 0)
            throw new ReportValidationException("REPORT_DASHBOARD_INVALID", "Dashboard phải có ít nhất 1 widget");

        if (widgets.Count > MaxWidgets)
            throw new ReportValidationException("REPORT_DASHBOARD_INVALID", $"Số widget vượt giới hạn cho phép ({MaxWidgets})");

        var mapped = widgets.Select(w =>
        {
            if (string.IsNullOrWhiteSpace(w.ReportCode))
                throw new ReportValidationException("REPORT_DASHBOARD_INVALID", "Widget thiếu report_code");

            var widgetType = w.WidgetType?.ToUpperInvariant() switch
            {
                "CHART" => ReportWidgetType.Chart,
                "KPI" => ReportWidgetType.Kpi,
                "TABLE" or null or "" => ReportWidgetType.Table,
                _ => throw new ReportValidationException("REPORT_DASHBOARD_INVALID", $"widget_type '{w.WidgetType}' không hợp lệ")
            };

            return new ReportDashboardWidget(
                w.ReportCode, string.IsNullOrWhiteSpace(w.Title) ? w.ReportCode : w.Title,
                widgetType, w.W, w.H, w.X, w.Y);
        }).ToList();

        var visibilityEnum = string.Equals(visibility, "PRIVATE", StringComparison.OrdinalIgnoreCase)
            ? ReportVisibility.Private : ReportVisibility.Tenant;

        return new ReportDashboardInput(title, mapped, visibilityEnum);
    }
}
