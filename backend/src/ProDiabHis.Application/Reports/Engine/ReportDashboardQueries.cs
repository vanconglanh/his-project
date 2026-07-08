using MediatR;

namespace ProDiabHis.Application.Reports.Engine;

// ---- Dashboard tuy bien (P2.2) — CRUD + chay du lieu tung widget ---- //

public record GetReportDashboardsQuery() : IRequest<IReadOnlyList<ReportDashboard>>;

public record GetReportDashboardByIdQuery(string Id) : IRequest<ReportDashboard>;

public record CreateReportDashboardCommand(ReportDashboardInput Input) : IRequest<ReportDashboard>;

public record UpdateReportDashboardCommand(string Id, ReportDashboardInput Input) : IRequest<ReportDashboard>;

public record DeleteReportDashboardCommand(string Id) : IRequest;

/// <summary>1 widget da chay xong du lieu — dung tra ve GET /dashboards/{id}/data.</summary>
public record ReportDashboardWidgetData(string ReportCode, string Title, ReportWidgetType WidgetType, ReportDataResult Payload);

public record ReportDashboardDataResult(string Title, IReadOnlyList<ReportDashboardWidgetData> Widgets);

public record GetReportDashboardDataQuery(string Id, DateOnly From, DateOnly To) : IRequest<ReportDashboardDataResult>;
