using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.RadResults.Ocr;

/// <summary>
/// Nguon trich text tu file phieu ket qua CDHA upload (PDF hoac anh scan). KHONG viet lai engine
/// doc file — impl (RadOcrTextProvider) chi dieu phoi giua 2 ha tang da co (giong LabOcrTextProvider):
///   - PDF -> IPdfTextExtractor (PdfPig text layer + fallback OCR anh tung trang)
///   - Anh -> IOcrTextProvider  (Tesseract "vie+eng")
/// Tach interface o Application de handler khong phu thuoc truc tiep engine ha tang, va de unit test.
/// </summary>
public interface IRadOcrTextProvider
{
    Task<Result<string>> ExtractTextAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct);
}
