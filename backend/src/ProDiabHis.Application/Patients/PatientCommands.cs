using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

// ── Guardian (nguoi giam ho) ──
public record GuardianRequest(
    string FullName,
    string Relationship,
    string Phone,
    string? IdNumber);

// GuardianResponse dung chung duoc dinh nghia trong PatientDto.cs (dung cho GET /patients/{id}/guardians)

// ── Create ──
public record CreatePatientRequest(
    string FullName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? IdNumber,
    string? Phone,
    string? Email,
    AddressDto? Address,
    string? Occupation,
    string? Ethnicity,
    string? BloodType,
    DateOnly? IdCardIssuedDate,
    string? IdCardIssuedPlace,
    string Nationality = "VN",
    string PatientType = "SERVICE",
    string? MaritalStatus = null,
    string? VisitType = "FIRST_VISIT",
    List<GuardianRequest>? Guardians = null,
    bool ConfirmCreateDespiteDuplicate = false);

/// <summary>Ho so benh nhan nghi trung, tra ve de le tan doi chieu truoc khi xac nhan tao moi</summary>
public record PatientDuplicateCandidate(
    Guid Id,
    string Code,
    string FullName,
    DateOnly? DateOfBirth,
    string? Phone,
    string MatchReason);

/// <summary>
/// Ket qua tao benh nhan: neu PossibleDuplicate = true va Patient = null nghia la
/// he thong PHAT HIEN nghi trung va CHUA tao ho so, cho le tan xac nhan lai bang
/// cach goi lai voi ConfirmCreateDespiteDuplicate = true.
/// </summary>
public record CreatePatientResult(
    PatientResponse? Patient,
    bool PossibleDuplicate,
    List<PatientDuplicateCandidate> DuplicateCandidates);

public record CreatePatientCommand(CreatePatientRequest Request)
    : IRequest<Result<CreatePatientResult>>;

// ── Update ──
public record UpdatePatientRequest(
    string FullName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? IdNumber,
    string? Phone,
    string? Email,
    AddressDto? Address,
    string? Occupation,
    string? Ethnicity,
    string? BloodType,
    string? Status,
    DateOnly? IdCardIssuedDate = null,
    string? IdCardIssuedPlace = null,
    string? Nationality = null,
    string? PatientType = null,
    string? MaritalStatus = null,
    string? VisitType = null);

public record UpdatePatientCommand(Guid PatientId, UpdatePatientRequest Request)
    : IRequest<Result<PatientResponse>>;

// ── Delete ──
public record DeletePatientCommand(Guid PatientId)
    : IRequest<Result<bool>>;

// ── Upload Avatar ──
public record UploadAvatarCommand(Guid PatientId, Stream FileStream, string FileName, string ContentType, long SizeBytes)
    : IRequest<Result<string>>;

// ── Reception Note ──
public record UpdateReceptionNoteCommand(Guid PatientId, string ReceptionNote)
    : IRequest<Result<PatientResponse>>;

// ── Allergy ──
public record AddAllergyRequest(
    string Allergen,
    string? Reaction,
    string Severity,
    DateOnly? OnsetDate,
    string? Note);

public record AddAllergyCommand(Guid PatientId, AddAllergyRequest Request)
    : IRequest<Result<AllergyResponse>>;

public record DeleteAllergyCommand(Guid PatientId, Guid AllergyId)
    : IRequest<Result<bool>>;

// ── Insurance ──
public record InsuranceRequest(
    string Type,
    string CardNo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string? HospitalCode,
    int? CoveragePercent);

public record AddInsuranceCommand(Guid PatientId, InsuranceRequest Request)
    : IRequest<Result<InsuranceResponse>>;

public record UpdateInsuranceCommand(Guid PatientId, Guid InsuranceId, InsuranceRequest Request)
    : IRequest<Result<InsuranceResponse>>;

public record DeleteInsuranceCommand(Guid PatientId, Guid InsuranceId)
    : IRequest<Result<bool>>;

// ── Emergency Contact ──
public record EmergencyContactRequest(
    string FullName,
    string Relationship,
    string Phone,
    string? Address);

public record AddEmergencyContactCommand(Guid PatientId, EmergencyContactRequest Request)
    : IRequest<Result<EmergencyContactResponse>>;

public record UpdateEmergencyContactCommand(Guid PatientId, Guid ContactId, EmergencyContactRequest Request)
    : IRequest<Result<EmergencyContactResponse>>;

public record DeleteEmergencyContactCommand(Guid PatientId, Guid ContactId)
    : IRequest<Result<bool>>;

// ── Consent ──
public record AddConsentRequest(
    string ConsentType,
    string? SignedBy,
    Guid? DocumentFileId);

public record AddConsentCommand(Guid PatientId, AddConsentRequest Request)
    : IRequest<Result<ConsentResponse>>;
