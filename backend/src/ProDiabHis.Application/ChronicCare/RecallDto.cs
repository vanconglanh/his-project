namespace ProDiabHis.Application.ChronicCare;

public record RecallItem(
    Guid Id,
    Guid PatientId,
    string PatientCode,
    string PatientFullName,
    string? Phone,
    string RecallType,
    DateOnly? DueDate,
    string Priority,
    string Status,
    string? Channel,
    string? Note,
    DateTime? ContactedAt,
    DateTime CreatedAt);

public record CarePathwayTargetDto(string Param, string TargetOp, decimal TargetValue, string? Unit, string? Note);
