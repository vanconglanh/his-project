using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>
/// QuestPDF render bien lai thu tien kho A5, theo chuan format chung (letterhead teal ReportPdfCommon).
/// </summary>
public class ReceiptPdfService : IReceiptPdfService
{
    static ReceiptPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateReceiptPdfAsync(
        ReceiptData receipt,
        bool reprint = false,
        CancellationToken ct = default)
    {
        var lh = receipt.Letterhead ?? new LetterheadDto(
            receipt.TenantName ?? "Pro-Diab HIS", receipt.TenantCskcbCode, null, receipt.TenantAddress,
            null, null, null, null);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(8, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginHorizontal(9, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(ReportPdfCommon.Ink));

                page.Header().Column(col =>
                {
                    if (reprint)
                        col.Item().AlignRight()
                            .Text("[IN LẠI]").FontSize(8).FontColor(ReportPdfCommon.Brand).Bold();

                    col.Item().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "BIÊN LAI THU TIỀN"));

                    // Thong tin bien lai
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Row(string label, string? value)
                        {
                            table.Cell().PaddingBottom(2).Text(label + ":").FontColor(ReportPdfCommon.Muted).SemiBold();
                            table.Cell().PaddingBottom(2).Text(value ?? "—");
                        }

                        Row("Số biên lai", receipt.ReceiptNo);
                        Row("Ngày", receipt.PaidAt.ToString("dd/MM/yyyy HH:mm"));
                        Row("Bệnh nhân", receipt.PatientName);
                        if (!string.IsNullOrEmpty(receipt.PatientCode))
                            Row("Mã BN", receipt.PatientCode);
                        if (!string.IsNullOrEmpty(receipt.Phone))
                            Row("Điện thoại", receipt.Phone);
                    });

                    // Danh sach item
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Nội dung thu"));
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignCenter().Text("SL").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignRight().Text("Đơn giá").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignRight().Text("Thành tiền").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                        });

                        var i = 0;
                        foreach (var item in receipt.Items)
                        {
                            i++;
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.Name).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignCenter().Text(item.Quantity.ToString("N0")).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.LineTotal.ToString("N0")).FontSize(8.5f);
                        }
                    });

                    // Tong tien
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem(2).Text("TỔNG TIỀN:").Bold().FontSize(11).FontColor(ReportPdfCommon.Ink);
                        row.RelativeItem(3).AlignRight()
                            .Text($"{receipt.Amount:N0} VNĐ").Bold().FontSize(13)
                            .FontColor(ReportPdfCommon.Brand);
                    });

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem(2).Text("Phương thức:").FontColor(ReportPdfCommon.Muted).FontSize(9);
                        row.RelativeItem(3).AlignRight().Text(receipt.Method).FontSize(9);
                    });

                    if (!string.IsNullOrEmpty(receipt.Reference))
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Text("Mã GD:").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            row.RelativeItem(3).AlignRight().Text(receipt.Reference).FontSize(9);
                        });

                    if (!string.IsNullOrEmpty(receipt.CashierName))
                        col.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem(2).Text("Thu ngân:").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            row.RelativeItem(3).AlignRight().Text(receipt.CashierName).FontSize(9);
                        });

                    col.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                                .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                            sig.Item().AlignCenter().Text("NGƯỜI THU TIỀN").Bold().FontSize(9.5f);
                            sig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                        });
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.75f).LineColor(ReportPdfCommon.LineColor);
                    col.Item().PaddingTop(3).AlignCenter()
                        .Text("Cảm ơn quý khách!").FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                    col.Item().AlignCenter()
                        .Text($"In lúc: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7.5f)
                        .FontColor(ReportPdfCommon.Muted);
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
