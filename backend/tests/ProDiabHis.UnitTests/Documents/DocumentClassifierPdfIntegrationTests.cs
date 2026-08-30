using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using ProDiabHis.Application.Documents;
using Xunit;

namespace ProDiabHis.UnitTests.Documents;

/// <summary>
/// Verify THAT tren FILE PDF that (khong phai chuoi mock): sinh PDF bang QuestPDF, doc lai text layer
/// bang UglyToad.PdfPig (dung engine ma IPdfTextExtractor su dung o tang ha tang), roi chay
/// DocumentClassifierService THUC. Chung minh duong: PDF -> text -> phan loai -> route dung.
/// Ghi ca file PDF + ket qua phan loai ra thu muc evidence de kiem chung.
/// </summary>
public class DocumentClassifierPdfIntegrationTests
{
    private static readonly string EvidenceDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
        "docs", "qc", "evidence-smart-document-upload-20260830"));

    private static string ReadPdfText(byte[] pdfBytes)
    {
        using var ms = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(ms);
        return string.Join("\n", pdf.GetPages().Select(p => p.Text));
    }

    private static void WriteEvidence(string name, byte[] pdfBytes, string rawText, DocumentClassifyResult r)
    {
        Directory.CreateDirectory(EvidenceDir);
        File.WriteAllBytes(Path.Combine(EvidenceDir, $"{name}.pdf"), pdfBytes);
        var lines = new List<string>
        {
            $"File           : {name}.pdf",
            $"Loai nhan dien : {r.Type}",
            $"Do tin cay     : {r.Confidence:0.00}",
            $"Bang chung     : {(r.Evidence.Count == 0 ? "(khong)" : string.Join(", ", r.Evidence))}",
            "Ung vien       :"
        };
        lines.AddRange(r.Candidates.Select(c =>
            $"   - {c.Type,-10} score={c.Score:0.00} evidence={string.Join(", ", c.Evidence)}"));
        lines.Add("");
        lines.Add("--- RAW TEXT DOC TU PDF ---");
        lines.Add(rawText);
        File.WriteAllText(Path.Combine(EvidenceDir, $"{name}-ket-qua.txt"), string.Join("\n", lines));
    }

    [Fact]
    public async Task RealInBodyPdf_ClassifiedAsInBody_AndRoutable()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = BuildInBodyPdf();
        var rawText = ReadPdfText(pdfBytes);

        // Khong co encounter -> khong xet Lab; chi dua vao nhan dac trung InBody
        var sut = new DocumentClassifierService(FakePendingLabTestsProvider.Empty());
        var result = await sut.ClassifyAsync(new DocumentClassifyInput(rawText, null), default);

        WriteEvidence("case1-inbody-tu-nhan-dien", pdfBytes, rawText, result);

        Assert.Equal(DocumentType.InBody, result.Type);
        Assert.True(result.Confidence >= 0.6, $"confidence={result.Confidence}");
        Assert.NotEmpty(result.Evidence);
    }

    [Fact]
    public async Task RealLegacyPdf_NoMatch_FallsBackToLegacy()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = BuildLegacyRecordPdf();
        var rawText = ReadPdfText(pdfBytes);

        // Ho so cu ngau nhien, khong khop InBody, khong co encounter/pending -> Legacy fallback an toan
        var sut = new DocumentClassifierService(FakePendingLabTestsProvider.Empty());
        var result = await sut.ClassifyAsync(new DocumentClassifyInput(rawText, null), default);

        WriteEvidence("case2-ho-so-cu-mac-dinh-legacy", pdfBytes, rawText, result);

        Assert.Equal(DocumentType.Legacy, result.Type);
        Assert.True(result.Confidence <= 0.6, $"confidence={result.Confidence}");
    }

    [Fact]
    public async Task RealLabPdf_WithPendingTests_ClassifiedAsLabResult()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = BuildLabResultPdf();
        var rawText = ReadPdfText(pdfBytes);

        var encounterId = Guid.NewGuid();
        var pending = new List<(Guid, string, string)>
        {
            (Guid.NewGuid(), "GLU",   "Glucose"),
            (Guid.NewGuid(), "HBA1C", "HbA1c"),
            (Guid.NewGuid(), "CHOL",  "Cholesterol toàn phần"),
        };
        var sut = new DocumentClassifierService(new FakePendingLabTestsProvider(pending));
        var result = await sut.ClassifyAsync(new DocumentClassifyInput(rawText, encounterId), default);

        WriteEvidence("case3-ket-qua-xet-nghiem", pdfBytes, rawText, result);

        Assert.Equal(DocumentType.LabResult, result.Type);
        Assert.True(result.Confidence >= 0.6, $"confidence={result.Confidence}");
    }

    private static byte[] BuildInBodyPdf() => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11));
            page.Header().Text("Body Composition Analysis - InBody 770").SemiBold().FontSize(14);
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Text("InBody Score: 78 / 100");
                col.Item().Text("Percent Body Fat (PBF): 22.5 %");
                col.Item().Text("Skeletal Muscle Mass (SMM): 30.2 kg");
                col.Item().Text("Visceral Fat Level: 8");
                col.Item().Text("Segmental Lean Analysis: Normal");
            });
        });
    }).GeneratePdf();

    private static byte[] BuildLegacyRecordPdf() => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11));
            page.Header().Text("SỔ KHÁM BỆNH CŨ - NĂM 2015").SemiBold().FontSize(14);
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Text("Họ tên: Trần Thị B    Năm sinh: 1970");
                col.Item().Text("Chẩn đoán: Viêm họng cấp");
                col.Item().Text("Lời dặn bác sĩ: Uống thuốc theo toa, tái khám sau 5 ngày.");
                col.Item().Text("Ghi chú: Hồ sơ giấy lưu trữ, số quyển 12, trang 34.");
            });
        });
    }).GeneratePdf();

    private static byte[] BuildLabResultPdf() => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11));
            page.Header().Text("PHÒNG XÉT NGHIỆM - PHIẾU KẾT QUẢ").SemiBold().FontSize(14);
            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().Text("Glucose (đường huyết): 5.6 mmol/L");
                col.Item().Text("HbA1c: 6.1 %");
                col.Item().Text("Cholesterol toàn phần: 4.8 mmol/L");
            });
        });
    }).GeneratePdf();
}
