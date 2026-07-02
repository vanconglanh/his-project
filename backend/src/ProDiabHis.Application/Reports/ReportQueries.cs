using MediatR;

namespace ProDiabHis.Application.Reports;

public record GetRevenueReportQuery(
    string Period,
    DateOnly From,
    DateOnly To,
    Guid? ClinicId) : IRequest<RevenueReportResponse>;

public record GetDoctorKpiQuery(
    DateOnly From,
    DateOnly To,
    int Top = 20) : IRequest<IReadOnlyList<DoctorKpiResponse>>;

public record GetTopDrugsQuery(
    DateOnly From,
    DateOnly To,
    int Top = 20) : IRequest<IReadOnlyList<TopDrugResponse>>;

public record GetDiabetesCohortQuery(
    DateOnly From,
    DateOnly To) : IRequest<DiabetesCohortResponse>;

public record GetDashboardOverviewQuery() : IRequest<DashboardOverviewResponse>;

public record GetRevenueTrendChartQuery(string Range = "30d") : IRequest<IReadOnlyList<ChartDataPoint>>;

public record GetEncountersTrendChartQuery(string Range = "30d") : IRequest<IReadOnlyList<ChartDataPoint>>;

public record GetAlertsQuery(string? Severity, string? Type) : IRequest<IReadOnlyList<AlertResponse>>;

public record GetEncountersCountQuery(
    string Period,
    DateOnly From,
    DateOnly To) : IRequest<IReadOnlyList<EncounterCountItem>>;

public record GetTopDiagnosesQuery(
    DateOnly From,
    DateOnly To,
    int Top = 20) : IRequest<IReadOnlyList<TopDiagnosisResponse>>;

public record ExportReportCommand(ExportReportRequest Request) : IRequest<(byte[] Content, string ContentType, string FileName)>;

/// <summary>Query xuat bao cao PDF A4 theo loai (Financial / Clinical / Pharmacy).</summary>
public record GetReportPdfQuery(
    ReportType ReportType,
    DateOnly From,
    DateOnly To,
    int? ClinicId,
    string? ReportCode = null) : IRequest<(byte[] Bytes, string FileName, string ReportCode)>;
