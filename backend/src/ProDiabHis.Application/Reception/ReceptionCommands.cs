using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reception;

public record CheckInRequest(
    Guid PatientId,
    Guid RoomId,
    List<string>? ServicePackageIds,
    string? ReasonForVisit,
    string? Note,
    string? Priority,
    // BM-33 (Lark): khi tiep don tu 1 lich hen CONFIRMED (xem ListTodayConfirmedAppointmentsQuery),
    // FE truyen kem AppointmentId de dong bo trang thai lich hen -> CHECKED_IN, tranh 2 model
    // Lich hen / Tiep don bi lech nhau. Null = tiep don thuong (khong tu lich hen), giu nguyen hanh vi cu.
    int? AppointmentId = null);

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
