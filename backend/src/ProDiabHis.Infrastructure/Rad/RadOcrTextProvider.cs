using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LegacyImport;
using ProDiabHis.Application.RadResults.Ocr;

namespace ProDiabHis.Infrastructure.Rad;

/// <summary>
/// Impl <see cref="IRadOcrTextProvider"/> — KHONG viet lai engine doc file. Chi dieu phoi giua 2
/// ha tang OCR da co trong du an (giong LabOcrTextProvider):
///   - PDF (application/pdf hoac .pdf) -> <see cref="IPdfTextExtractor"/> (PdfPig text layer + fallback
///     render tung trang roi OCR — dung cho ca phieu CDHA scan thanh PDF).
///   - Anh (png/jpg/webp/bmp/tiff)     -> <see cref="IOcrTextProvider"/> (Tesseract "vie+eng").
/// Parser ngu canh CDHA nam o Application (RadResultOcrParser) — provider nay chi lo LAY TEXT.
/// </summary>
public class RadOcrTextProvider : IRadOcrTextProvider
{
    private readonly IPdfTextExtractor _pdf;
    private readonly IOcrTextProvider _ocr;
    private readonly ILogger<RadOcrTextProvider> _logger;

    public RadOcrTextProvider(IPdfTextExtractor pdf, IOcrTextProvider ocr, ILogger<RadOcrTextProvider> logger)
    { _pdf = pdf; _ocr = ocr; _logger = logger; }

    public Task<Result<string>> ExtractTextAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct)
    {
        var safeName = fileName ?? string.Empty;
        var mime = (contentType ?? string.Empty).ToLowerInvariant();
        var name = safeName.ToLowerInvariant();
        var isPdf = mime == "application/pdf" || name.EndsWith(".pdf");

        if (isPdf)
        {
            _logger.LogInformation("Rad OCR: doc PDF {FileName} qua IPdfTextExtractor", safeName);
            return _pdf.ExtractTextAsync(fileBytes, safeName, ct);
        }

        _logger.LogInformation("Rad OCR: doc anh {FileName} ({Mime}) qua Tesseract", safeName, mime);
        return _ocr.ExtractTextAsync(fileBytes, safeName, ct);
    }
}
