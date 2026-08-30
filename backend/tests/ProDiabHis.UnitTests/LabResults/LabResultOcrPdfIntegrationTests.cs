using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using ProDiabHis.Application.LabResults.Ocr;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

/// <summary>
/// Verify THAT tren FILE PDF that (khong phai chuoi mock): sinh 1 phieu KQ xet nghiem bang QuestPDF,
/// doc lai text layer bang UglyToad.PdfPig (DUNG engine ma IPdfTextExtractor su dung o tang 1), roi
/// chay LabResultOcrParser theo cac XN dang cho. Chung minh duong doc PDF -> text -> parse hoat dong.
/// Ghi ca file PDF + ket qua ra thu muc evidence de kiem chung.
/// </summary>
public class LabResultOcrPdfIntegrationTests
{
    private static readonly string EvidenceDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
        "docs", "qc", "evidence-lab-result-ocr-20260830"));

    [Fact]
    public void RealPdf_ReadByPdfPig_ThenParsed_ExtractsOrderedTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = BuildLabResultSheetPdf();

        // Doc text layer giong tang 1 cua PdfTextExtractor
        string rawText;
        using (var ms = new MemoryStream(pdfBytes))
        using (var pdf = PdfDocument.Open(ms))
        {
            rawText = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        }

        var pending = new[]
        {
            new LabOcrPendingTest(Guid.NewGuid(), "GLU",   "Glucose"),
            new LabOcrPendingTest(Guid.NewGuid(), "HBA1C", "HbA1c"),
            new LabOcrPendingTest(Guid.NewGuid(), "CHOL",  "Cholesterol toàn phần"),
            new LabOcrPendingTest(Guid.NewGuid(), "TG",    "Triglyceride"),
            new LabOcrPendingTest(Guid.NewGuid(), "TSH",   "TSH"), // khong co tren phieu -> chua doc duoc
        };

        var result = LabResultOcrParser.Parse(rawText, pending);

        // Ghi evidence
        Directory.CreateDirectory(EvidenceDir);
        File.WriteAllBytes(Path.Combine(EvidenceDir, "phieu-ket-qua-xn-test.pdf"), pdfBytes);
        File.WriteAllText(Path.Combine(EvidenceDir, "raw-text-doc-tu-pdf.txt"), rawText);
        var lines = result.Fields.Select(f =>
            $"{f.TestCode,-8} {f.TestName,-28} extracted={f.Extracted,-5} value={f.RawValue ?? "(trong)"} unit={f.Unit ?? "-"}");
        File.WriteAllText(Path.Combine(EvidenceDir, "ket-qua-parse.txt"),
            "Ket qua LabResultOcrParser tren PDF that:\n" + string.Join("\n", lines));

        // Assert: 4 XN co tren phieu doc duoc, TSH khong
        Assert.Equal(7.2m,  Val(result, "GLU"));
        Assert.Equal(8.10m, Val(result, "HBA1C"));
        Assert.Equal(6.1m,  Val(result, "CHOL"));
        Assert.Equal(2.30m, Val(result, "TG"));
        Assert.False(result.Fields.First(f => f.TestCode == "TSH").Extracted);
        Assert.Equal(4, result.ExtractedCount);
    }

    private static byte[] BuildLabResultSheetPdf()
    {
        var rows = new (string Name, string Value, string Unit, string Ref)[]
        {
            ("Glucose (đường huyết)",    "7.2",  "mmol/L", "3.9 - 6.4"),
            ("HbA1c",                    "8.10", "%",      "4.0 - 6.0"),
            ("Cholesterol toàn phần",    "6.1",  "mmol/L", "< 5.2"),
            ("Triglyceride",             "2.30", "mmol/L", "< 1.7"),
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("PHÒNG XÉT NGHIỆM - PHIẾU KẾT QUẢ").SemiBold().FontSize(14);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text("Họ tên: Nguyễn Văn A    Mã BN: BN00123    Ngày: 30/08/2026");
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Tên xét nghiệm").SemiBold();
                            h.Cell().Text("Kết quả").SemiBold();
                            h.Cell().Text("Đơn vị").SemiBold();
                            h.Cell().Text("Tham chiếu").SemiBold();
                        });
                        foreach (var r in rows)
                        {
                            table.Cell().Text(r.Name);
                            table.Cell().Text(r.Value);
                            table.Cell().Text(r.Unit);
                            table.Cell().Text(r.Ref);
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static decimal? Val(LabOcrParseResult r, string code) =>
        r.Fields.First(f => f.TestCode == code).ValueNumeric;
}
