namespace ProDiabHis.Application.Reports;

public interface IPdfReportExporter
{
    /// <summary>Xuat bao cao doanh thu sang PDF bytes (backward compat).</summary>
    Task<byte[]> ExportRevenueAsync(RevenueReportResponse report, CancellationToken ct = default);

    /// <summary>Xuat bao cao bac si KPI sang PDF bytes (backward compat).</summary>
    Task<byte[]> ExportDoctorKpiAsync(IReadOnlyList<DoctorKpiResponse> rows, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Xuat bao cao tai chinh A4 (Financial) theo chuan nhan dien dIaB.</summary>
    Task<byte[]> ExportFinancialAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<FinancialRowDto> rows,
        CancellationToken ct = default);

    /// <summary>Xuat bao cao lam sang A4 (Clinical) theo chuan nhan dien dIaB.</summary>
    Task<byte[]> ExportClinicalAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<ClinicalRowDto> rows,
        CancellationToken ct = default);

    /// <summary>Xuat bao cao ton kho duoc A4 (Pharmacy) theo chuan nhan dien dIaB.</summary>
    Task<byte[]> ExportPharmacyAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<PharmacyRowDto> rows,
        CancellationToken ct = default);
}
