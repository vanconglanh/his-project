using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-INBODY-xx — bao mat + phan quyen cho InBodyReportsController (ket qua may InBody).</summary>
[Collection("Api")]
public class InBodyReportsApiIntegrationTests
{
    private const string Pid = "12121212-1212-1212-1212-121212121212";
    private const string Rid = "34343434-3434-3434-3434-343434343434";

    private readonly ApiTestFixture _fx;

    public InBodyReportsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-INBODY-01: POST tai len ket qua InBody khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaiLenKetQuaInBody_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync($"/api/v1/patients/{Pid}/inbody-reports", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-INBODY-02: GET danh sach ket qua InBody khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachKetQuaInBody_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/inbody-reports");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-INBODY-03: POST xac nhan ket qua InBody khi chua dang nhap -> 401
    [ApiFact]
    public async Task XacNhanKetQuaInBody_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync($"/api/v1/inbody-reports/{Rid}/confirm", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-INBODY-04: DELETE ket qua InBody khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaKetQuaInBody_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/inbody-reports/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-INBODY-05: POST tai len ket qua InBody thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaiLenKetQuaInBody_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync($"/api/v1/patients/{Pid}/inbody-reports", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-INBODY-06: GET danh sach ket qua InBody thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachKetQuaInBody_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/inbody-reports");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-INBODY-07: POST xac nhan ket qua InBody thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XacNhanKetQuaInBody_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync($"/api/v1/inbody-reports/{Rid}/confirm", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-INBODY-08: DELETE ket qua InBody chi co patient.read -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaKetQuaInBody_ChiCoQuyenDoc_Tra403()
    {
        var res = await _fx.ClientWith("patient.read").DeleteAsync($"/api/v1/inbody-reports/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-INBODY-09: POST xac nhan ket qua InBody chi co patient.read -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XacNhanKetQuaInBody_ChiCoQuyenDoc_Tra403()
    {
        var res = await _fx.ClientWith("patient.read").PostAsync($"/api/v1/inbody-reports/{Rid}/confirm", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-INBODY-10: GET danh sach ket qua InBody voi dung quyen patient.read -> khong loi he thong
    [ApiFact]
    public async Task DanhSachKetQuaInBody_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("patient.read").GetAsync($"/api/v1/patients/{Pid}/inbody-reports");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-INBODY-11: GET danh sach ket qua InBody voi token het han -> 401
    [ApiFact]
    public async Task DanhSachKetQuaInBody_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync($"/api/v1/patients/{Pid}/inbody-reports");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
