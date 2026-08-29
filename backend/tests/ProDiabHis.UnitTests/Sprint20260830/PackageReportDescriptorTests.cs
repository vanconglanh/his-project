using FluentAssertions;
using ProDiabHis.Application.Reports.Engine;
using ProDiabHis.Infrastructure.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H15-xx — H-15 (FR-1212) 3 bao cao goi dich vu: package-revenue / package-utilization /
/// package-outstanding-debt.
/// Trong tam BAO MAT DU LIEU: moi cau SQL sinh ra PHAI co tenant_id = @tenantId, dieu kien loc
/// chi nhanh (@ignoreBranch/@branchId) va loai soft-delete. Thieu 1 trong 3 = ro ri du lieu
/// phong kham / chi nhanh khac qua duong bao cao.
/// </summary>
public class PackageReportDescriptorTests
{
    private static readonly string[] PackageReportCodes =
        { "package-revenue", "package-utilization", "package-outstanding-debt" };

    private static readonly ReportRegistry Registry = new();

    private static ReportQueryContext Ctx(int branchId = 3, bool ignoreBranch = false,
        IReadOnlyDictionary<string, string?>? filters = null)
        => new(TenantId: 1, From: new DateOnly(2026, 1, 1), To: new DateOnly(2026, 12, 31),
            Filters: filters ?? new Dictionary<string, string?>(), BranchId: branchId,
            IgnoreBranchFilter: ignoreBranch);

    // UTC-H15-01 — ca 3 bao cao phai duoc dang ky trong registry
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void BaoCao_PhaiDuocDangKyTrongRegistry(string code)
    {
        var d = Registry.GetByCode(code);

        d.Should().NotBeNull();
        d!.Title.Should().NotBeNullOrWhiteSpace();
        d.Columns.Should().NotBeEmpty();
        d.PdfTypeCode.Should().NotBeNullOrWhiteSpace();
    }

    // UTC-H15-02 — SQL phai loc theo tenant (chong ro ri cheo phong kham)
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void Sql_PhaiLocTheoTenant(string code)
    {
        var (sql, _) = Registry.GetByCode(code)!.BuildQuery(Ctx());

        sql.Should().Contain("tenant_id = @tenantId");
    }

    // UTC-H15-03 — SQL phai loc theo chi nhanh (E/Dot0: khong bao cao nao duoc mu chi nhanh)
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void Sql_PhaiCoDieuKienLocChiNhanh(string code)
    {
        var (sql, _) = Registry.GetByCode(code)!.BuildQuery(Ctx());

        sql.Should().Contain("@ignoreBranch");
        sql.Should().Contain("@branchId");
    }

    // UTC-H15-04 — SQL phai loai ban ghi da xoa mem
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void Sql_PhaiLoaiBanGhiXoaMem(string code)
    {
        var (sql, _) = Registry.GetByCode(code)!.BuildQuery(Ctx());

        sql.Should().Contain("deleted_at IS NULL");
    }

    // UTC-H15-05 — tham so Dapper phai mang dung branchId/ignoreBranch tu ngu canh
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void ThamSo_PhaiMangDungNguCanhChiNhanh(string code)
    {
        var (_, prms) = Registry.GetByCode(code)!.BuildQuery(Ctx(branchId: 7, ignoreBranch: false));
        var dp = prms.Should().BeOfType<Dapper.DynamicParameters>().Subject;

        dp.ParameterNames.Should().Contain("branchId");
        dp.ParameterNames.Should().Contain("ignoreBranch");
        dp.Get<int>("branchId").Should().Be(7);
        dp.Get<bool>("ignoreBranch").Should().BeFalse();
        dp.Get<int>("tenantId").Should().Be(1);
    }

    // UTC-H15-06 — bao cao cong no chi lay goi CON NO va chua huy (dung nghiep vu)
    [Fact]
    public void BaoCaoCongNo_ChiLayGoiConNoVaChuaHuy()
    {
        var (sql, _) = Registry.GetByCode("package-outstanding-debt")!.BuildQuery(Ctx());

        sql.Should().Contain("amount_due > 0");
        sql.Should().Contain("status <> 'cancelled'");
    }

    // UTC-H15-07 — SQL phai co gioi han dong tra ve (chong treo he thong khi du lieu lon)
    [Theory]
    [InlineData("package-revenue")]
    [InlineData("package-utilization")]
    [InlineData("package-outstanding-debt")]
    public void Sql_PhaiCoGioiHanDong(string code)
    {
        var (sql, _) = Registry.GetByCode(code)!.BuildQuery(Ctx());

        sql.Should().Contain("LIMIT");
    }

    // UTC-H15-08 — ma bao cao phai duy nhat trong toan registry (tranh de PDF/code-gen)
    [Fact]
    public void MaBaoCao_PhaiDuyNhatTrongRegistry()
    {
        var codes = Registry.GetAll().Select(d => d.Code).ToList();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().Contain(PackageReportCodes);
    }

    // UTC-H15-09 — ma PdfTypeCode phai duy nhat (dung sinh ma so phieu in)
    [Fact]
    public void PdfTypeCode_PhaiDuyNhat()
    {
        var pdfCodes = Registry.GetAll().Select(d => d.PdfTypeCode).ToList();

        pdfCodes.Should().OnlyHaveUniqueItems();
    }
}
