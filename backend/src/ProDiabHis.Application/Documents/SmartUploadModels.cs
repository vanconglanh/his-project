using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.InBody;
using ProDiabHis.Application.LabResults.Ocr;

namespace ProDiabHis.Application.Documents;

/// <summary>Ket qua upload thong minh: phan loai + (neu tu dong route duoc) ket qua tu luong tuong ung.</summary>
public record SmartUploadResponse(
    DocumentClassifyResult Classification,
    bool RequiresEncounter,
    string? RawTextPreview,
    InBodyReportResponse? InBody,
    LabOcrExtractResponse? LabResult);

/// <summary>
/// Upload 1 lan -> OCR (tai dung ILabOcrTextProvider) -> phan loai (IDocumentClassifier) ->
/// dieu phoi goi lai command/handler cua dung luong (InBody / LabResult). KHONG viet lai
/// logic 3 luong da co, chi orchestrate.
/// </summary>
public record SmartUploadCommand(Guid PatientId, Guid? EncounterId, byte[] FileBytes, string FileName, string ContentType)
    : IRequest<Result<SmartUploadResponse>>;
