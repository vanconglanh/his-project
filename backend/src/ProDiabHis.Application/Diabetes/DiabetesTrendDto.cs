namespace ProDiabHis.Application.Diabetes;

public record TrendPoint(DateTime AssessedAt, decimal? Hba1c, decimal? FastingGlucose, decimal? Egfr,
    int? BpSystolic, int? BpDiastolic, decimal? Bmi);

public record CarePathwayTargetItem(string Param, string TargetOp, decimal TargetValue, string? Unit);

public record DiabetesTrajectoryResponse(
    Guid PatientId,
    IReadOnlyList<TrendPoint> Series,
    IReadOnlyList<CarePathwayTargetItem> Targets);

public record DeteriorationFlagDto(string Code, string Message, string Severity);

public record DeteriorationFlagsResponse(Guid PatientId, IReadOnlyList<DeteriorationFlagDto> Flags);

public record RiskListItem(
    Guid PatientId,
    string PatientCode,
    string PatientFullName,
    string? Phone,
    string RiskLevel,
    decimal RiskScore,
    decimal? LatestHba1c,
    decimal? LatestEgfr,
    int? LatestBpSys,
    int? LatestBpDia,
    string? Hba1cTrend,
    DateTime? LastVisitAt,
    DateTime ComputedAt);
