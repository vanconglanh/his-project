using ProDiabHis.Application.Pharmacy.Dispensing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>Sinh PDF Phieu phat thuoc (QuestPDF), kho A5, tai dung khung thuong hieu diaB.</summary>
public class PharmacyDispenseReceiptPdfBuilder : IPharmacyDispenseReceiptPdfBuilder
{
    static PharmacyDispenseReceiptPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(DispenseReceiptData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(8, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginHorizontal(9, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(ReportPdfCommon.Ink));

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, data.Letterhead, null));

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU PHÁT THUỐC"));
                    col.Item().PaddingTop(3).AlignRight()
                        .Text($"Số phiếu: PP-{data.DispenseRecordId[..Math.Min(8, data.DispenseRecordId.Length)].ToUpperInvariant()}")
                        .FontSize(8.5f).FontColor(ReportPdfCommon.Muted);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Bệnh nhân: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(string.IsNullOrWhiteSpace(data.PatientName) ? "—" : data.PatientName).Bold().FontSize(10.5f);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Ngày phát: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span($"{data.DispensedAt:HH:mm dd/MM/yyyy}").SemiBold().FontSize(9.5f);
                        });
                    });

                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("Mã đơn thuốc: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                        t.Span(data.PrescriptionId).FontSize(9);
                    });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(24);
                            cd.RelativeColumn(3.4f);
                            cd.RelativeColumn(1f);
                            cd.RelativeColumn(1f);
                            cd.RelativeColumn(1.6f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(ReportPdfCommon.HeadCell).Text("STT").FontColor("#FFFFFF").Bold().FontSize(9);
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Tên thuốc"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "ĐVT"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "SL"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Lô / HSD"));
                        });

                        var i = 0;
                        foreach (var item in data.Items)
                        {
                            i++;
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignCenter().Text(item.Stt.ToString());
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.DrugName);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.Unit ?? "—");
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.Quantity.ToString("0.##"));
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i))
                                .Text(item.BatchNo == null ? "—" : $"{item.BatchNo}{(item.ExpiryDate.HasValue ? $" / {item.ExpiryDate:dd/MM/yyyy}" : "")}")
                                .FontSize(8.5f);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(data.Note))
                        col.Item().PaddingTop(8).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(data.Note).FontSize(9);
                        });

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text("NGƯỜI NHẬN THUỐC").Bold().FontSize(9.5f);
                            sig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                        });
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                                .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                            sig.Item().AlignCenter().Text("NGƯỜI PHÁT").Bold().FontSize(9.5f);
                            sig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.CurrentPageNumber().FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.Span("/").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.TotalPages().FontSize(8).FontColor(ReportPdfCommon.Muted);
                });
            });
        }).GeneratePdf();
    }
}
