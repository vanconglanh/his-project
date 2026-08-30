using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabResults.Ocr;
using ProDiabHis.Application.LegacyImport;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>
/// Impl <see cref="ILabOcrTextProvider"/> — KHONG viet lai engine doc file. Chi dieu phoi giua 2
/// ha tang OCR da co trong du an:
///   - PDF (application/pdf hoac .pdf) -> <see cref="IPdfTextExtractor"/> (PdfPig text layer + fallback
///     render tung trang roi OCR anh — dung cho ca phieu KQ scan thanh PDF).
///   - Anh (png/jpg/webp/bmp/tiff)     -> <see cref="IOcrTextProvider"/> (Tesseract "vie+eng").
/// Parser ngu canh xet nghiem nam o Application (LabResultOcrParser) — provider nay chi lo LAY TEXT.
/// </summary>
public class LabOcrTextProvider : ILabOcrTextProvider
{
    private readonly IPdfTextExtractor _pdf;
    private readonly IOcrTextProvider _ocr;
    private readonly ILogger<LabOcrTextProvider> _logger;

    public LabOcrTextProvider(IPdfTextExtractor pdf, IOcrTextProvider ocr, ILogger<LabOcrTextProvider> logger)
    { _pdf = pdf; _ocr = ocr; _logger = logger; }

    public Task<Result<string>> ExtractTextAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct)
    {
        var safeName = fileName ?? string.Empty;
        var mime = (contentType ?? string.Empty).ToLowerInvariant();
        var name = safeName.ToLowerInvariant();
        var isPdf = mime == "application/pdf" || name.EndsWith(".pdf");

        if (isPdf)
        {
            _logger.LogInformation("Lab OCR: doc PDF {FileName} qua IPdfTextExtractor", safeName);
            return _pdf.ExtractTextAsync(fileBytes, safeName, ct);
        }

        _logger.LogInformation("Lab OCR: doc anh {FileName} ({Mime}) qua Tesseract", safeName, mime);
        return _ocr.ExtractTextAsync(fileBytes, safeName, ct);
    }
}
