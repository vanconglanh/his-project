namespace ProDiabHis.Application.VitalSigns;

public record VitalSignsRequest(
    DateTime? RecordedAt,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRate,
    int? BpSystolic,
    int? BpDiastolic,
    int? Spo2Percent,
    decimal? WeightKg,
    decimal? HeightCm,
    int? PainScale,
    decimal? GlucoseMgDl,
    string? Note);

public record VitalSignsResponse(
    Guid Id,
    Guid EncounterId,
    DateTime RecordedAt,
    string? RecordedBy,
    string? RecordedByName,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRate,
    int? BpSystolic,
    int? BpDiastolic,
    int? Spo2Percent,
    decimal? WeightKg,
    decimal? HeightCm,
    int? PainScale,
    decimal? GlucoseMgDl,
    string? Note,
    decimal? Bmi,
    int RecordSequence,
    DateTime CreatedAt);
