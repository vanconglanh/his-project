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
/// <param name="DiagnosisIcd10">
/// FR-804: chan doan/ly do kham (ICD-10) NEU benh nhan/le tan da biet luc dat lich (thuong chua co).
/// Neu duoc truyen va khong nam trong danh muc diab_his_tel_allowed_icd10 dang active -> CANH BAO MEM
/// (khong chan dat lich, tra ve o Icd10Warning cua response).
/// </param>
public record CreateTelehealthAppointmentRequest(
    Guid DoctorUserId,
    Guid HisServiceId,
    DateTime ScheduledStart,
    string? Symptom,
    string? DiagnosisIcd10 = null);

public record TelehealthSessionResponse(
    Guid Id,
    Guid PatientId,
    Guid? DoctorUserId,
    string DocosanStatus,
    string HisStatus,
    DateTime ScheduledStart,
    DateTime? ScheduledEnd,
    string? PaymentStatus,
    DateTime? LastSyncedAt,
    /// <summary>FR-804: canh bao mem neu DiagnosisIcd10 nam ngoai danh muc duoc phep tu van tu xa. Null = khong co canh bao.</summary>
    string? Icd10Warning = null);

// ── FR-804: Danh muc ICD-10 duoc phep tu van tu xa (Admin CRUD) ──
public record AllowedIcd10Request(string Icd10Code, string Icd10Name, bool IsActive, string? Note);

public record AllowedIcd10Response(
    Guid Id,
    string Icd10Code,
    string Icd10Name,
    bool IsActive,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

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
