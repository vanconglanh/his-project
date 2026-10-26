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
    private readonly ILogger<InBodyPdfTextProvider> _logger;

    public InBodyPdfTextProvider(IPdfTextExtractor pdfText, ILogger<InBodyPdfTextProvider> logger)
    {
        _pdfText = pdfText;
        _logger = logger;
    }

    public async Task<Result<InBodyReportData>> ExtractAsync(Stream fileStream, string fileName, CancellationToken ct)
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

            var textResult = await _pdfText.ExtractTextAsync(bytes, fileName, ct);
            if (!textResult.IsSuccess)
            {
                _logger.LogWarning("InBody: lay text that bai cho {FileName}: {Err}", fileName, textResult.ErrorMessage);
                return Result<InBodyReportData>.Failure(
                    "INBODY_EXTRACT_FAILED", "Không đọc được file PDF, vui lòng kiểm tra lại file hoặc nhập tay");
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
