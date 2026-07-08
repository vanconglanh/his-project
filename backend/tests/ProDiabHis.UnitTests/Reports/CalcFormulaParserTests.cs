using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// Kiem tra CalcFormulaParser — cong thuc calc field CHI duoc chua field measure whitelist +
/// so + toan tu so hoc, khong duoc lot bat ky ky tu/token la vao SQL sinh ra.
/// </summary>
public class CalcFormulaParserTests
{
    private static Dataset MakeDataset() => new(
        "test-ds", "Test dataset", "test_table t", "t.tenant_id = @tenantId", "dateCol",
        new List<DatasetField>
        {
            DatasetField.Dimension("dateCol", "Ngày", "t.date_col", ReportColumnType.Date),
            DatasetField.Dimension("category", "Nhóm", "t.category", ReportColumnType.Text),
            DatasetField.Measure("revenue", "Doanh thu", "t.revenue", ReportColumnType.Money, ReportAggregation.Sum, ReportAggregation.Avg),
            DatasetField.Measure("cost", "Chi phí", "t.cost", ReportColumnType.Money, ReportAggregation.Sum),
            DatasetField.Measure("qty", "Số lượng", "t.qty", ReportColumnType.Number, ReportAggregation.Sum)
        });

    [Fact]
    public void ToSql_SimpleSubtraction_WrapsEachMeasureWithSum()
    {
        var dataset = MakeDataset();
        var sql = CalcFormulaParser.ToSql(dataset, "revenue - cost");

        Assert.Contains("SUM(t.revenue)", sql);
        Assert.Contains("SUM(t.cost)", sql);
        Assert.Equal("(SUM(t.revenue) - SUM(t.cost))", sql);
    }

    [Fact]
    public void ToSql_Division_WrapsDenominatorWithNullIf()
    {
        var dataset = MakeDataset();
        var sql = CalcFormulaParser.ToSql(dataset, "revenue / qty");

        Assert.Equal("(SUM(t.revenue) / NULLIF(SUM(t.qty), 0))", sql);
    }

    [Fact]
    public void ToSql_ParenthesesAndPrecedence_RespectsMathOrder()
    {
        var dataset = MakeDataset();
        var sql = CalcFormulaParser.ToSql(dataset, "(revenue - cost) / qty * 100");

        Assert.Equal("(((SUM(t.revenue) - SUM(t.cost)) / NULLIF(SUM(t.qty), 0)) * 100)", sql);
    }

    [Fact]
    public void ToSql_NumberLiteral_KeptAsIs()
    {
        var dataset = MakeDataset();
        var sql = CalcFormulaParser.ToSql(dataset, "revenue * 1.5");

        Assert.Equal("(SUM(t.revenue) * 1.5)", sql);
    }

    [Theory]
    [InlineData("revenue; DROP TABLE t")]
    [InlineData("SLEEP(5)")]
    [InlineData("revenue -- comment")]
    [InlineData("revenue' OR '1'='1")]
    [InlineData("UNKNOWN_FIELD")]
    [InlineData("category")] // dimension, khong phai measure
    [InlineData("revenue +")] // thieu toan hang
    [InlineData("(revenue")] // ngoac lech
    [InlineData("revenue @@")]
    public void ToSql_MaliciousOrInvalidFormula_ThrowsValidationException(string formula)
    {
        var dataset = MakeDataset();
        var ex = Assert.Throws<ReportValidationException>(() => CalcFormulaParser.ToSql(dataset, formula));
        Assert.Equal("REPORT_DEFINITION_INVALID", ex.ErrorCode);
    }

    [Fact]
    public void ToSql_EmptyFormula_Throws()
    {
        var dataset = MakeDataset();
        Assert.Throws<ReportValidationException>(() => CalcFormulaParser.ToSql(dataset, ""));
        Assert.Throws<ReportValidationException>(() => CalcFormulaParser.ToSql(dataset, "   "));
    }

    [Fact]
    public void ToSql_TooLongFormula_Throws()
    {
        var dataset = MakeDataset();
        var longFormula = string.Join(" + ", Enumerable.Repeat("revenue", 60));
        Assert.Throws<ReportValidationException>(() => CalcFormulaParser.ToSql(dataset, longFormula));
    }
}
