using FluentAssertions;
using ProDiabHis.Application.Reports.Engine;
using ProDiabHis.Infrastructure.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// Dot 4 da chi nhanh — bao cao "inter-branch-debt" (Cong no noi bo giua cac chi nhanh, BR-85/BR-87,
/// muc 6.2). Trong tam: SQL phai loc tenant + scope chi nhanh (debtor HOAC creditor) + loai xoa mem.
/// </summary>
public class InterBranchDebtReportDescriptorTests
{
    private static readonly ReportRegistry Registry = new();

    private static ReportQueryContext Ctx(int branchId = 3, bool ignoreBranch = false)
        => new(TenantId: 1, From: new DateOnly(2026, 1, 1), To: new DateOnly(2026, 12, 31),
            Filters: new Dictionary<string, string?>(), BranchId: branchId, IgnoreBranchFilter: ignoreBranch);

    [Fact]
    public void BaoCao_PhaiDuocDangKyTrongRegistry()
    {
        var d = Registry.GetByCode("inter-branch-debt");

        d.Should().NotBeNull();
        d!.Title.Should().NotBeNullOrWhiteSpace();
        d.Columns.Should().NotBeEmpty();
        d.PdfTypeCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Sql_PhaiLocTheoTenant()
    {
        var (sql, _) = Registry.GetByCode("inter-branch-debt")!.BuildQuery(Ctx());

        sql.Should().Contain("tenant_id = @tenantId");
    }

    [Fact]
    public void Sql_PhaiCoDieuKienLocChiNhanhTheoDebtorHoacCreditor()
    {
        var (sql, _) = Registry.GetByCode("inter-branch-debt")!.BuildQuery(Ctx());

        sql.Should().Contain("@ignoreBranch");
        sql.Should().Contain("debtor_branch_id = @branchId");
        sql.Should().Contain("creditor_branch_id = @branchId");
    }

    [Fact]
    public void Sql_PhaiLoaiBanGhiXoaMem()
    {
        var (sql, _) = Registry.GetByCode("inter-branch-debt")!.BuildQuery(Ctx());

        sql.Should().Contain("deleted_at IS NULL");
    }

    [Fact]
    public void ThamSo_PhaiMangDungNguCanhChiNhanh()
    {
        var (_, prms) = Registry.GetByCode("inter-branch-debt")!.BuildQuery(Ctx(branchId: 7, ignoreBranch: false));
        var dp = prms.Should().BeOfType<Dapper.DynamicParameters>().Subject;

        dp.Get<int>("branchId").Should().Be(7);
        dp.Get<bool>("ignoreBranch").Should().BeFalse();
        dp.Get<int>("tenantId").Should().Be(1);
    }

    [Fact]
    public void Sql_PhaiCoGioiHanDong()
    {
        var (sql, _) = Registry.GetByCode("inter-branch-debt")!.BuildQuery(Ctx());

        sql.Should().Contain("LIMIT");
    }

    [Fact]
    public void MaBaoCao_PhaiDuyNhatTrongRegistry()
    {
        var codes = Registry.GetAll().Select(d => d.Code).ToList();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().Contain("inter-branch-debt");
    }
}
