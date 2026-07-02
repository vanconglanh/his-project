using System.Text.Json.Serialization;

namespace ProDiabHis.Application.Encounters;

public record PatientSummaryDto(
    string FullName,
    int? YearOfBirth,
    string? Gender,
    string? Phone);

public record DiagnosisResponse(
    Guid Id,
    string Icd10Code,
    string Name,
    string Type,
    string? Note,
    DateTime CreatedAt);

public record EncounterResponse(
    Guid Id,
    int TenantId,
    string PatientId,
    PatientSummaryDto? PatientSummary,
    string? RoomId,
    string? DoctorId,
    string? DoctorName,
    string EncounterType,
    string? ReasonForVisit,
    string? ChiefComplaint,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    bool AlertOver12h,
    IReadOnlyList<DiagnosisResponse> Diagnoses,
    object? VitalSignsLatest,
    bool HasEmrSigned,
    bool HasPrescription,
    DateTime CreatedAt);

public record EncounterDetailResponse(
    Guid Id,
    int TenantId,
    string PatientId,
    PatientSummaryDto? PatientSummary,
    string? RoomId,
    string? DoctorId,
    string? DoctorName,
    string EncounterType,
    string? ReasonForVisit,
    string? ChiefComplaint,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    bool AlertOver12h,
    IReadOnlyList<DiagnosisResponse> Diagnoses,
    object? VitalSignsLatest,
    bool HasEmrSigned,
    bool HasPrescription,
    DateTime CreatedAt,
    IReadOnlyList<object> VitalSigns,
    IReadOnlyList<object> LabOrders,
    IReadOnlyList<object> RadOrders,
    IReadOnlyList<object> Prescriptions,
    EmrSummaryDto? EmrSummary);

public record EmrSummaryDto(Guid Id, DateTime? SignedAt, int Version);

public record TimelineEventDto(
    DateTime Timestamp,
    string EventType,
    string? Actor,
    string? ActorRole,
    Guid? RefId,
    string? Summary,
    object? Payload);

public record Over12hAlertDto(
    Guid EncounterId,
    string PatientName,
    string? DoctorName,
    DateTime StartedAt,
    double HoursOpen,
    DateTime? AlertSentAt);

public record PageMeta(int Page, int PageSize, int Total);
