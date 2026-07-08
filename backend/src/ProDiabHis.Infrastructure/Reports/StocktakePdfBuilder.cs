using ProDiabHis.Application.Pharmacy.Warehouse;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>Sinh PDF Phieu kiem ke kho (QuestPDF), kho A4, tai dung khung thuong hieu diaB.</summary>
public class StocktakePdfBuilder : IStocktakePdfBuilder
{
    static StocktakePdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(StocktakeData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, data.Letterhead, null));

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU KIỂM KÊ KHO"));

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Số phiếu: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(data.StocktakeCode ?? "—").SemiBold().FontSize(9.5f);
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Ngày kiểm kê: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span($"{data.StocktakeDate:dd/MM/yyyy}").SemiBold().FontSize(9.5f);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Kho: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(data.Location ?? "—").SemiBold().FontSize(9.5f);
                        });
                    });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(24);
                            cd.RelativeColumn(1.1f);
                            cd.RelativeColumn(2.6f);
                            cd.RelativeColumn(0.8f);
                            cd.RelativeColumn(1.1f);
                            cd.RelativeColumn(1f);
                            cd.RelativeColumn(1f);
                            cd.RelativeColumn(1f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(ReportPdfCommon.HeadCell).Text("STT").FontColor("#FFFFFF").Bold().FontSize(9);
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Mã thuốc"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Tên thuốc"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "ĐVT"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Lô"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Tồn sổ"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Tồn thực"));
                            h.Cell().Element(c => ReportPdfCommon.HText(c, "Chênh lệch"));
                        });

                        var i = 0;
                        foreach (var item in data.Items)
                        {
                            i++;
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignCenter().Text(item.Stt.ToString());
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.DrugCode ?? "—");
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.DrugName);
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.Unit ?? "—");
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).Text(item.LotNumber ?? "—");
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.SystemQty.ToString());
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight().Text(item.CountedQty.ToString());
                            table.Cell().Element(c => ReportPdfCommon.BodyCell(c, i)).AlignRight()
                                .Text(item.Difference == 0 ? "0" : (item.Difference > 0 ? $"+{item.Difference}" : item.Difference.ToString()))
                                .FontColor(item.Difference == 0 ? ReportPdfCommon.Ink : ReportPdfCommon.Brand)
                                .SemiBold();
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(data.Note))
                        col.Item().PaddingTop(8).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(data.Note).FontSize(9);
                        });
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, null));
            });
        }).GeneratePdf();
    }
}
