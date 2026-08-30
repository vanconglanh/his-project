using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LegacyImport;

/// <summary>
/// Nguon OCR anh scan ho so giay cu. MVP: <c>TesseractOcrProvider</c> (Tesseract engine,
/// ngon ngu "vie+eng"). Theo khuon IInBodyDataProvider - de mo rong provider khac sau nay
/// (vd cloud OCR) ma khong doi contract.
/// </summary>
public interface IOcrTextProvider
{
    Task<Result<string>> ExtractTextAsync(byte[] imageBytes, string fileName, CancellationToken ct);
}
