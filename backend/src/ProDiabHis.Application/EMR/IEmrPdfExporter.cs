using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.EMR;

/// <summary>Export EMR as PDF (QuestPDF) with embedded signature image.</summary>
public interface IEmrPdfExporter
{
    Task<byte[]> ExportAsync(EmrPdfContext context, CancellationToken ct = default);
}

public record EmrDiagnosisLine(string Icd10Code, string Name);

public record EmrVitalsDto(
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRate,
    int? BpSystolic,
    int? BpDiastolic,
    int? Spo2Percent,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? GlucoseMgDl);

public record EmrPdfContext(
    Guid EncounterId,
    string PatientName,
    string? DoctorName,
    DateTime EncounterDate,
    string ContentHtml,
    bool IsSigned,
    DateTime? SignedAt,
    string? SignerName,
    string? CertSerial,
    LetterheadDto? Letterhead = null,
    string? PatientCode = null,
    string? PatientGender = null,
    DateOnly? PatientDob = null,
    string? PatientAddress = null,
    string? ChiefComplaint = null,
    string? ReasonForVisit = null,
    EmrDiagnosisLine? PrimaryDiagnosis = null,
    IReadOnlyList<EmrDiagnosisLine>? SecondaryDiagnoses = null,
    EmrVitalsDto? Vitals = null,
    string? EncounterNo = null);
