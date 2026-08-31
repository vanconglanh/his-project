using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.InBody;
using ProDiabHis.Application.LabResults.Ocr;
using ProDiabHis.Application.RadResults.Ocr;

namespace ProDiabHis.Application.Documents;

/// <summary>Ket qua upload thong minh: phan loai + (neu tu dong route duoc) ket qua tu luong tuong ung.</summary>
public record SmartUploadResponse(
    DocumentClassifyResult Classification,
    bool RequiresEncounter,
    string? RawTextPreview,
    InBodyReportResponse? InBody,
    LabOcrExtractResponse? LabResult,
    RadOcrExtractResponse? RadResult);

/// <summary>
/// Upload 1 lan -> OCR (tai dung ILabOcrTextProvider) -> phan loai (IDocumentClassifier) ->
/// dieu phoi goi lai command/handler cua dung luong (InBody / LabResult / RadResult). KHONG viet
/// lai logic cac luong da co, chi orchestrate.
/// </summary>
public record SmartUploadCommand(Guid PatientId, Guid? EncounterId, byte[] FileBytes, string FileName, string ContentType)
    : IRequest<Result<SmartUploadResponse>>;

// ─── Batch: nhieu file cung luc HOAC 1 file ZIP ─────────────────────────────────

/// <summary>Mot file dau vao trong batch smart-upload (da doc ra bytes).</summary>
public record SmartUploadFileInput(byte[] FileBytes, string FileName, string ContentType);

/// <summary>
/// Ket qua xu ly 1 file trong batch — MOI FILE DOC LAP, khong gop chung ket qua.
/// Success=false khi file do OCR loi / dinh dang khong doc duoc (cac file khac van xu ly binh thuong).
/// </summary>
public record SmartUploadItemResult(
    string FileName,
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    SmartUploadResponse? Result);

/// <summary>Ket qua batch: danh sach ket qua theo tung file (KHONG gop thanh 1).</summary>
public record SmartUploadBatchResponse(IReadOnlyList<SmartUploadItemResult> Items);

/// <summary>
/// Upload NHIEU file cung luc HOAC 1 file ZIP chua nhieu file -> moi file duoc OCR + phan loai
/// DOC LAP (goi lai <see cref="SmartUploadCommand"/> cho tung file), tra ket qua theo tung file.
/// Chi la lop vong lap/batch phia tren — KHONG sua doi logic xu ly-1-file da co.
/// </summary>
public record SmartUploadBatchCommand(Guid PatientId, Guid? EncounterId, IReadOnlyList<SmartUploadFileInput> Files)
    : IRequest<Result<SmartUploadBatchResponse>>;
