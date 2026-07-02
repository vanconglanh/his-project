namespace ProDiabHis.Application.Patients;

public record AddressDto(
    string? ProvinceCode,
    string? DistrictCode,
    string? WardCode,
    string? Street);

public record PatientResponse(
    Guid Id,
    int TenantId,
    string Code,
    string FullName,
    string? Gender,
    DateOnly? DateOfBirth,
    int? Age,
    string? IdNumber,
    string? Phone,
    string? Email,
    AddressDto? Address,
    string? Occupation,
    string? Ethnicity,
    string? AvatarUrl,
    string? ReceptionNote,
    string? BloodType,
    string? AllergiesSummary,
    string? BhytCardNo,
    DateOnly? BhytValidTo,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateOnly? IdCardIssuedDate,
    string? IdCardIssuedPlace,
    string Nationality,
    string PatientType,
    string? MaritalStatus,
    string? VisitType);

public record EncounterSummaryDto(
    Guid Id,
    string EncounterNo,
    DateTime EncounterDate,
    string? DoctorName,
    string? RoomName,
    string? ChiefComplaint,
    List<string> DiagnosisIcd10,
    string Status);

public record AllergyResponse(
    Guid Id,
    int PatientId,
    string Allergen,
    string? Reaction,
    string Severity,
    DateOnly? OnsetDate,
    string? Note,
    DateTime CreatedAt);

public record InsuranceResponse(
    Guid Id,
    int PatientId,
    string Type,
    string? CardNo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string? HospitalCode,
    int? CoveragePercent,
    DateTime CreatedAt);

public record EmergencyContactResponse(
    Guid Id,
    int PatientId,
    string FullName,
    string Relationship,
    string Phone,
    string? Address);

public record ConsentResponse(
    Guid Id,
    int PatientId,
    string ConsentType,
    DateTime SignedAt,
    string? SignedBy,
    string? DocumentUrl,
    DateTime? RevokedAt);
