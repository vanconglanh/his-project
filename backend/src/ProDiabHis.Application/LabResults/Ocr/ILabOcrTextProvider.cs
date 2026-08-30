using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LabResults.Ocr;

/// <summary>
/// Nguon trich text tu file ket qua xet nghiem upload (PDF hoac anh scan). KHONG viet lai engine
/// doc file — impl (LabOcrTextProvider) chi dieu phoi giua 2 ha tang da co:
///   - PDF  -> IPdfTextExtractor (PdfPig text layer + fallback OCR anh tung trang)
///   - Anh  -> IOcrTextProvider  (Tesseract "vie+eng")
/// Tach interface o Application de handler khong phu thuoc truc tiep engine ha tang, va de unit test.
/// </summary>
public interface ILabOcrTextProvider
{
    Task<Result<string>> ExtractTextAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct);
}
