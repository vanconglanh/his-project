namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Xuat PDF config-driven cho Report Engine (23 bao cao) — 1 implementation dung chung cho moi descriptor.</summary>
public interface IGenericReportPdfExporter
{
    Task<byte[]> ExportAsync(
        ReportDescriptor descriptor,
        ReportPdfRequest req,
        LetterheadDto letterhead,
        ReportDataResult data,
        CancellationToken ct = default);
}
