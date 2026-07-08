using MediatR;

namespace ProDiabHis.Application.Reports.Engine;

/// <summary>1 tuy chon cho filter dropdown (vd Nguoi thu, Quay thu, Bac si...).</summary>
public record ReportOptionItem(string Value, string Label);

public record GetReportCatalogQuery() : IRequest<IReadOnlyList<ReportDescriptor>>;

public record GetReportDataQuery(
    string Code,
    DateOnly From,
    DateOnly To,
    IReadOnlyDictionary<string, string?> Filters,
    int Page,
    int PageSize) : IRequest<ReportDataResult>;

public record ExportGenericReportQuery(
    string Code,
    DateOnly From,
    DateOnly To,
    IReadOnlyDictionary<string, string?> Filters,
    string Format) : IRequest<(byte[] Bytes, string ContentType, string FileName)>;

public record GetReportOptionsQuery(string Source) : IRequest<IReadOnlyList<ReportOptionItem>>;
