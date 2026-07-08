using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// Kiem tra SafeQueryBuilder.Build voi cot tro toi calc field — cong thuc hop le sinh dung SQL, tham so
/// tenant/date van duoc bind qua Dapper; cong thuc doc hai KHONG duoc lot vao SQL sinh ra.
/// </summary>
public class SafeQueryBuilderCalcFieldTests
{
    private static Dataset MakeDataset() => new(
        "kho-test", "Kho test", "diab_his_pha_stock s", "s.tenant_id = @tenantId", "importedDate",
        new List<DatasetField>
        {
            DatasetField.Dimension("importedDate", "Ngày nhập", "DATE(s.created_at)", ReportColumnType.Date),
            DatasetField.Dimension("drugName", "Tên thuốc", "s.drug_name", ReportColumnType.Text),
            DatasetField.Measure("quantity", "SL tồn", "s.quantity", ReportColumnType.Number, ReportAggregation.Sum),
            DatasetField.Measure("importPrice", "Đơn giá", "s.import_price", ReportColumnType.Money, ReportAggregation.Sum)
        });

    private static ReportQueryContext Ctx() => new(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), new Dictionary<string, string?>());

    [Fact]
    public void Build_ColumnReferencesValidCalcField_ProducesSqlWithComputedExpression()
    {
        var dataset = MakeDataset();
        var calcFields = new List<ReportDefinitionCalcField>
        {
            new("giaTriTon", "Giá trị tồn", "quantity * importPrice", "Money")
        };
        var input = new ReportDefinitionInput(
            "Test", dataset.Key,
            new List<ReportDefinitionColumn>
            {
                new("drugName", "Tên thuốc", Agg: null),
                new("giaTriTon", "Giá trị tồn", Agg: null)
            },
            Filters: Array.Empty<ReportDefinitionFilter>(),
            GroupBy: Array.Empty<string>(),
            Sort: Array.Empty<ReportDefinitionSort>(),
            Kpis: Array.Empty<ReportDefinitionKpi>(),
            Chart: null,
            ViewType: ReportViewType.Table,
            Visibility: ReportVisibility.Private,
            CalcFields: calcFields);

        var (sql, parameters) = SafeQueryBuilder.Build(dataset, input, Ctx(), 200);

        Assert.Contains("SUM(s.quantity) * SUM(s.import_price)", sql);
        Assert.Contains("AS `giaTriTon`", sql);
        Assert.Contains("GROUP BY", sql); // co dim + measure -> phai group by
        Assert.Equal(1, parameters.Get<int>("tenantId"));
    }

    [Fact]
    public void Build_CalcFieldColumnWithAgg_Throws()
    {
        var dataset = MakeDataset();
        var calcFields = new List<ReportDefinitionCalcField> { new("giaTriTon", "Giá trị tồn", "quantity * importPrice", "Money") };
        var input = new ReportDefinitionInput(
            "Test", dataset.Key,
            new List<ReportDefinitionColumn> { new("giaTriTon", "Giá trị tồn", ReportAggregation.Sum) },
            Array.Empty<ReportDefinitionFilter>(), Array.Empty<string>(), Array.Empty<ReportDefinitionSort>(),
            Array.Empty<ReportDefinitionKpi>(), null, ReportViewType.Table, ReportVisibility.Private, calcFields);

        var ex = Assert.Throws<ReportValidationException>(() => SafeQueryBuilder.Build(dataset, input, Ctx(), 200));
        Assert.Equal("REPORT_DEFINITION_INVALID", ex.ErrorCode);
    }

    [Theory]
    [InlineData("quantity; DROP TABLE diab_his_pha_stock")]
    [InlineData("SLEEP(5)")]
    [InlineData("quantity OR 1=1")]
    public void Build_MaliciousCalcFieldFormula_ThrowsAndNeverReachesSql(string formula)
    {
        var dataset = MakeDataset();
        var calcFields = new List<ReportDefinitionCalcField> { new("hack", "Hack", formula, "Number") };
        var input = new ReportDefinitionInput(
            "Test", dataset.Key,
            new List<ReportDefinitionColumn> { new("hack", "Hack", Agg: null) },
            Array.Empty<ReportDefinitionFilter>(), Array.Empty<string>(), Array.Empty<ReportDefinitionSort>(),
            Array.Empty<ReportDefinitionKpi>(), null, ReportViewType.Table, ReportVisibility.Private, calcFields);

        var ex = Assert.Throws<ReportValidationException>(() => SafeQueryBuilder.Build(dataset, input, Ctx(), 200));
        Assert.Equal("REPORT_DEFINITION_INVALID", ex.ErrorCode);
    }
}
