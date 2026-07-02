using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Encounters;

public record ListEncountersQuery(
    string? PatientId,
    string? DoctorId,
    string? RoomId,
    string? Status,
    string? EncounterType,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int Page,
    int PageSize)
    : IRequest<Result<PagedResult<EncounterResponse>>>;

public record GetEncounterDetailQuery(Guid EncounterId)
    : IRequest<Result<EncounterDetailResponse>>;

public record GetEncounterTimelineQuery(Guid EncounterId)
    : IRequest<Result<IReadOnlyList<TimelineEventDto>>>;

public record GetOver12hAlertsQuery()
    : IRequest<Result<IReadOnlyList<Over12hAlertDto>>>;
