using FluentAssertions;
using ProDiabHis.Application.Reports.Engine;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// Test chong tai phat (regression guard) cho lo ro ri du lieu cheo chi nhanh trong Report Builder engine
/// (docs/prd/phan-tich-da-chi-nhanh-mo-rong-20260829.md muc E/Dot 0.b).
///
/// Bug goc: SafeQueryBuilder.Build KHONG BAO GIO doc ReportQueryContext.BranchId/IgnoreBranchFilter —
/// du Dataset khai bao BranchAlias hay khong, cau SQL sinh ra van chi loc theo tenant_id. Ket qua: user
/// chi nhanh A xem duoc Report Builder preview/run chua du lieu chi nhanh B (thu ngan, luot kham, kho,
/// don thuoc, cong no, CLS).
///
/// Khong the chay integration test that voi DB trong moi truong nay (khong co MySQL/Testcontainers san
/// sang trong sandbox) -> test nay kiem tra CHUOI SQL + THAM SO Dapper sinh ra tu SafeQueryBuilder.Build,
/// dam bao dieu kien BranchSql.Condition(...) va tham so @branchId/@ignoreBranch LUON co mat khi Dataset
/// khai bao BranchAlias.
/// </summary>
public class SafeQueryBuilderBranchFilterTests
{
    private static Dataset MakeDataset(string? branchAlias) => new(
        Key: "test-ds",
        Label: "Test dataset",
        FromSql: "diab_his_bil_payments p",
        BaseWhereSql: "p.tenant_id = @tenantId AND p.deleted_at IS NULL",
        DateFieldKey: "paidDate",
        Fields: new List<DatasetField>
        {
            DatasetField.Dimension("paidDate", "Ngay thu", "DATE(p.paid_at)", ReportColumnType.Date),
            DatasetField.Measure("amount", "So tien", "p.amount", ReportColumnType.Money, ReportAggregation.Sum)
        },
        BranchAlias: branchAlias);

    private static ReportDefinitionInput MakeInput() => new(
        Title: "Test",
        DatasetKey: "test-ds",
        Columns: new List<ReportDefinitionColumn> { new("paidDate", "Ngay thu", Agg: null) },
        Filters: Array.Empty<ReportDefinitionFilter>(),
        GroupBy: Array.Empty<string>(),
        Sort: Array.Empty<ReportDefinitionSort>(),
        Kpis: Array.Empty<ReportDefinitionKpi>(),
        Chart: null,
        ViewType: ReportViewType.Table,
        Visibility: ReportVisibility.Private);

    [Fact]
    public void Build_KhiDatasetCoBranchAlias_PhaiGhepDieuKienBranchVaThamSo()
    {
        var dataset = MakeDataset(branchAlias: "p");
        var input = MakeInput();
        var ctx = new ReportQueryContext(
            TenantId: 1,
            From: new DateOnly(2026, 8, 1),
            To: new DateOnly(2026, 8, 29),
            Filters: new Dictionary<string, string?>(),
            BranchId: 5,
            IgnoreBranchFilter: false);

        var (sql, parameters) = SafeQueryBuilder.Build(dataset, input, ctx, limit: 100);

        sql.Should().Contain("@ignoreBranch = 1 OR p.branch_id IS NULL OR p.branch_id = @branchId",
            "SafeQueryBuilder phai luon ghep BranchSql.Condition khi Dataset khai bao BranchAlias");

        var paramNames = ((Dapper.DynamicParameters)parameters).ParameterNames;
        paramNames.Should().Contain("branchId");
        paramNames.Should().Contain("ignoreBranch");
    }

    [Fact]
    public void Build_KhiDatasetKhongCoBranchAlias_KhongDuocTuTaoThamSoBranch()
    {
        // Dataset "legacy" chua khai bao BranchAlias (vd dataset chi dua tren bang toan cuc theo tenant,
        // khong gan branch) -> khong duoc bo sung dieu kien/tham so branch, tranh loi "tham so khong ton tai".
        var dataset = MakeDataset(branchAlias: null);
        var input = MakeInput();
        var ctx = new ReportQueryContext(
            TenantId: 1, From: new DateOnly(2026, 8, 1), To: new DateOnly(2026, 8, 29),
            Filters: new Dictionary<string, string?>());

        var (sql, parameters) = SafeQueryBuilder.Build(dataset, input, ctx, limit: 100);

        sql.Should().NotContain("branch_id");
        var paramNames = ((Dapper.DynamicParameters)parameters).ParameterNames;
        paramNames.Should().NotContain("branchId");
    }

    [Fact]
    public void Build_KhiIgnoreBranchFilterTrue_DieuKienVanDuocGhep_NhungChoPhepBoQuaOChayThat()
    {
        // Sieu admin / user co quyen branch.cross_view -> IgnoreBranchFilter = true. Dieu kien SQL van phai
        // duoc ghep (khong an toan neu bo hoan toan) — logic bo qua nam trong chinh bieu thuc SQL
        // "@ignoreBranch = 1 OR ...", KHONG duoc xu ly bang cach loai bo whereClause phia C#.
        var dataset = MakeDataset(branchAlias: "p");
        var input = MakeInput();
        var ctx = new ReportQueryContext(
            TenantId: 1, From: new DateOnly(2026, 8, 1), To: new DateOnly(2026, 8, 29),
            Filters: new Dictionary<string, string?>(), BranchId: 0, IgnoreBranchFilter: true);

        var (sql, parameters) = SafeQueryBuilder.Build(dataset, input, ctx, limit: 100);

        sql.Should().Contain("@ignoreBranch = 1 OR p.branch_id IS NULL OR p.branch_id = @branchId");
    }
}
