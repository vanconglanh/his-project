using ProDiabHis.Application.EMR;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.EMR;

/// <summary>Export EMR as PDF using QuestPDF.</summary>
public class QuestPdfEmrExporter : IEmrPdfExporter
{
    public Task<byte[]> ExportAsync(EmrPdfContext context, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("PHÒNG KHÁM PRO-DIAB HIS").Bold().FontSize(14).AlignCenter();
                    col.Item().Text("BỆNH ÁN ĐIỆN TỬ").Bold().FontSize(12).AlignCenter();
                    col.Item().LineHorizontal(1);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text($"Bệnh nhân: {context.PatientName}");
                        row.RelativeItem().Text($"Ngày khám: {context.EncounterDate:dd/MM/yyyy}").AlignRight();
                    });
                    if (context.DoctorName is not null)
                        col.Item().Text($"Bác sĩ: {context.DoctorName}");
                    col.Item().LineHorizontal(0.5f).LineColor("#cccccc");
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    // Render content_html as plain text (simple extraction)
                    var plainText = StripHtml(context.ContentHtml);
                    col.Item().Text(plainText).FontSize(10.5f);

                    if (context.IsSigned)
                    {
                        col.Item().PaddingTop(20).LineHorizontal(0.5f).LineColor("#cccccc");
                        col.Item().PaddingTop(8).Column(sig =>
                        {
                            sig.Item().Text("CHỮ KÝ SỐ").Bold();
                            if (context.SignedAt.HasValue)
                                sig.Item().Text($"Ký lúc: {context.SignedAt.Value:dd/MM/yyyy HH:mm}");
                            if (context.SignerName is not null)
                                sig.Item().Text($"Người ký: {context.SignerName}");
                            if (context.CertSerial is not null)
                                sig.Item().Text($"Serial chứng thư: {context.CertSerial}").FontSize(8).FontColor("#666666");
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        var bytes = doc.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
    }
}
