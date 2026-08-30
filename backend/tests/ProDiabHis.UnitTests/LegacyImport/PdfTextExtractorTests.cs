using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LegacyImport;
using ProDiabHis.Infrastructure.Ocr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Xunit;

namespace ProDiabHis.UnitTests.LegacyImport;

/// <summary>
/// Unit test cho PdfTextExtractor (nhap ho so giay cu qua ZIP, ho tro them dinh dang PDF).
/// Tier 1: PdfPig doc text layer (PDF tao bang QuestPDF luon co text layer -> phai lay duoc text,
/// KHONG can goi OCR fallback).
/// Tier 2: gia lap PDF "khong co text layer du dung" bang cach mock IOcrTextProvider va kiem tra
/// no duoc goi (khong chay Tesseract that trong unit test de tranh phu thuoc native/tessdata that).
/// </summary>
public class PdfTextExtractorTests
{
    static PdfTextExtractorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static byte[] BuildTextPdf(string content)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Content().Text(content).FontSize(14);
            });
        }).GeneratePdf();
    }

    private static byte[] BuildEmptyPdf()
    {
        // PDF hop le nhung khong co text layer dang ke (chi 1 trang trang, khong noi dung chu).
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Content().Text(string.Empty);
            });
        }).GeneratePdf();
    }

    [Fact]
    public async Task ExtractTextAsync_PdfCoTextLayer_TraVeTextTrucTiep_KhongGoiOcr()
    {
        // Arrange
        var ocr = Substitute.For<IOcrTextProvider>();
        var sut = new PdfTextExtractor(ocr, NullLogger<PdfTextExtractor>.Instance);
        var pdfBytes = BuildTextPdf("Ho so benh nhan Nguyen Van A - Chan doan: Dai thao duong type 2");

        // Act
        var result = await sut.ExtractTextAsync(pdfBytes, "hoso_001.pdf", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Dai thao duong");
        await ocr.DidNotReceive().ExtractTextAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractTextAsync_PdfKhongCoTextLayer_FallbackGoiOcrTungTrang()
    {
        // Arrange: PDF khong co text -> extractor phai fallback sang render+OCR (goi IOcrTextProvider)
        var ocr = Substitute.For<IOcrTextProvider>();
        ocr.ExtractTextAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("Van ban OCR tu anh scan"));
        var sut = new PdfTextExtractor(ocr, NullLogger<PdfTextExtractor>.Instance);
        var pdfBytes = BuildEmptyPdf();

        // Act
        var result = await sut.ExtractTextAsync(pdfBytes, "hoso_scan_002.pdf", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Van ban OCR tu anh scan");
        await ocr.Received(1).ExtractTextAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractTextAsync_FileKhongPhaiPdfHopLe_TraVeFailure()
    {
        // Arrange
        var ocr = Substitute.For<IOcrTextProvider>();
        var sut = new PdfTextExtractor(ocr, NullLogger<PdfTextExtractor>.Instance);
        var garbageBytes = System.Text.Encoding.UTF8.GetBytes("khong phai file pdf");

        // Act
        var result = await sut.ExtractTextAsync(garbageBytes, "loi.pdf", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("LEGACY_IMPORT_PDF_INVALID");
    }
}
