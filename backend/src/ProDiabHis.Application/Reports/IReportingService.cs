namespace ProDiabHis.Application.Reports;

public interface IReportingService
{
    Task<RevenueReportResponse> GetRevenueReportAsync(
        int tenantId, string period, DateOnly from, DateOnly to, Guid? clinicId,
        CancellationToken ct = default);

    Task<IReadOnlyList<DoctorKpiResponse>> GetDoctorKpiAsync(
        int tenantId, DateOnly from, DateOnly to, int top,
        CancellationToken ct = default);

    Task<IReadOnlyList<TopDrugResponse>> GetTopDrugsAsync(
        int tenantId, DateOnly from, DateOnly to, int top,
        CancellationToken ct = default);

    Task<DiabetesCohortResponse> GetDiabetesCohortAsync(
        int tenantId, DateOnly from, DateOnly to,
        CancellationToken ct = default);

    Task<DiabetesCohortDetailedResponse> GetDiabetesCohortDetailedAsync(
        int tenantId, DateOnly asOf, string? dmType,
        CancellationToken ct = default);

    Task<DashboardOverviewResponse> GetDashboardOverviewAsync(
        int tenantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ChartDataPoint>> GetRevenueTrendAsync(
        int tenantId, int days,
        CancellationToken ct = default);

    Task<IReadOnlyList<ChartDataPoint>> GetEncountersTrendAsync(
        int tenantId, int days,
        CancellationToken ct = default);

    Task<IReadOnlyList<AlertResponse>> GetAlertsAsync(
        int tenantId, string? severity, string? type,
        CancellationToken ct = default);
}
