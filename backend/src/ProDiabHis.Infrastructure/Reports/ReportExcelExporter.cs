using ClosedXML.Excel;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Infrastructure.Reports;

public class ReportExcelExporter : IExcelExporter
{
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
