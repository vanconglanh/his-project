namespace ProDiabHis.Application.Branches;

public record InternalReferralDto(
    int Id,
    int TenantId,
    string PatientId,
    string? PatientName,
    int SourceBranchId,
    string? SourceBranchName,
    int TargetBranchId,
    string? TargetBranchName,
    string? EncounterId,
    Guid? ReferringDoctorId,
    string? Reason,
    string Status,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateInternalReferralRequest(
    string PatientId,
    int TargetBranchId,
    string? EncounterId,
    string? Reason,
    string? Note);

public record UpdateInternalReferralStatusRequest(string Status, string? Note);
