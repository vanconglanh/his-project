namespace ProDiabHis.Application.EMR;

/// <summary>Export EMR as PDF (QuestPDF) with embedded signature image.</summary>
public interface IEmrPdfExporter
{
    Task<byte[]> ExportAsync(EmrPdfContext context, CancellationToken ct = default);
}

public record EmrPdfContext(
    Guid EncounterId,
    string PatientName,
    string? DoctorName,
    DateTime EncounterDate,
    string ContentHtml,
    bool IsSigned,
    DateTime? SignedAt,
    string? SignerName,
    string? CertSerial);
