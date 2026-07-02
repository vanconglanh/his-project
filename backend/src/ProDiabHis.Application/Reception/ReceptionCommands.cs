using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reception;

public record CheckInRequest(
    Guid PatientId,
    Guid RoomId,
    List<string>? ServicePackageIds,
    string? ReasonForVisit,
    string? Note,
    string? Priority);

public record CheckInCommand(CheckInRequest Request)
    : IRequest<Result<ReceptionTicketResponse>>;

public record CallTicketCommand(Guid TicketId)
    : IRequest<Result<ReceptionTicketResponse>>;

public record SkipTicketCommand(Guid TicketId)
    : IRequest<Result<ReceptionTicketResponse>>;

public record CancelTicketCommand(Guid TicketId, string? Reason)
    : IRequest<Result<ReceptionTicketResponse>>;

// Queries
public record ListQueueQuery(Guid? RoomId, string? Status, DateOnly? Date)
    : IRequest<List<ReceptionTicketResponse>>;

public record GetTicketQuery(Guid TicketId)
    : IRequest<Result<ReceptionTicketResponse>>;

public record ListRoomsQuery() : IRequest<List<RoomResponse>>;

public record GetReceptionStatsQuery(DateOnly? Date) : IRequest<ReceptionStatsDto>;
