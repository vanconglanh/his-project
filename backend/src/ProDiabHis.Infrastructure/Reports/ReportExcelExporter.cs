using ClosedXML.Excel;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

public class ReportExcelExporter : IExcelExporter
{
    /// <summary>Xuat bao cao config-driven (Report Engine) — header tu descriptor.Columns, dong subtotal/total.</summary>
    public byte[] ExportGeneric(ReportDescriptor descriptor, ReportDataResult data, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        var columns = descriptor.Columns;

        // Header
        for (int i = 0; i < columns.Count; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = columns[i].Label;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#01645A");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;

        void WriteDataRow(IDictionary<string, object?> data0)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var col = columns[c];
                var raw = data0.TryGetValue(col.Key, out var v) ? v : null;
                var cell = ws.Cell(row, c + 1);
                WriteCellValue(cell, raw, col);
            }
            row++;
        }

        void WriteTotalsRow(IReadOnlyDictionary<string, decimal> totals, string label)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            for (int c = 0; c < columns.Count; c++)
            {
                if (!columns[c].IsGroupSubtotal) continue;
                var val = totals.TryGetValue(columns[c].Key, out var v) ? v : 0m;
                var cell = ws.Cell(row, c + 1);
                cell.Value = val;
                cell.Style.NumberFormat.Format = "#,##0";
                cell.Style.Font.Bold = true;
            }
            row++;
        }

        if (data.Groups is { Count: > 0 })
        {
            foreach (var g in data.Groups)
            {
                var header = ws.Cell(row, 1);
                header.Value = descriptor.ShowGroupCount ? $"{g.Label} ({g.Count} dòng)" : g.Label;
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                row++;

                foreach (var r in g.Rows) WriteDataRow(r);
                WriteTotalsRow(g.Subtotals, "Cộng nhóm");
            }
        }
        else if (data.Rows is { Count: > 0 })
        {
            foreach (var r in data.Rows) WriteDataRow(r);
        }

        if (columns.Any(c => c.IsGroupSubtotal))
            WriteTotalsRow(data.Totals, "TỔNG CỘNG");

        // Freeze 2 hang dau (header + dong dau du lieu) + cot dau
        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteCellValue(IXLCell cell, object? raw, ReportColumn col)
    {
        if (raw is null || raw is DBNull)
        {
            cell.Value = "-";
            return;
        }

        switch (col.Type)
        {
            case ReportColumnType.Money:
            case ReportColumnType.Number:
                cell.Value = ReportValueConverter.ToDecimal(raw);
                cell.Style.NumberFormat.Format = "#,##0";
                break;
            case ReportColumnType.Date:
                if (raw is DateTime dt) cell.Value = dt.Date;
                else if (raw is DateOnly d0) cell.Value = d0.ToDateTime(TimeOnly.MinValue);
                else cell.Value = raw.ToString();
                cell.Style.DateFormat.Format = "dd/MM/yyyy";
                break;
            case ReportColumnType.DateTime:
                if (raw is DateTime dt2) cell.Value = dt2;
                else cell.Value = raw.ToString();
                cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                break;
            default:
                cell.Value = raw.ToString();
                break;
        }
    }

    public byte[] Export<T>(IEnumerable<T> rows, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        var list = rows.ToList();
        if (!list.Any())
        {
            using var ms2 = new MemoryStream();
            wb.SaveAs(ms2);
            return ms2.ToArray();
        }

        var props = typeof(T).GetProperties();

        // Header
        for (int i = 0; i < props.Length; i++)
            ws.Cell(1, i + 1).Value = props[i].Name;

        // Data
        for (int row = 0; row < list.Count; row++)
        {
            for (int col = 0; col < props.Length; col++)
            {
                var val = props[col].GetValue(list[row]);
                ws.Cell(row + 2, col + 1).Value = val?.ToString() ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
