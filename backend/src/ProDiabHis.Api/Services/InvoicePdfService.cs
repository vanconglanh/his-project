using ProDiabHis.Application.Billing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>
/// QuestPDF render hoa don kho A5 (ADR-0001).
/// Tai su dung pattern TicketPdfService.
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateInvoicePdfAsync(
        BillingResponse billing,
        PrintBillingOptions options,
        CancellationToken ct = default)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(12, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    if (options.Reprint)
                    {
                        col.Item().AlignRight()
                            .Text("[IN LAI]").FontSize(8).FontColor(Colors.Red.Medium).Bold();
                    }

                    col.Item().AlignCenter().Text("PRO-DIAB HIS").Bold().FontSize(14);
                    col.Item().AlignCenter().Text("HOA DON DICH VU").Bold().FontSize(12);
                    col.Item().AlignCenter().Text(options.CopyLabel).Italic().FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    // Thong tin hoa don
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void InfoRow(string label, string? value)
                        {
                            table.Cell().PaddingBottom(2).Text(label + ":").Bold();
                            table.Cell().PaddingBottom(2).Text(value ?? "-");
                        }

                        InfoRow("So hoa don", billing.BillNo);
                        InfoRow("Ngay lap", billing.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
                        InfoRow("Trang thai", billing.Status);
                        if (billing.PatientSummary != null)
                        {
                            InfoRow("Benh nhan", billing.PatientSummary.FullName);
                            if (billing.PatientSummary.Dob.HasValue)
                                InfoRow("Ngay sinh", billing.PatientSummary.Dob.Value.ToString("dd/MM/yyyy"));
                            InfoRow("Dien thoai", billing.PatientSummary.Phone);
                        }
                        InfoRow("Hinh thuc TT", billing.Payer);
                    });

                    col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    // Danh sach dich vu
                    col.Item().PaddingTop(4).Text("DANH SACH DICH VU:").Bold().FontSize(9);
                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        // Header
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Ten dich vu").Bold().FontSize(8);
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("SL").Bold().FontSize(8);
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(2).AlignRight().Text("Don gia").Bold().FontSize(8);
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(2).AlignRight().Text("Thanh tien").Bold().FontSize(8);

                        foreach (var item in billing.Items)
                        {
                            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                .Text(item.Name).FontSize(8);
                            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                .AlignCenter().Text(item.Quantity.ToString("N0")).FontSize(8);
                            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                .AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(8);
                            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                .AlignRight().Text(item.LineTotal.ToString("N0")).FontSize(8);
                        }
                    });

                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Tong tien
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        void SumRow(string label, decimal amount, bool bold = false, string? color = null)
                        {
                            var labelCell = table.Cell().AlignRight().PaddingBottom(2).Text(label);
                            var valueCell = table.Cell().AlignRight().PaddingBottom(2).Text(amount.ToString("N0") + " VND");
                            if (bold) { labelCell.Bold(); valueCell.Bold(); }
                            if (color != null) valueCell.FontColor(color);
                        }

                        SumRow("Tam tinh:", billing.Subtotal);
                        if (billing.VatTotal > 0) SumRow("VAT:", billing.VatTotal);
                        if (billing.DiscountAmount > 0) SumRow("Giam gia:", billing.DiscountAmount);
                        if (billing.BhytAmount > 0) SumRow("BHYT chi tra:", billing.BhytAmount);
                        SumRow("TONG CONG:", billing.PatientPayable, bold: true, color: Colors.Blue.Darken2);
                        if (billing.PaidAmount > 0) SumRow("Da thanh toan:", billing.PaidAmount);
                        if (billing.Balance != 0) SumRow("Con lai:", billing.Balance, bold: true,
                            color: billing.Balance > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                    });

                    if (!string.IsNullOrEmpty(billing.Note))
                    {
                        col.Item().PaddingTop(4).Text($"Ghi chu: {billing.Note}").FontSize(8)
                            .FontColor(Colors.Grey.Darken1).Italic();
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(3).AlignCenter()
                        .Text("Cam on quy khach da su dung dich vu!").FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter()
                        .Text($"In luc: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7)
                        .FontColor(Colors.Grey.Lighten1);
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
