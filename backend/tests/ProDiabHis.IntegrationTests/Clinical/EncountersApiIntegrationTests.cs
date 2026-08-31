using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-ENCOUNTER-xx — bao mat + phan quyen cho EncountersController (/api/v1/encounters).</summary>
[Collection("Api")]
public class EncountersApiIntegrationTests
{
    private const string Eid = "33333333-3333-3333-3333-333333333333";
    private const string Did = "44444444-4444-4444-4444-444444444444";

    private readonly ApiTestFixture _fx;

    public EncountersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-ENCOUNTER-01: GET danh sach luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-02: POST tao luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/encounters", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-03: GET canh bao qua 12h khi chua dang nhap -> 401
    [ApiFact]
    public async Task CanhBaoQua12h_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/encounters/alerts/over-12h");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-04: GET chi tiet luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task ChiTietLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-05: PUT cap nhat luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/encounters/{Eid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-06: POST bat dau kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task BatDauKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/start", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-07: POST ket thuc kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task KetThucKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-08: PUT ly do den kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task LyDoDenKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/encounters/{Eid}/chief-complaint", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-09: POST them chan doan khi chua dang nhap -> 401
    [ApiFact]
    public async Task ThemChanDoan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/diagnoses", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-10: DELETE chan doan khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaChanDoan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/encounters/{Eid}/diagnoses/{Did}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-11: GET timeline luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task TimelineLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/timeline");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-12: GET trang thai khoa benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task TrangThaiKhoaBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/lock-state");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-13: POST tao ban dinh chinh khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoBanDinhChinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/addenda", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-14: GET lich su dinh chinh khi chua dang nhap -> 401
    [ApiFact]
    public async Task LichSuDinhChinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/addenda");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCOUNTER-15: GET danh sach luot kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-16: POST tao luot kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/encounters", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-17: GET canh bao qua 12h thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CanhBaoQua12h_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/encounters/alerts/over-12h");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-18: GET chi tiet luot kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ChiTietLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-19: PUT cap nhat luot kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CapNhatLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/encounters/{Eid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-20: POST bat dau kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task BatDauKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/start", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-21: POST ket thuc kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task KetThucKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-22: POST bat dau kham chi co encounter.read -> 403 (thieu encounter.start)
    [ApiFact]
    public async Task BatDauKham_ChiCoQuyenDoc_Tra403()
    {
        var res = await _fx.ClientWith("encounter.read").PostAsJsonAsync($"/api/v1/encounters/{Eid}/start", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-23: PUT ly do den kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LyDoDenKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/encounters/{Eid}/chief-complaint", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-24: POST them chan doan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ThemChanDoan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/diagnoses", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-25: DELETE chan doan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaChanDoan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/encounters/{Eid}/diagnoses/{Did}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-26: GET timeline thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TimelineLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/timeline");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-27: GET trang thai khoa benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TrangThaiKhoaBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/lock-state");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-28: POST tao ban dinh chinh thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoBanDinhChinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/addenda", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-29: GET lich su dinh chinh thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LichSuDinhChinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/addenda");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCOUNTER-30: GET danh sach luot kham voi dung quyen encounter.read -> 200
    [ApiFact]
    public async Task DanhSachLuotKham_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("encounter.read").GetAsync("/api/v1/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-ENCOUNTER-31: GET canh bao qua 12h voi dung quyen encounter.read -> khong loi he thong
    [ApiFact]
    public async Task CanhBaoQua12h_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("encounter.read").GetAsync("/api/v1/encounters/alerts/over-12h");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-ENCOUNTER-32: GET danh sach luot kham voi token het han -> 401
    [ApiFact]
    public async Task DanhSachLuotKham_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
