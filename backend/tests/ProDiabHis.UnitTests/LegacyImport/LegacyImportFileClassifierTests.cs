using FluentAssertions;
using ProDiabHis.Infrastructure.Jobs;
using Xunit;

namespace ProDiabHis.UnitTests.LegacyImport;

/// <summary>
/// Unit test cho LegacyImportFileClassifier - logic phan loai file trong ZIP nhap ho so giay cu
/// (mo rong ho tro PDF/TIFF/BMP, guard ro rang cho HEIC/HEIF).
/// </summary>
public class LegacyImportFileClassifierTests
{
    [Theory]
    [InlineData("hoso_001.jpg", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.JPEG", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.png", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.tiff", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.tif", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.bmp", LegacyImportFileKind.Image)]
    [InlineData("hoso_001.pdf", LegacyImportFileKind.Pdf)]
    [InlineData("hoso_001.PDF", LegacyImportFileKind.Pdf)]
    [InlineData("hoso_001.heic", LegacyImportFileKind.UnsupportedGuard)]
    [InlineData("hoso_001.HEIF", LegacyImportFileKind.UnsupportedGuard)]
    [InlineData("readme.txt", LegacyImportFileKind.Ignored)]
    [InlineData("hoso_001.docx", LegacyImportFileKind.Ignored)]
    [InlineData("hoso_001.exe", LegacyImportFileKind.Ignored)]
    public void Classify_TraVeDungLoai(string fileName, LegacyImportFileKind expected)
    {
        LegacyImportFileClassifier.Classify(fileName).Should().Be(expected);
    }
}
