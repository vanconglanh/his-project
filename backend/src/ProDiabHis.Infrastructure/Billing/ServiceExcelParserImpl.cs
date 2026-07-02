using ClosedXML.Excel;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Infrastructure.Billing;

public class ServiceExcelParserImpl : IServiceExcelParser
{
    public List<ServiceExcelRow> Parse(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.First();
        var result = new List<ServiceExcelRow>();
        int row = 2;

        while (!ws.Row(row).IsEmpty())
        {
            var code = ws.Cell(row, 1).GetString().Trim();
            var name = ws.Cell(row, 2).GetString().Trim();
            var category = ws.Cell(row, 3).GetString().Trim().ToUpper();
            decimal price = 0;
            int vatRate = 0;

            try { price = ws.Cell(row, 4).GetValue<decimal>(); } catch { }
            try { vatRate = ws.Cell(row, 5).GetValue<int>(); } catch { }

            result.Add(new ServiceExcelRow(code, name, category, price, vatRate));
            row++;
        }
        return result;
    }
}
