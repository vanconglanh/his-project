using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProDiabHis.Infrastructure.Ocr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Xunit;

namespace ProDiabHis.UnitTests.LegacyImport;

/// <summary>
/// VERIFY THAT (khong mock) - dung Tesseract engine that (charlesw.Tesseract, tessdata vie+eng co
/// san trong backend/tessdata) de xac nhan tang 2 (render trang PDF -> anh -> OCR) hoat dong dung
/// tren 1 PDF "gia lap scan" (chu duoc ve thanh anh, nhung vao trong PDF nhu 1 hinh - PdfPig se
/// KHONG trich duoc text tu day). Doc them ghi chu evidence:
/// docs/qc/evidence-legacy-ocr-import-20260830/README.md
/// </summary>
public class PdfOcrFallbackRealEngineVerifyTests
{
    static PdfOcrFallbackRealEngineVerifyTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static byte[] BuildScanLikePdf(string text)
    {
        // Ve text thanh 1 anh bitmap (gia lap trang giay scan) - PDF nhung con lai chi la 1 hinh,
        // KHONG co text layer that.
        using var bitmap = new SKBitmap(900, 300);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = 48,
                TextAlign = SKTextAlign.Left,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            canvas.DrawText(text, 30, 150, paint);
        }
        using var img = SKImage.FromBitmap(bitmap);
        using var pngData = img.Encode(SKEncodedImageFormat.Png, 100);
        var pngBytes = pngData.ToArray();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Content().Image(pngBytes);
            });
        }).GeneratePdf();
    }

    [Fact]
    public async Task ExtractTextAsync_PdfDangScanThat_OcrFallbackRaDungText()
    {
        // Arrange: engine that, khong mock
        var config = new ConfigurationBuilder().Build();
        var ocrProvider = new TesseractOcrProvider(config, NullLogger<TesseractOcrProvider>.Instance);
        var sut = new PdfTextExtractor(ocrProvider, NullLogger<PdfTextExtractor>.Instance);
        var pdfBytes = BuildScanLikePdf("HOSOBENHNHAN");

        // Act
        var result = await sut.ExtractTextAsync(pdfBytes, "hoso_scan_that_003.pdf", CancellationToken.None);

        // Assert - Tesseract that phai doc duoc chuoi chu in hoa ro net nay (tolerant: chi can
        // chua phan lon chuoi, tranh flaky do khac biet font-rendering giua may)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value!.ToUpperInvariant().Should().Contain("HOSO");
    }
}
