using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-DIABETES-DASH-xx — bao mat + phan quyen cho DiabetesDashboardController.</summary>
[Collection("Api")]
public class DiabetesDashboardApiIntegrationTests
{
    private const string Pid = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    private readonly ApiTestFixture _fx;

    public DiabetesDashboardApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-DIABETES-DASH-01: GET quy dao dieu tri khi chua dang nhap -> 401
    [ApiFact]
    public async Task QuyDaoDieuTri_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/diabetes/trajectory");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-DASH-02: GET co canh bao xau di khi chua dang nhap -> 401
    [ApiFact]
    public async Task CoCanhBaoXauDi_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/diabetes/deterioration-flags");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-DASH-03: GET danh sach nguy co khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachNguyCo_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/diabetes/risk-list");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-DASH-04: GET quy dao dieu tri thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task QuyDaoDieuTri_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/diabetes/trajectory");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-DASH-05: GET co canh bao xau di thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CoCanhBaoXauDi_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/diabetes/deterioration-flags");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-DASH-06: GET danh sach nguy co thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachNguyCo_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/diabetes/risk-list");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-DASH-07: GET danh sach nguy co voi quyen diabetes.assess (thieu risk.read) -> 403
    [ApiFact]
    public async Task DanhSachNguyCo_SaiQuyen_Tra403()
    {
        var res = await _fx.ClientWith("diabetes.assess").GetAsync("/api/v1/diabetes/risk-list");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-DASH-08: GET danh sach nguy co voi dung quyen risk.read -> khong loi he thong
    [ApiFact]
    public async Task DanhSachNguyCo_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("risk.read").GetAsync("/api/v1/diabetes/risk-list");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DIABETES-DASH-09: GET quy dao dieu tri voi dung quyen diabetes.assess -> khong loi he thong
    [ApiFact]
    public async Task QuyDaoDieuTri_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("diabetes.assess").GetAsync($"/api/v1/patients/{Pid}/diabetes/trajectory");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DIABETES-DASH-10: GET danh sach nguy co voi token het han -> 401
    [ApiFact]
    public async Task DanhSachNguyCo_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/diabetes/risk-list");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
