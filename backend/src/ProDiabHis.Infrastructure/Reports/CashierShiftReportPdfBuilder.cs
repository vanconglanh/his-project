using ProDiabHis.Application.Billing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>Sinh PDF Bao cao ca thu ngan (QuestPDF), kho A4, tai dung khung thuong hieu diaB.</summary>
public class CashierShiftReportPdfBuilder : ICashierShiftReportPdfBuilder
{
    static CashierShiftReportPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(CashierShiftReportData d)
    {
        var gross = d.TotalCash + d.TotalCard + d.TotalTransfer + d.TotalQr + d.TotalOther;
        var net = gross - d.TotalRefund - d.TotalVoid;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, d.Letterhead, null));

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "BÁO CÁO CA THU NGÂN"));

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Ca ngày: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span($"{d.ShiftDate:dd/MM/yyyy}").SemiBold().FontSize(9.5f);
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Nhân viên: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(d.CashierName ?? "—").SemiBold().FontSize(9.5f);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Trạng thái: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(d.Status == "CLOSED" ? "Đã đóng ca" : "Đang mở").SemiBold().FontSize(9.5f);
                        });
                    });

                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Mở ca: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span($"{d.ShiftStart:HH:mm dd/MM/yyyy}").FontSize(9.5f);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Đóng ca: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(d.ShiftEnd.HasValue ? $"{d.ShiftEnd:HH:mm dd/MM/yyyy}" : "—").FontSize(9.5f);
                        });
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Element(c => ReportPdfCommon.RenderKpi(c, "TỔNG THU GỘP", $"{gross:N0} đ", ReportPdfCommon.TintTeal));
                        row.ConstantItem(8);
                        row.RelativeItem().Element(c => ReportPdfCommon.RenderKpi(c, "TỔNG THU THUẦN", $"{net:N0} đ", ReportPdfCommon.TintTeal));
                        row.ConstantItem(8);
                        row.RelativeItem().Element(c => ReportPdfCommon.RenderKpi(c, "SỐ GIAO DỊCH", $"{d.CountTransactions:N0}", ReportPdfCommon.TintNeutral));
                    });

                    // Bang tong hop theo phuong thuc thanh toan
                    col.Item().PaddingTop(14).Text("TỔNG HỢP THEO PHƯƠNG THỨC THANH TOÁN").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Ink);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2.4f);
                            cd.RelativeColumn(1.6f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Phương thức"));
                            h.Cell().Element(ReportPdfCommon.HeadCell).AlignRight().Text("Số tiền").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                        });

                        var rows = new (string Label, decimal Value)[]
                        {
                            ("Tiền mặt", d.TotalCash),
                            ("Thẻ", d.TotalCard),
                            ("Chuyển khoản", d.TotalTransfer),
                            ("QR", d.TotalQr),
                            ("Khác", d.TotalOther),
                            ("Hoàn tiền", -d.TotalRefund),
                            ("Hủy giao dịch", -d.TotalVoid),
                        };

                        var i = 0;
                        foreach (var (label, value) in rows)
                        {
                            i++;
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(label);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text($"{value:N0} đ");
                        }
                    });

                    // Bang tong hop tien mat dau/cuoi ca
                    col.Item().PaddingTop(14).Text("ĐỐI SOÁT TIỀN MẶT").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Ink);
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2.4f);
                            cd.RelativeColumn(1.6f);
                        });

                        void SumRow(int idx, string label, decimal value)
                        {
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).Text(label).FontColor(ReportPdfCommon.Muted);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).AlignRight().Text($"{value:N0} đ");
                        }

                        SumRow(1, "Số dư đầu ca", d.OpeningBalance);
                        SumRow(2, "Tiền mặt kỳ vọng cuối ca", d.ExpectedCash ?? 0);
                        SumRow(3, "Tiền mặt thực tế kiểm đếm", d.ActualCash ?? 0);
                        SumRow(4, "Chênh lệch", d.Difference ?? 0);
                        SumRow(5, "Số dư cuối ca", d.ClosingBalance);
                    });

                    if (!string.IsNullOrWhiteSpace(d.Note))
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(d.Note).FontSize(9);
                        });
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, d.CashierName));
            });
        }).GeneratePdf();
    }
}
