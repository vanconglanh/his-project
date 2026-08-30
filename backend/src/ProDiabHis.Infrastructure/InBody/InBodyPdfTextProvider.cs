using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.InBody;
using UglyToad.PdfPig;

namespace ProDiabHis.Infrastructure.InBody;

/// <summary>
/// Implementation MVP cua <see cref="IInBodyDataProvider"/> — chi doc TEXT LAYER cua PDF bang
/// UglyToad.PdfPig, KHONG lam OCR anh. Neu file PDF may InBody xuat ra la dang scan anh (khong
/// co text layer nhung/it text trich duoc), ket qua se rong hoac thieu hau het field — dieu
/// duong phai nhap tay. Day la GIOI HAN DA BIET cua MVP (xem docs/prd/inbody-ocr-20260830.md).
///
/// Dinh huong tuong lai: khi tich hop thang API may InBody, them class InBodyApiProvider moi
/// implement cung interface nay, dang ky lai trong DI — khong doi Application/API contract.
/// </summary>
public class InBodyPdfTextProvider : IInBodyDataProvider
{
    private readonly ILogger<InBodyPdfTextProvider> _logger;

    public InBodyPdfTextProvider(ILogger<InBodyPdfTextProvider> logger)
    {
        _logger = logger;
    }

    public Task<Result<InBodyReportData>> ExtractAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        try
        {
            using var pdf = PdfDocument.Open(fileStream);
            var textBuilder = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            var rawText = textBuilder.ToString();
            var data = InBodyReportParser.Parse(rawText);
            return Task.FromResult(Result<InBodyReportData>.Success(data));
        }
        catch (Exception ex)
        {
            // Khong throw ra ngoai — coi nhu extract that bai (vd file khong phai PDF hop le,
            // file hong). Log khong dau de tranh loi encoding (quy uoc CLAUDE.md).
            _logger.LogWarning(ex, "InBody PDF extract that bai cho file {FileName}", fileName);
            return Task.FromResult(Result<InBodyReportData>.Failure(
                "INBODY_EXTRACT_FAILED", "Không đọc được file PDF, vui lòng kiểm tra lại file hoặc nhập tay"));
        }
    }
}
