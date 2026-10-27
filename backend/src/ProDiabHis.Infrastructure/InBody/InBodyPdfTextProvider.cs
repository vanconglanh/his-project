using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.InBody;
using ProDiabHis.Application.LegacyImport;

namespace ProDiabHis.Infrastructure.InBody;

/// <summary>
/// Implementation cua <see cref="IInBodyDataProvider"/>. KHONG tu doc PDF nua ma tai dung
/// <see cref="IPdfTextExtractor"/> (PdfPig text layer + FALLBACK render tung trang roi OCR
/// Tesseract) — vi phieu may InBody270 thuong duoc in/scan thanh PDF ANH KHONG CO TEXT LAYER
/// (BM-InBody OCR). Truoc day provider chi doc text layer bang PdfPig nen cac phieu scan tra ve
/// text rong -> moi o chi so trong, dieu duong phai nhap tay toan bo.
///
/// Sau khi co text (text layer hoac OCR), giao cho <see cref="InBodyReportParser"/> trich chi so.
/// </summary>
public class InBodyPdfTextProvider : IInBodyDataProvider
{
    private readonly IPdfTextExtractor _pdfText;
    private readonly IOcrTextProvider _ocr;
    private readonly ILogger<InBodyPdfTextProvider> _logger;

    public InBodyPdfTextProvider(IPdfTextExtractor pdfText, IOcrTextProvider ocr, ILogger<InBodyPdfTextProvider> logger)
    {
        _pdfText = pdfText;
        _ocr = ocr;
        _logger = logger;
    }

    public async Task<Result<InBodyReportData>> ExtractAsync(Stream fileStream, string fileName, string? contentType, CancellationToken ct)
    {
        try
        {
            byte[] bytes;
            if (fileStream is MemoryStream ms0)
            {
                bytes = ms0.ToArray();
            }
            else
            {
                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }

            // BM-120: phieu InBody co the la ANH (jpeg/png) -> OCR Tesseract truc tiep;
            // PDF (text layer hoac scan) -> IPdfTextExtractor (co fallback render->OCR). Cung khuon LabOcrTextProvider.
            var mime = (contentType ?? string.Empty).ToLowerInvariant();
            var name = (fileName ?? string.Empty).ToLowerInvariant();
            var isPdf = mime == "application/pdf" || name.EndsWith(".pdf")
                || (string.IsNullOrEmpty(mime) && !name.EndsWith(".jpg") && !name.EndsWith(".jpeg")
                    && !name.EndsWith(".png") && !name.EndsWith(".webp"));

            var textResult = isPdf
                ? await _pdfText.ExtractTextAsync(bytes, fileName, ct)
                : await _ocr.ExtractTextAsync(bytes, fileName, ct);
            if (!textResult.IsSuccess)
            {
                _logger.LogWarning("InBody: lay text that bai cho {FileName} (pdf={IsPdf}): {Err}", fileName, isPdf, textResult.ErrorMessage);
                return Result<InBodyReportData>.Failure(
                    "INBODY_EXTRACT_FAILED", "Không đọc được file, vui lòng kiểm tra lại file hoặc nhập tay");
            }

            var data = InBodyReportParser.Parse(textResult.Value);
            return Result<InBodyReportData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InBody PDF extract that bai cho file {FileName}", fileName);
            return Result<InBodyReportData>.Failure(
                "INBODY_EXTRACT_FAILED", "Không đọc được file PDF, vui lòng kiểm tra lại file hoặc nhập tay");
        }
    }
}
