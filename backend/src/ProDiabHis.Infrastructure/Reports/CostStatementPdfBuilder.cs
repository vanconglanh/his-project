using ProDiabHis.Application.Billing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>Sinh PDF "Bang ke chi phi" (QuestPDF, A5 doc), chia nhom theo loai dong hoa don.</summary>
public class CostStatementPdfBuilder : ICostStatementPdfBuilder
{
    static CostStatementPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string GroupLabel(string? type) => (type ?? "").ToUpperInvariant() switch
    {
        "PACKAGE" or "SUBSCRIPTION" or "PKG" => "GÓI DỊCH VỤ",
        "EXAM" or "VISIT" or "CONSULT" or "SERVICE" => "DỊCH VỤ KHÁM",
        "LAB" => "XÉT NGHIỆM",
        "RAD" or "IMAGING" => "CHẨN ĐOÁN HÌNH ẢNH",
        "DRUG" or "MEDICINE" or "SUPPLY" => "THUỐC - VẬT TƯ",
        "PROCEDURE" => "THỦ THUẬT",
        "" => "KHÁC",
        var other => other
    };

    public byte[] Build(CostStatementData d)
    {
        var lh = d.Letterhead;
        var groups = d.Lines.GroupBy(l => GroupLabel(l.ItemType)).ToList();
        var gross = d.Lines.Sum(l => l.LineTotal);
        string Money(decimal v) => v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(ReportPdfCommon.Ink));

                page.Header().Column(h =>
                {
                    h.Item().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));
                    h.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().AlignLeft().Text("BẢNG KÊ CHI PHÍ").Bold().FontSize(13).FontColor(ReportPdfCommon.Brand);
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Số BK: {d.BillNo ?? "—"}").SemiBold();
                            c.Item().AlignRight().Text($"Ngày: {d.Date:dd/MM/yyyy}").FontColor(ReportPdfCommon.Muted);
                        });
                    });
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem(3).Text(t =>
                        {
                            t.Span("Bệnh nhân: ").FontColor(ReportPdfCommon.Muted);
                            t.Span(d.PatientName).SemiBold();
                        });
                        r.RelativeItem(2).AlignRight().Text(t =>
                        {
                            t.Span("Mã KH: ").FontColor(ReportPdfCommon.Muted);
                            t.Span(d.PatientCode ?? "—").SemiBold();
                        });
                    });

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(18);
                            c.ConstantColumn(34);
                            c.RelativeColumn(3);
                            c.ConstantColumn(24);
                            c.ConstantColumn(20);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.5f);
                        });
                        table.Header(h =>
                        {
                            foreach (var s in new[] { "STT", "Mã DV", "Tên dịch vụ", "ĐVT", "SL", "Đơn giá (VNĐ)", "Thành tiền (VNĐ)" })
                                h.Cell().Element(c => ReportPdfCommon.HText(c, s));
                        });

                        var stt = 0;
                        foreach (var g in groups)
                        {
                            table.Cell().ColumnSpan(7).Background(ReportPdfCommon.TintNeutral)
                                .PaddingVertical(4).PaddingHorizontal(6).Text(g.Key).Bold();
                            var i = 0;
                            foreach (var l in g)
                            {
                                stt++; i++;
                                var idx = i;
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).Text(stt.ToString());
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).Text(string.IsNullOrWhiteSpace(l.Code) ? "—" : l.Code);
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).Text(l.Name);
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).Text("—");
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).AlignRight().Text(l.Quantity.ToString("0.##"));
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).AlignRight().Text(Money(l.UnitPrice));
                                table.Cell().Element(c => ReportPdfCommon.BodyCell(c, idx)).AlignRight().Text(Money(l.LineTotal));
                            }
                        }
                    });

                    col.Item().PaddingTop(8).AlignRight().Width(190).Column(t =>
                    {
                        void Row(string label, string value) => t.Item().PaddingVertical(1).Row(r =>
                        {
                            r.RelativeItem().Text(label).FontColor(ReportPdfCommon.Muted);
                            r.AutoItem().Text(value).SemiBold();
                        });
                        Row("Tổng giá trị dịch vụ", Money(gross));
                        if (d.DiscountAmount > 0) Row("Giảm giá", "-" + Money(d.DiscountAmount));
                        if (d.PaidAmount > 0) Row("Đã thu tạm", Money(d.PaidAmount));
                        t.Item().PaddingTop(3).Background(ReportPdfCommon.TintAmber).Border(1).BorderColor(ReportPdfCommon.LineColor)
                            .Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("CÒN PHẢI THU").Bold();
                                r.AutoItem().Text(Money(d.Remaining) + " đ").Bold().FontSize(10);
                            });
                    });

                    col.Item().PaddingTop(16).Row(r =>
                    {
                        foreach (var s in new[] { "Người lập bảng kê", "Khách hàng xác nhận", "Thu ngân/Kế toán" })
                            r.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text(s).Bold();
                                c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(7.5f).FontColor(ReportPdfCommon.Muted);
                            });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    var email = lh.EmailSupport ?? lh.Email;
                    var parts = new[] { lh.Phone, email }.Where(x => !string.IsNullOrWhiteSpace(x));
                    t.Span(string.Join("  |  ", parts)).FontSize(7.5f).FontColor(ReportPdfCommon.Muted);
                });
            });
        }).GeneratePdf();
    }
}
