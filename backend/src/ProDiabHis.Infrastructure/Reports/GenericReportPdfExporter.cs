using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Exporter PDF config-driven cho Report Engine — nhan 1 ReportDescriptor + ReportDataResult bat ky
/// va render theo khung chuan diaB (tai su dung ReportPdfCommon: letterhead/barcode/footer/brand mau).
/// </summary>
public class GenericReportPdfExporter : IGenericReportPdfExporter
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<GenericReportPdfExporter>? _logger;
    private readonly IReadOnlyList<string> _allowedLogoHosts;

    public GenericReportPdfExporter(
        IHttpClientFactory? httpClientFactory = null,
        ILogger<GenericReportPdfExporter>? logger = null,
        IConfiguration? configuration = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;

        var configuredHosts = configuration?.GetSection("Reports:AllowedLogoHosts").Get<string[]>();
        _allowedLogoHosts = configuredHosts ?? Array.Empty<string>();
    }

    public async Task<byte[]> ExportAsync(
        ReportDescriptor descriptor,
        ReportPdfRequest req,
        LetterheadDto letterhead,
        ReportDataResult data,
        CancellationToken ct = default)
    {
        byte[]? logoBytes = await ReportPdfCommon.TryFetchLogoAsync(
            _httpClientFactory, _logger, _allowedLogoHosts, letterhead.LogoUrl, ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                if (descriptor.Orientation == ReportOrientation.Landscape)
                    ReportPdfCommon.ConfigureA4LandscapePage(page);
                else
                    ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, descriptor.Title));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => ReportPdfCommon.RenderMeta(c, req));

                    if (data.Kpis.Count > 0)
                        col.Item().PaddingTop(10).Element(c => RenderKpis(c, data.Kpis));

                    col.Item().PaddingTop(12).Element(c => RenderGenericTable(c, descriptor, data));
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, req.ExportedByFullName));
            });
        }).GeneratePdf();

        return bytes;
    }

    private static void RenderKpis(IContainer container, IReadOnlyList<ReportKpiResult> kpis)
    {
        container.Row(row =>
        {
            for (int i = 0; i < kpis.Count; i++)
            {
                var k = kpis[i];
                var value = k.IsMoney ? $"{k.Value:#,##0} đ" : $"{k.Value:#,##0}";
                row.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, k.Label, value, k.Tint));
                if (i < kpis.Count - 1) row.ConstantItem(10);
            }
        });
    }

    private static void RenderGenericTable(IContainer container, ReportDescriptor descriptor, ReportDataResult data)
    {
        var columns = descriptor.Columns;

        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                foreach (var c in columns)
                    cols.RelativeColumn(c.Width);
            });

            tbl.Header(h =>
            {
                foreach (var c in columns)
                {
                    if (c.Align == ReportAlign.Right)
                        ReportPdfCommon.HeadCell(h.Cell()).AlignRight().Text(c.Label).FontColor("#FFFFFF").Bold().FontSize(9.5f);
                    else if (c.Align == ReportAlign.Center)
                        ReportPdfCommon.HeadCell(h.Cell()).AlignCenter().Text(c.Label).FontColor("#FFFFFF").Bold().FontSize(9.5f);
                    else
                        ReportPdfCommon.HText(h.Cell(), c.Label);
                }
            });

            var rowIndex = 0;

            if (data.Groups is { Count: > 0 })
            {
                foreach (var g in data.Groups)
                {
                    var label = descriptor.ShowGroupCount ? $"{g.Label}  ({g.Count} dòng)" : g.Label;
                    tbl.Cell().ColumnSpan((uint)columns.Count)
                        .Background(ReportPdfCommon.TintNeutral)
                        .PaddingVertical(5).PaddingHorizontal(6)
                        .Text(label).Bold().FontSize(9.5f).FontColor(ReportPdfCommon.Brand);

                    foreach (var row in g.Rows)
                    {
                        RenderRow(tbl, columns, rowIndex, row);
                        rowIndex++;
                    }

                    RenderTotalsRow(tbl, columns, g.Subtotals, "Cộng nhóm", ReportPdfCommon.ZebraBg, ReportPdfCommon.Ink);
                }
            }
            else if (data.Rows is { Count: > 0 })
            {
                foreach (var row in data.Rows)
                {
                    RenderRow(tbl, columns, rowIndex, row);
                    rowIndex++;
                }
            }
            else
            {
                tbl.Cell().ColumnSpan((uint)columns.Count)
                    .PaddingVertical(14).AlignCenter()
                    .Text("Không có dữ liệu trong kỳ").FontColor(ReportPdfCommon.Muted).Italic();
            }

            if (columns.Any(c => c.IsGroupSubtotal))
                RenderTotalsRow(tbl, columns, data.Totals, "TỔNG CỘNG", ReportPdfCommon.BrandDark, "#FFFFFF");
        });
    }

    private static void RenderRow(
        TableDescriptor tbl,
        IReadOnlyList<ReportColumn> columns,
        int rowIndex,
        IDictionary<string, object?> row)
    {
        foreach (var col in columns)
        {
            var raw = row.TryGetValue(col.Key, out var v) ? v : null;
            var text = ReportValueConverter.FormatValue(raw, col.Type);
            var cell = ReportPdfCommon.BodyCell(tbl.Cell(), rowIndex);
            cell = col.Align switch
            {
                ReportAlign.Right => cell.AlignRight(),
                ReportAlign.Center => cell.AlignCenter(),
                _ => cell
            };
            cell.Text(text).FontSize(9);
        }
    }

    private static void RenderTotalsRow(
        TableDescriptor tbl,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyDictionary<string, decimal> totals,
        string label,
        string bg,
        string fg)
    {
        var firstSubtotalIndex = columns.ToList().FindIndex(c => c.IsGroupSubtotal);
        var leadingCount = firstSubtotalIndex < 0 ? columns.Count : Math.Max(1, firstSubtotalIndex);

        tbl.Cell().ColumnSpan((uint)leadingCount)
            .Background(bg).PaddingVertical(6).PaddingHorizontal(6)
            .AlignRight().Text(label).FontColor(fg).Bold().FontSize(9.5f);

        for (int idx = leadingCount; idx < columns.Count; idx++)
        {
            var col = columns[idx];
            if (col.IsGroupSubtotal)
            {
                var val = totals.TryGetValue(col.Key, out var v) ? v : 0m;
                tbl.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6)
                    .AlignRight().Text($"{val:#,##0}").FontColor(fg).Bold().FontSize(9.5f);
            }
            else
            {
                tbl.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6).Text(string.Empty);
            }
        }
    }
}
