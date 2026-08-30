using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LegacyImport;
using Tesseract;

namespace ProDiabHis.Infrastructure.Ocr;

/// <summary>
/// OCR anh scan bang Tesseract (charlesw.Tesseract 5.2.0), ngon ngu "vie+eng".
/// tessdata: doc tu config "Ocr:TessdataPath", fallback "tessdata" canh AppContext.BaseDirectory
/// (ProDiabHis.Api.csproj copy backend/tessdata vao output - xem ghi chu trong .csproj).
///
/// Ly do tao 1 TesseractEngine moi cho MOI lan goi ExtractTextAsync (thay vi giu 1 engine dung chung):
/// TesseractEngine KHONG thread-safe, batch nay chay tuan tu tung anh trong 1 Hangfire job (khong
/// song song), overhead khoi tao (~vai chuc ms/anh) chap nhan duoc so voi rui ro loi state giua cac
/// lan Process() neu dung chung 1 engine. Don gian + an toan hon la pool engine cho use-case "chay
/// 1 lan migration du lieu cu" nay.
/// </summary>
public class TesseractOcrProvider : IOcrTextProvider
{
    private readonly string _tessdataPath;
    private readonly ILogger<TesseractOcrProvider> _logger;

    public TesseractOcrProvider(IConfiguration configuration, ILogger<TesseractOcrProvider> logger)
    {
        _logger = logger;
        var configured = configuration["Ocr:TessdataPath"];
        _tessdataPath = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    public Task<Result<string>> ExtractTextAsync(byte[] imageBytes, string fileName, CancellationToken ct)
    {
        try
        {
            using var engine = new TesseractEngine(_tessdataPath, "vie+eng", EngineMode.Default);
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);
            var text = page.GetText();
            var confidence = page.GetMeanConfidence();
            _logger.LogInformation("TesseractOcrProvider: OCR xong file={File}, confidence={Conf}", fileName, confidence);
            return Task.FromResult(Result<string>.Success(text ?? string.Empty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TesseractOcrProvider: loi OCR file={File}", fileName);
            return Task.FromResult(Result<string>.Failure("LEGACY_IMPORT_OCR_FAILED", "Không đọc được nội dung ảnh scan"));
        }
    }
}
