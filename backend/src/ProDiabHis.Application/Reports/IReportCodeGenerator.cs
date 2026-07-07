namespace ProDiabHis.Application.Reports;

/// <summary>
/// Sinh ma bao cao doc nhat theo pattern RPT-{TYPE}-{yyyyMMdd}-{seq:D4}.
/// Sequence duoc tang dan bang Redis INCR, TTL 26h.
/// </summary>
public interface IReportCodeGenerator
{
    /// <summary>
    /// Tra ve ma bao cao tiep theo, vi du: RPT-FIN-20260526-0001.
    /// Backward-compat cho 3 loai bao cao cu (Financial/Clinical/Pharmacy).
    /// </summary>
    Task<string> NextAsync(int tenantId, ReportType type, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Tra ve ma bao cao tiep theo theo typeCode tuy y (2-3 ky tu, vd "RVD" cho revenue-daily),
    /// lay tu ReportDescriptor.PdfTypeCode — dung cho Report Engine config-driven (23 bao cao).
    /// </summary>
    Task<string> NextAsync(int tenantId, string typeCode, DateOnly date, CancellationToken ct = default);
}
