using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>
/// QuestPDF render hoa don kho A5 (ADR-0001), theo chuan format chung (letterhead teal ReportPdfCommon).
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
        CancellationToken ct = default,
        LetterheadDto? letterhead = null)
    {
        var lh = letterhead ?? new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

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
                    if (options.Reprint)
                        col.Item().AlignRight()
                            .Text("[IN LẠI]").FontSize(8).FontColor(ReportPdfCommon.Brand).Bold();

                    col.Item().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "HOÁ ĐƠN DỊCH VỤ"));
                    col.Item().PaddingTop(2).AlignCenter()
                        .Text(options.CopyLabel).Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);

                    // Thong tin hoa don
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void InfoRow(string label, string? value)
                        {
                            table.Cell().PaddingBottom(2).Text(label + ":").FontColor(ReportPdfCommon.Muted).SemiBold();
                            table.Cell().PaddingBottom(2).Text(value ?? "—");
                        }

                        InfoRow("Số hoá đơn", billing.BillNo);
                        InfoRow("Ngày lập", billing.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
                        InfoRow("Trạng thái", billing.Status);
                        if (billing.PatientSummary != null)
                        {
                            InfoRow("Bệnh nhân", billing.PatientSummary.FullName);
                            if (billing.PatientSummary.Dob.HasValue)
                                InfoRow("Ngày sinh", billing.PatientSummary.Dob.Value.ToString("dd/MM/yyyy"));
                            InfoRow("Điện thoại", billing.PatientSummary.Phone);
                        }
                        InfoRow("Hình thức TT", billing.Payer);
                    });

                    // Danh sach dich vu
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
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Tên dịch vụ"));
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignCenter().Text("SL").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignRight().Text("Đơn giá").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignRight().Text("Thành tiền").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                        });

                        var i = 0;
                        foreach (var item in billing.Items)
                        {
                            i++;
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.Name).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignCenter().Text(item.Quantity.ToString("N0")).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(8.5f);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.LineTotal.ToString("N0")).FontSize(8.5f);
                        }
                    });

                    // Tong tien
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        void SumRow(string label, decimal amount, bool bold = false, string? color = null)
                        {
                            var labelCell = table.Cell().AlignRight().PaddingBottom(2).Text(label);
                            var valueCell = table.Cell().AlignRight().PaddingBottom(2).Text(amount.ToString("N0") + " VNĐ");
                            if (bold) { labelCell.Bold(); valueCell.Bold(); }
                            if (color != null) valueCell.FontColor(color); else valueCell.FontColor(ReportPdfCommon.Ink);
                            labelCell.FontColor(ReportPdfCommon.Muted);
                        }

                        SumRow("Tạm tính:", billing.Subtotal);
                        if (billing.VatTotal > 0) SumRow("VAT:", billing.VatTotal);
                        if (billing.DiscountAmount > 0) SumRow("Giảm giá:", billing.DiscountAmount);
                        if (billing.BhytAmount > 0) SumRow("BHYT chi trả:", billing.BhytAmount);
                        SumRow("TỔNG CỘNG:", billing.PatientPayable, bold: true, color: ReportPdfCommon.Brand);
                        if (billing.PaidAmount > 0) SumRow("Đã thanh toán:", billing.PaidAmount);
                        if (billing.Balance != 0) SumRow("Còn lại:", billing.Balance, bold: true,
                            color: billing.Balance > 0 ? "#DC2626" : "#16A34A");
                    });

                    if (!string.IsNullOrEmpty(billing.Note))
                        col.Item().PaddingTop(8).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(8.5f);
                            t.Span(billing.Note).FontSize(8.5f);
                        });
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.75f).LineColor(ReportPdfCommon.LineColor);
                    col.Item().PaddingTop(3).AlignCenter()
                        .Text("Cảm ơn quý khách đã sử dụng dịch vụ!").FontSize(8.5f)
                        .FontColor(ReportPdfCommon.Muted);
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
