namespace ProDiabHis.Application.Reports;

/// <summary>
/// Sinh ma bao cao doc nhat theo pattern RPT-{TYPE}-{yyyyMMdd}-{seq:D4}.
/// Sequence duoc tang dan bang Redis INCR, TTL 26h.
/// </summary>
public interface IReportCodeGenerator
{
    /// <summary>
    /// Tra ve ma bao cao tiep theo, vi du: RPT-FIN-20260526-0001.
    /// </summary>
    Task<string> NextAsync(int tenantId, ReportType type, DateOnly date, CancellationToken ct = default);
}
