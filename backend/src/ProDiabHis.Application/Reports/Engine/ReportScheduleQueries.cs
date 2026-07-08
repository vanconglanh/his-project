using MediatR;

namespace ProDiabHis.Application.Reports.Engine;

public record GetReportSchedulesQuery() : IRequest<IReadOnlyList<ReportSchedule>>;

public record CreateReportScheduleCommand(ReportScheduleInput Input) : IRequest<ReportSchedule>;

public record UpdateReportScheduleCommand(string Id, ReportScheduleInput Input) : IRequest<ReportSchedule>;

public record DeleteReportScheduleCommand(string Id) : IRequest;
