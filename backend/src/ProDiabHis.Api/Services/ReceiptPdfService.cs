using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>
/// QuestPDF render bien lai nhiet K80 (58mm x chieu cao dong).
/// Khong co footer trang co dinh — height tu gian theo noi dung.
/// </summary>
public class ReceiptPdfService : IReceiptPdfService
{
    // K80 thermal roll: 58mm rong, height tu gian
    private static readonly PageSize K80Size = new(58, 200, Unit.Millimetre);

    static ReceiptPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateReceiptPdfAsync(
        ReceiptData receipt,
        bool reprint = false,
        CancellationToken ct = default)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(K80Size);
                page.Margin(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header phong kham
                    if (!string.IsNullOrEmpty(receipt.TenantName))
                    {
                        col.Item().AlignCenter().Text(receipt.TenantName).Bold().FontSize(10);
                    }
                    if (!string.IsNullOrEmpty(receipt.TenantAddress))
                    {
                        col.Item().AlignCenter().Text(receipt.TenantAddress).FontSize(7)
                            .FontColor(Colors.Grey.Darken1);
                    }
                    if (!string.IsNullOrEmpty(receipt.TenantCskcbCode))
                    {
                        col.Item().AlignCenter().Text($"Mã CSKCB: {receipt.TenantCskcbCode}").FontSize(7)
                            .FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().AlignCenter().PaddingVertical(2).Text("BIEN LAI THU TIEN").Bold().FontSize(9);
                    if (reprint)
                        col.Item().AlignCenter().Text("[IN LAI]").FontSize(8).FontColor(Colors.Red.Medium).Bold();
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                    // Thong tin bien lai
                    col.Item().PaddingTop(3).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Row(string label, string? value)
                        {
                            table.Cell().PaddingBottom(1).Text(label + ":").Bold().FontSize(7);
                            table.Cell().PaddingBottom(1).Text(value ?? "-").FontSize(7);
                        }

                        Row("So bien lai", receipt.ReceiptNo);
                        Row("Ngay", receipt.PaidAt.ToString("dd/MM/yyyy HH:mm"));
                        Row("Benh nhan", receipt.PatientName);
                        if (!string.IsNullOrEmpty(receipt.PatientCode))
                            Row("Ma BN", receipt.PatientCode);
                        if (!string.IsNullOrEmpty(receipt.Phone))
                            Row("DT", receipt.Phone);
                    });

                    col.Item().PaddingTop(2).LineHorizontal(0.3f).LineColor(Colors.Grey.Medium);

                    // Danh sach item
                    col.Item().PaddingTop(2).Text("CHI TIET:").Bold().FontSize(7);
                    foreach (var item in receipt.Items)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(4).Text(item.Name).FontSize(7);
                            row.RelativeItem(2).AlignRight()
                                .Text($"{item.Quantity:N0} x {item.UnitPrice:N0}").FontSize(7);
                        });
                        col.Item().AlignRight()
                            .Text($"= {item.LineTotal:N0} VND").FontSize(7).FontColor(Colors.Grey.Darken2);
                    }

                    col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Black);

                    // Tong tien
                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem(2).Text("TONG TIEN:").Bold().FontSize(9);
                        row.RelativeItem(3).AlignRight()
                            .Text($"{receipt.Amount:N0} VND").Bold().FontSize(9)
                            .FontColor(Colors.Blue.Darken2);
                    });

                    col.Item().PaddingTop(1).Row(row =>
                    {
                        row.RelativeItem(2).Text("Phuong thuc:").FontSize(7);
                        row.RelativeItem(3).AlignRight().Text(receipt.Method).FontSize(7);
                    });

                    if (!string.IsNullOrEmpty(receipt.Reference))
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Text("Ma GD:").FontSize(7);
                            row.RelativeItem(3).AlignRight().Text(receipt.Reference).FontSize(7);
                        });
                    }

                    if (!string.IsNullOrEmpty(receipt.CashierName))
                    {
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem(2).Text("Thu ngan:").FontSize(7);
                            row.RelativeItem(3).AlignRight().Text(receipt.CashierName).FontSize(7);
                        });
                    }

                    col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().PaddingTop(3).AlignCenter()
                        .Text("Cam on quy khach!").FontSize(8).Bold();
                    col.Item().AlignCenter()
                        .Text($"In: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(6)
                        .FontColor(Colors.Grey.Lighten1);
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
