using System.Collections.Generic;
using ProDiabHis.Application.Reports.Engine;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// BM-71: cot nhieu tu (receiptNo, patientCode, patientName...) trong Bao cao doanh thu ngay hien "-"
/// vi column.Key la camelCase nhung JSON output snake_case moi dict key (rows/subtotals/totals).
/// GetReportDataHandler.ToSnakeCaseColumns phai doi column.Key sang snake_case de khop du lieu tren wire.
/// </summary>
public class ReportDataColumnKeyCasingTests
{
    private static ReportDataResult MakeResult(params string[] columnKeys)
    {
        var cols = new List<ReportColumn>();
        foreach (var k in columnKeys)
            cols.Add(new ReportColumn(k, k, ReportColumnType.Text));

        return new ReportDataResult(
            Columns: cols,
            Groups: null,
            Rows: new List<IDictionary<string, object?>>(),
            Totals: new Dictionary<string, decimal>(),
            Kpis: new List<ReportKpiResult>(),
            TotalRows: 0);
    }

    [Theory]
    [InlineData("receiptNo", "receipt_no")]
    [InlineData("patientCode", "patient_code")]
    [InlineData("patientName", "patient_name")]
    [InlineData("totalCost", "total_cost")]
    [InlineData("bankTransfer", "bank_transfer")]
    [InlineData("netCollected", "net_collected")]
    [InlineData("patientSource", "patient_source")]
    public void MultiWord_Column_Key_Is_SnakeCased(string camel, string expectedSnake)
    {
        var result = MakeResult(camel);

        var converted = GetReportDataHandler.ToSnakeCaseColumns(result);

        Assert.Equal(expectedSnake, converted.Columns[0].Key);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("description")]
    [InlineData("cash")]
    [InlineData("card")]
    [InlineData("refund")]
    public void SingleWord_Column_Key_Is_Unchanged(string key)
    {
        var result = MakeResult(key);

        var converted = GetReportDataHandler.ToSnakeCaseColumns(result);

        Assert.Equal(key, converted.Columns[0].Key);
    }

    [Fact]
    public void Column_Keys_Match_SnakeCase_Row_Keys_On_Wire()
    {
        // Row keys tren wire la snake_case (DictionaryKeyPolicy = SnakeCaseLower doi tu alias camelCase).
        var row = new Dictionary<string, object?>
        {
            ["date"] = "2026-10-11",
            ["receipt_no"] = "HD-202610-974A2",
            ["patient_code"] = "BNT00001",
            ["patient_name"] = "Nguyễn Văn Cũ",
        };
        var result = MakeResult("date", "receiptNo", "patientCode", "patientName");

        var converted = GetReportDataHandler.ToSnakeCaseColumns(result);

        // Sau khi chuan hoa, moi column.Key phai ton tai trong row -> FE row[col.key] khong con "-".
        foreach (var col in converted.Columns)
            Assert.True(row.ContainsKey(col.Key), $"Row thieu key '{col.Key}' (cot khong khop du lieu).");
    }

    [Fact]
    public void Label_And_Other_Fields_Are_Preserved()
    {
        var result = new ReportDataResult(
            Columns: new List<ReportColumn> { new("receiptNo", "Số Phiếu", ReportColumnType.Text, ReportAlign.Left, 0.9f) },
            Groups: null,
            Rows: new List<IDictionary<string, object?>>(),
            Totals: new Dictionary<string, decimal>(),
            Kpis: new List<ReportKpiResult>(),
            TotalRows: 0);

        var converted = GetReportDataHandler.ToSnakeCaseColumns(result);

        var col = converted.Columns[0];
        Assert.Equal("receipt_no", col.Key);
        Assert.Equal("Số Phiếu", col.Label);
        Assert.Equal(ReportColumnType.Text, col.Type);
        Assert.Equal(0.9f, col.Width);
    }
}
