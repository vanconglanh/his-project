using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using ProDiabHis.Application.RadResults.Ocr;
using Xunit;

namespace ProDiabHis.UnitTests.RadResults;

/// <summary>
/// Verify THAT tren FILE PDF that (khong phai chuoi mock): sinh 1 phieu ket qua CDHA (X-quang nguc)
/// bang QuestPDF, doc lai text layer bang UglyToad.PdfPig (DUNG engine ma IPdfTextExtractor su dung o
/// tang 1), roi chay RadResultOcrParser tach 2 doan Mo ta/Ket luan. Chung minh duong doc PDF -> text
/// -> parse hoat dong. Ghi ca file PDF + ket qua ra thu muc evidence de kiem chung.
/// </summary>
public class RadResultOcrPdfIntegrationTests
{
    private static readonly string EvidenceDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
        "docs", "qc", "evidence-radresult-ocr-20260830"));

    [Fact]
    public void RealPdf_ReadByPdfPig_ThenParsed_TachDungMoTaVaKetLuan()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = BuildRadResultSheetPdf();

        // Doc text layer giong tang 1 cua PdfTextExtractor
        string rawText;
        using (var ms = new MemoryStream(pdfBytes))
        using (var pdf = PdfDocument.Open(ms))
        {
            rawText = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }

        var result = RadResultOcrParser.Parse(rawText);

        // Ghi evidence
        Directory.CreateDirectory(EvidenceDir);
        File.WriteAllBytes(Path.Combine(EvidenceDir, "phieu-ket-qua-cdha-test.pdf"), pdfBytes);
        File.WriteAllText(Path.Combine(EvidenceDir, "raw-text-doc-tu-pdf.txt"), rawText);
        File.WriteAllText(Path.Combine(EvidenceDir, "ket-qua-parse.txt"),
            "Ket qua RadResultOcrParser tren PDF that:\n\n" +
            $"[MO TA / findings]\n{result.Findings}\n\n" +
            $"[KET LUAN / conclusion]\n{result.Conclusion}\n\n" +
            $"[DE NGHI / recommendations]\n{result.Recommendations}\n");

        Assert.True(result.HasAnyExtracted);
        Assert.NotNull(result.Findings);
        Assert.Contains("phế trường", result.Findings!);
        Assert.NotNull(result.Conclusion);
        Assert.Contains("bình thường", result.Conclusion!);
        // Phan chu ky bac si khong bi gom vao noi dung
        Assert.DoesNotContain("Trần Văn B", result.Findings ?? "");
        Assert.DoesNotContain("Trần Văn B", result.Conclusion ?? "");
    }

    private static byte[] BuildRadResultSheetPdf()
    {
        var moTa = new[]
        {
            "Hai phế trường sáng, không thấy đám mờ bất thường.",
            "Rốn phổi hai bên không to. Bóng tim không to.",
            "Góc sườn hoành hai bên nhọn. Không tràn dịch màng phổi.",
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("KHOA CHẨN ĐOÁN HÌNH ẢNH - PHIẾU KẾT QUẢ X-QUANG").SemiBold().FontSize(14);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text("Họ tên: Nguyễn Văn A    Tuổi: 45    Giới: Nam");
                    col.Item().Text("Kỹ thuật: X-quang ngực thẳng");

                    col.Item().PaddingTop(12).Text("Mô tả:").SemiBold();
                    foreach (var m in moTa)
                        col.Item().Text(m);

                    col.Item().PaddingTop(12).Text("Kết luận:").SemiBold();
                    col.Item().Text("Hình ảnh X-quang ngực trong giới hạn bình thường.");

                    col.Item().PaddingTop(12).Text("Đề nghị:").SemiBold();
                    col.Item().Text("Tái khám khi có triệu chứng ho kéo dài.");

                    col.Item().PaddingTop(20).Text("Bác sĩ thực hiện: BS. Trần Văn B");
                });
            });
        }).GeneratePdf();
    }
}
