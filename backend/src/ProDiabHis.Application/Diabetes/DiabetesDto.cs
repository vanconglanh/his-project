namespace ProDiabHis.Application.Diabetes;

public record ComplicationsDto(
    bool? Retinopathy,
    bool? Neuropathy,
    bool? Nephropathy,
    bool? Cad,
    bool? Pad,
    bool? DiabeticFoot);

public record TreatmentTargetDto(
    decimal? Hba1cTarget,
    decimal? LdlTarget,
    string? BpTarget);

public record DiabetesAssessmentRequest(
    decimal? Hba1c,
    decimal? FastingGlucose,
    decimal? PostprandialGlucose,
    decimal? RandomGlucose,
    decimal? Egfr,
    decimal? SerumCreatinine,
    decimal? UrineAcr,
    int? BpSystolic,
    int? BpDiastolic,
    decimal? Bmi,
    decimal? WaistCircumference,
    string? DiabetesType,
    ComplicationsDto? Complications,
    TreatmentTargetDto? TreatmentTarget,
    string? Note);

public record DiabetesAssessmentResponse(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    decimal? Hba1c,
    decimal? FastingGlucose,
    decimal? PostprandialGlucose,
    decimal? RandomGlucose,
    decimal? Egfr,
    decimal? SerumCreatinine,
    decimal? UrineAcr,
    int? BpSystolic,
    int? BpDiastolic,
    decimal? Bmi,
    decimal? WaistCircumference,
    string? DiabetesType,
    ComplicationsDto? Complications,
    TreatmentTargetDto? TreatmentTarget,
    string? Note,
    DateTime AssessedAt,
    Guid? AssessedBy,
    string? AssessedByName);

public record DiabetesTemplateRequest(string Name, object? DefaultValues, IReadOnlyList<string>? Checklist);

public record DiabetesTemplateResponse(
    Guid Id,
    int? TenantId,
    string Name,
    object? DefaultValues,
    IReadOnlyList<string>? Checklist,
    bool IsSystem,
    DateTime CreatedAt);
