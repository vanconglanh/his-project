using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LegacyImport;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ProDiabHis.Infrastructure.Ocr;

/// <summary>
/// Implementation cua <see cref="IPdfTextExtractor"/> - chien luoc 2 tang (xem XML doc interface):
///   Tang 1: PdfPig doc text layer.
///   Tang 2 (fallback khi tang 1 khong ra du text): PDFtoImage (PDFium native, chay duoc tren
///   Linux Docker khong can cai them goi he thong) render tung trang PDF thanh SKBitmap -> PNG
///   bytes -> tai su dung <see cref="IOcrTextProvider"/> (Tesseract) cho tung trang -> gop text.
///
/// Nguong quyet dinh "khong du text": tong so ky tu non-whitespace trich duoc tu tang 1 nho hon
/// <see cref="MinTextLayerChars"/> thi coi nhu PDF la anh scan thuan, chuyen sang tang 2.
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private const int MinTextLayerChars = 20;
    private const int MaxOcrPages = 30; // gioi han so trang render+OCR de tranh 1 file PDF qua lon lam treo job

    private readonly IOcrTextProvider _ocr;
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(IOcrTextProvider ocr, ILogger<PdfTextExtractor> logger)
    {
        _ocr = ocr;
        _logger = logger;
    }

    public async Task<Result<string>> ExtractTextAsync(byte[] pdfBytes, string fileName, CancellationToken ct)
    {
        string? textLayer;
        int pageCount;
        try
        {
            (textLayer, pageCount) = ReadTextLayer(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PdfTextExtractor: khong doc duoc PDF (file hong/khong hop le) {File}", fileName);
            return Result<string>.Failure("LEGACY_IMPORT_PDF_INVALID", "Không đọc được file PDF, file có thể bị hỏng hoặc không hợp lệ");
        }

        var nonWhitespaceChars = textLayer is null ? 0 : textLayer.Count(c => !char.IsWhiteSpace(c));
        if (nonWhitespaceChars >= MinTextLayerChars)
        {
            _logger.LogInformation("PdfTextExtractor: dung text layer cho {File}, {Chars} ky tu, {Pages} trang", fileName, nonWhitespaceChars, pageCount);
            return Result<string>.Success(textLayer!);
        }

        _logger.LogInformation("PdfTextExtractor: {File} khong co text layer du dung (chi {Chars} ky tu) - fallback render+OCR", fileName, nonWhitespaceChars);
        return await ExtractViaOcrFallbackAsync(pdfBytes, fileName, pageCount, ct);
    }

    private static (string Text, int PageCount) ReadTextLayer(byte[] pdfBytes)
    {
        using var pdf = PdfDocument.Open(pdfBytes);
        var builder = new System.Text.StringBuilder();
        var pageCount = 0;
        foreach (var page in pdf.GetPages())
        {
            pageCount++;
            builder.AppendLine(page.Text);
        }

        return (builder.ToString(), pageCount);
    }

    private async Task<Result<string>> ExtractViaOcrFallbackAsync(byte[] pdfBytes, string fileName, int knownPageCount, CancellationToken ct)
    {
        try
        {
#pragma warning disable CA1416 // PDFtoImage/PDFium ho tro Windows/Linux/macOS - du cho moi target deploy cua du an nay
            var pageCount = knownPageCount > 0 ? knownPageCount : Conversion.GetPageCount(pdfBytes);
            if (pageCount <= 0)
            {
                return Result<string>.Failure("LEGACY_IMPORT_PDF_EMPTY", "File PDF không có trang nào");
            }

            var pagesToProcess = Math.Min(pageCount, MaxOcrPages);
            var combined = new System.Text.StringBuilder();
            for (var i = 0; i < pagesToProcess; i++)
            {
                ct.ThrowIfCancellationRequested();
                using var bitmap = Conversion.ToImage(pdfBytes, (Index)i);
                using var pngData = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                var pngBytes = pngData.ToArray();

                var pageResult = await _ocr.ExtractTextAsync(pngBytes, $"{fileName}#page{i + 1}", ct);
                if (pageResult.IsSuccess)
                {
                    combined.AppendLine(pageResult.Value);
                }
                else
                {
                    _logger.LogWarning("PdfTextExtractor: OCR trang {Page} cua {File} that bai: {Err}", i + 1, fileName, pageResult.ErrorMessage);
                }
            }

            _logger.LogInformation("PdfTextExtractor: OCR fallback xong {File}, {Pages}/{Total} trang", fileName, pagesToProcess, pageCount);
#pragma warning restore CA1416
            return Result<string>.Success(combined.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PdfTextExtractor: loi render/OCR PDF {File}", fileName);
            return Result<string>.Failure("LEGACY_IMPORT_PDF_OCR_FAILED", "Không đọc được nội dung file PDF dạng ảnh scan");
        }
    }
}
