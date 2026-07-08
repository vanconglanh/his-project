using MediatR;

namespace ProDiabHis.Application.Reports.Engine;

// ---- Report Builder (P1) — dataset / CRUD definition / preview ---- //

public record GetReportDatasetsQuery() : IRequest<IReadOnlyList<Dataset>>;

public record GetReportDefinitionsQuery() : IRequest<IReadOnlyList<ReportDefinition>>;

public record CreateReportDefinitionCommand(ReportDefinitionInput Input) : IRequest<ReportDefinition>;

public record UpdateReportDefinitionCommand(string Id, ReportDefinitionInput Input) : IRequest<ReportDefinition>;

public record DeleteReportDefinitionCommand(string Id) : IRequest;

public record PreviewReportDefinitionQuery(
    string DatasetKey,
    IReadOnlyList<ReportDefinitionColumn> Columns,
    IReadOnlyList<ReportDefinitionFilter> Filters,
    IReadOnlyList<string> GroupBy,
    IReadOnlyList<ReportDefinitionSort> Sort,
    IReadOnlyList<ReportDefinitionKpi> Kpis,
    DateOnly From,
    DateOnly To) : IRequest<ReportDataResult>;
