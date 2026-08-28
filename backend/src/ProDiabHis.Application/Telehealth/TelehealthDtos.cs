namespace ProDiabHis.Application.Telehealth;

// ── FR-801: eligibility ──
public record TelehealthEligibilityResponse(
    bool Eligible,
    string? Reason,
    Guid? LastInPersonEncounterId,
    DateTime? LastInPersonEncounterDate,
    int RequiredWithinDays);

// ── Link tai khoan Docosan cho benh nhan Portal ──
public record LinkDocosanAccountRequest(string? Email, string? DisplayName, string? Gender);

public record LinkDocosanAccountResponse(bool Linked, int? DocosanUserId, DateTime? TokenExpiresAt);

// ── Dat lich telehealth ──
public record CreateTelehealthAppointmentRequest(
    Guid DoctorUserId,
    Guid HisServiceId,
    DateTime ScheduledStart,
    string? Symptom);

public record TelehealthSessionResponse(
    Guid Id,
    Guid PatientId,
    Guid? DoctorUserId,
    string DocosanStatus,
    string HisStatus,
    DateTime ScheduledStart,
    DateTime? ScheduledEnd,
    string? PaymentStatus,
    DateTime? LastSyncedAt);

public record TelehealthJoinLinkResponse(string JoinUrl, DateTime ExpiresAt);

// ── Admin: mapping dich vu telehealth ──
public record ServiceMappingRequest(
    Guid? HisServiceId,
    int DocosanServiceId,
    string DocosanServiceType,
    string? ServiceName,
    int DefaultQuantity,
    string Environment,
    bool IsActive);

public record ServiceMappingResponse(
    Guid Id,
    Guid? HisServiceId,
    int DocosanServiceId,
    string DocosanServiceType,
    string? ServiceName,
    int DefaultQuantity,
    string Environment,
    bool IsActive,
    DateTime CreatedAt);
