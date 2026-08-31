using System.IO.Compression;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Documents;
using Xunit;

namespace ProDiabHis.UnitTests.Documents;

/// <summary>
/// Verify THAT tren batch: dong goi 3 file PDF khac loai (InBody / KQ xet nghiem / ho so cu) vao 1 ZIP,
/// giai nen bang co che dung chung <see cref="SafeZipExtractor"/> (chan zip bomb/path traversal), doc
/// text tung file bang PdfPig roi chay <see cref="DocumentClassifierService"/> THUC cho TUNG file DOC LAP.
/// Chung minh: 1 ZIP -> giai nen -> moi file phan loai rieng, KHONG gop chung/khong lan lon ket qua.
/// Ghi evidence ra thu muc docs/qc/evidence-smart-document-upload-20260830/.
/// </summary>
public class SmartUploadBatchZipIntegrationTests
{
    private static readonly string EvidenceDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
        "docs", "qc", "evidence-smart-document-upload-20260830"));

    private static readonly string[] ZipAllowedExts = { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp" };

    private static string ReadPdfText(byte[] pdfBytes)
    {
        using var ms = new MemoryStream(pdfBytes);
        using var pdf = PdfDocument.Open(ms);
        return string.Join("\n", pdf.GetPages().Select(p => p.Text));
    }

    [Fact]
    public async Task ZipWithThreeMixedFiles_EachClassifiedIndependently()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // 3 file khac loai trong 1 ZIP
        var zipBytes = BuildZip(new (string Name, byte[] Bytes)[]
        {
            ("01-inbody.pdf",   BuildInBodyPdf()),
            ("02-xetnghiem.pdf", BuildLabResultPdf()),
            ("03-hosocu.pdf",   BuildLegacyRecordPdf()),
        });

        // Giai nen an toan (dung y het co che production)
        using var zipStream = new MemoryStream(zipBytes);
        var entries = await SafeZipExtractor.ExtractAsync(
            zipStream,
            name => ZipAllowedExts.Contains(Path.GetExtension(name).ToLowerInvariant()),
            new ZipExtractLimits(20, 20L * 1024 * 1024, 100L * 1024 * 1024),
            default);

        Assert.Equal(3, entries.Count);

        // Pending lab tests giả lập cho luồng xét nghiệm (cần encounter + pending list)
        var encounterId = Guid.NewGuid();
        var pending = new List<(Guid, string, string)>
        {
            (Guid.NewGuid(), "GLU",   "Glucose"),
            (Guid.NewGuid(), "HBA1C", "HbA1c"),
            (Guid.NewGuid(), "CHOL",  "Cholesterol toàn phần"),
        };
        var classifier = new DocumentClassifierService(
            new FakePendingLabTestsProvider(pending), FakePendingRadOrdersProvider.Empty());

        // Phan loai TUNG file DOC LAP
        var results = new List<(string Name, DocumentClassifyResult R)>();
        foreach (var e in entries)
        {
            var text = ReadPdfText(e.Bytes);
            var r = await classifier.ClassifyAsync(new DocumentClassifyInput(text, encounterId), default);
            results.Add((e.Name, r));
        }

        WriteEvidence(zipBytes, entries, results);

        // Moi file ra dung loai rieng — khong lan lon
        Assert.Equal(DocumentType.InBody, results[0].R.Type);
        Assert.True(results[0].R.Confidence >= 0.6);

        Assert.Equal(DocumentType.LabResult, results[1].R.Type);
        Assert.True(results[1].R.Confidence >= 0.6);

        Assert.Equal(DocumentType.Legacy, results[2].R.Type);
        Assert.True(results[2].R.Confidence <= 0.6);
    }

    private static void WriteEvidence(
        byte[] zipBytes, IReadOnlyList<ExtractedZipEntry> entries,
        List<(string Name, DocumentClassifyResult R)> results)
    {
        Directory.CreateDirectory(EvidenceDir);
        File.WriteAllBytes(Path.Combine(EvidenceDir, "batch-zip-3-files.zip"), zipBytes);

        var lines = new List<string>
        {
            "=== VERIFY BATCH: 1 ZIP -> giai nen -> phan loai TUNG file DOC LAP ===",
            $"So file hop le giai nen duoc: {entries.Count}",
            ""
        };
        foreach (var (name, r) in results)
        {
            lines.Add($"File           : {name}");
            lines.Add($"  Loai nhan dien : {r.Type}");
            lines.Add($"  Do tin cay     : {r.Confidence:0.00}");
            lines.Add($"  Bang chung     : {(r.Evidence.Count == 0 ? "(khong)" : string.Join(", ", r.Evidence))}");
            lines.Add("");
        }
        File.WriteAllText(Path.Combine(EvidenceDir, "batch-zip-3-files-ket-qua.txt"), string.Join("\n", lines));
    }

    private static byte[] BuildZip((string Name, byte[] Bytes)[] files)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, bytes) in files)
            {
                var entry = archive.CreateEntry(name);
                using var es = entry.Open();
                es.Write(bytes, 0, bytes.Length);
            }
        }
        return ms.ToArray();
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
            });
        });
    }).GeneratePdf();
}
