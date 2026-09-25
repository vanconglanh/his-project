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
    // BM-39 (Lark Bug-Feedback): truoc day EncounterResponse KHONG co RoomName - man Khu vuc benh
    // nhan (PatientIdentityCard) luon hien "Chua phan phong" du encounter da co RoomId that, trong
    // khi toolbar chuyen phong (RoomTransferSelect) lai tu resolve dung ten tu danh sach phong rieng
    // (useRooms) -> 2 noi hien 2 thong tin khac nhau cho cung 1 du lieu. Them RoomName de ca 2 noi
    // dung chung 1 nguon that tu BE.
    string? RoomName,
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
    // BR-26: chi nhanh cua luot kham (FE map branch_id -> ten tu danh sach chi nhanh da cache)
    int? BranchId = null);

public record EncounterDetailResponse(
    Guid Id,
    int TenantId,
    string PatientId,
    PatientSummaryDto? PatientSummary,
    string? RoomId,
    string? RoomName,
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
