using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-IBDEBT-01 — kiem tra bao mat, phan quyen va tiep can endpoint cong no noi bo giua chi nhanh.</summary>
[Collection("Api")]
public class InterBranchDebtsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public InterBranchDebtsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    // ITC-IBDEBT-01: chua dang nhap lay danh sach cong no noi bo phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachCongNoNoiBo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/inter-branch-debts");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-IBDEBT-01: chua dang nhap tat toan cong no noi bo phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TatToanCongNoNoiBo_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/inter-branch-debts/{SampleId}/settle", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-IBDEBT-01: token het han lay danh sach cong no noi bo phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachCongNoNoiBo_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/inter-branch-debts");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-IBDEBT-01: thieu quyen inter_branch_debt.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachCongNoNoiBo_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/inter-branch-debts");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-IBDEBT-01: thieu quyen inter_branch_debt.settle khi tat toan phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TatToanCongNoNoiBo_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/inter-branch-debts/{SampleId}/settle", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-IBDEBT-01: co quyen inter_branch_debt.read thi truy cap duoc danh sach cong no noi bo
    [ApiFact]
    public async Task CoQuyen_LayDanhSachCongNoNoiBo_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("inter_branch_debt.read").GetAsync("/api/v1/inter-branch-debts");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
