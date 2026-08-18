using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reception.Reassign;

// ────────────── Request / Response ──────────────

/// <summary>[G05] Yeu cau dieu phoi luot kham (doi bac si / doi phong).</summary>
public record ReassignTicketRequest(
    Guid? DoctorId,
    Guid? RoomId,
    string Reason,
    bool AcknowledgeScheduleWarning = false);

public record ReassignWarningDto(string Code, string Message);

public record ReassignPersonDto(Guid Id, string? FullName);

public record ReassignRoomDto(Guid Id, string? Code, string? Name);

public record ReassignTicketResponse(
    Guid TicketId,
    string TicketNo,
    DateOnly? TicketDate,
    string Status,
    Guid? EncounterId,
    ReassignPersonDto? Doctor,
    ReassignRoomDto? Room,
    int ReassignCount,
    string ChangeType,
    Guid ReassignmentId,
    DateTime ChangedAt,
    List<ReassignWarningDto> Warnings);

public record TicketReassignmentHistoryDto(
    Guid Id,
    Guid TicketId,
    Guid? EncounterId,
    string ChangeType,
    string TicketStatusAtChange,
    Guid? FromDoctorId,
    string? FromDoctorName,
    Guid? ToDoctorId,
    string? ToDoctorName,
    Guid? FromRoomId,
    string? FromRoomName,
    Guid? ToRoomId,
    string? ToRoomName,
    string Reason,
    bool ScheduleWarningFlag,
    string? WarningMessage,
    DateTime ChangedAt,
    Guid? ChangedBy,
    string? ChangedByName);

// ────────────── Command / Query ──────────────

public record ReassignTicketCommand(Guid TicketId, ReassignTicketRequest Request)
    : IRequest<Result<ReassignTicketResponse>>;

public record ListTicketReassignmentsQuery(Guid TicketId)
    : IRequest<Result<List<TicketReassignmentHistoryDto>>>;
