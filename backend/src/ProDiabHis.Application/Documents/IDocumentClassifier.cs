namespace ProDiabHis.Application.Documents;

// ═══════════════════════════════════════════════════════════════════════════
// Bo phan loai tai lieu tu dong — dat PHIA TRUOC 3 luong OCR da co (InBody,
// LabResult, LegacyImport). Chi doc text da OCR (tai dung ILabOcrTextProvider),
// KHONG tu viet engine OCR moi. Thuan, khong I/O truc tiep (tru
// IPendingLabTestsProvider) -> de unit test.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Loai tai lieu. Serialize thanh CHUOI ("InBody"/"LabResult"/"Legacy"/"Unknown") thay vi so
/// nguyen — de FE so khop on dinh, khong vo khi reorder enum (project chua bat
/// JsonStringEnumConverter toan cuc).
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum DocumentType
{
    InBody,
    LabResult,
    RadResult,
    Legacy,
    Unknown
}

/// <summary>Input cho bo phan loai — text da OCR + encounter (neu co) de doi chieu pending lab.</summary>
public record DocumentClassifyInput(string OcrText, Guid? EncounterId);

/// <summary>1 ung vien loai tai lieu kem diem tin cay va bang chung.</summary>
public record DocumentTypeCandidate(DocumentType Type, double Score, IReadOnlyList<string> Evidence);

/// <summary>Ket qua phan loai cuoi cung.</summary>
public record DocumentClassifyResult(
    DocumentType Type,
    double Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<DocumentTypeCandidate> Candidates);

public interface IDocumentClassifier
{
    Task<DocumentClassifyResult> ClassifyAsync(DocumentClassifyInput input, CancellationToken ct);
}

/// <summary>
/// Nguon lay danh sach chi dinh XN dang cho ket qua cua 1 encounter, dung de doi chieu
/// voi noi dung OCR khi phan loai tai lieu la LabResult. Impl o Infrastructure (Dapper),
/// interface dat o Application de DocumentClassifierService thuan & unit-test duoc.
/// </summary>
public interface IPendingLabTestsProvider
{
    Task<IReadOnlyList<(Guid LabOrderItemId, string TestCode, string TestName)>> GetPendingAsync(
        Guid encounterId, CancellationToken ct);
}

/// <summary>
/// Nguon lay danh sach chi dinh CDHA (RadOrder) dang cho ket qua cua 1 encounter, dung de
/// doi chieu (boost do tin cay) khi phan loai tai lieu la RadResult. Impl o Infrastructure
/// (Dapper), interface dat o Application de DocumentClassifierService thuan & unit-test duoc.
/// </summary>
public interface IPendingRadOrdersProvider
{
    Task<bool> HasPendingAsync(Guid encounterId, CancellationToken ct);
}
