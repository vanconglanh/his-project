namespace ProDiabHis.Application.Reception;

public record PatientSummaryDto(
    Guid Id,
    string Code,
    string FullName,
    DateOnly? Dob,
    string? Gender,
    string? BhytSummary);

public record ServicePackageDto(string Id, string Name, decimal Price);

public record ReceptionTicketResponse(
    Guid Id,
    int? TenantId,
    Guid PatientId,
    PatientSummaryDto? PatientSummary,
    string TicketNo,
    Guid RoomId,
    string? RoomName,
    Guid? DoctorId,
    string? DoctorName,
    List<ServicePackageDto> ServicePackages,
    string? ReasonForVisit,
    string Status,
    string Priority,
    DateTime CheckedInAt,
    DateTime? CalledAt,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    Guid? CreatedBy,
    string? Note);

public record RoomResponse(
    string Id,
    string Name,
    string RoomCode,
    DoctorOnDutyDto? OnDutyDoctor,
    int MaxPerDay,
    int CurrentWaiting);

public record DoctorOnDutyDto(string Id, string FullName);

public record ReceptionStatsDto(
    DateOnly Date,
    int TotalCheckedIn,
    int Waiting,
    int InProgress,
    int Done,
    int Skipped,
    int Cancelled,
    double AvgWaitMinutes);
