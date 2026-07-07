namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Registry tap trung tat ca ReportDescriptor cua he thong (23 bao cao theo PRD).</summary>
public interface IReportRegistry
{
    IReadOnlyList<ReportDescriptor> GetAll();

    ReportDescriptor? GetByCode(string code);
}
