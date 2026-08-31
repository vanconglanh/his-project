using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-VITAL-xx — bao mat + phan quyen cho VitalSignsController (sinh hieu).</summary>
[Collection("Api")]
public class VitalSignsApiIntegrationTests
{
    private const string Eid = "55555555-5555-5555-5555-555555555555";
    private const string Vid = "66666666-6666-6666-6666-666666666666";
    private const string Pid = "77777777-7777-7777-7777-777777777777";

    private readonly ApiTestFixture _fx;

    public VitalSignsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-VITAL-01: POST ghi sinh hieu khi chua dang nhap -> 401
    [ApiFact]
    public async Task GhiSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/vital-signs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-02: GET danh sach sinh hieu theo luot kham khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/vital-signs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-03: GET sinh hieu moi nhat khi chua dang nhap -> 401
    [ApiFact]
    public async Task SinhHieuMoiNhat_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/vital-signs/latest");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-04: POST ghi hang loat sinh hieu khi chua dang nhap -> 401
    [ApiFact]
    public async Task GhiHangLoatSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/vital-signs/batch", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-05: PUT sua sinh hieu khi chua dang nhap -> 401
    [ApiFact]
    public async Task SuaSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/vital-signs/{Vid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-06: DELETE sinh hieu khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/vital-signs/{Vid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-07: GET lich su sinh hieu benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task LichSuSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/vital-signs/history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-08: GET xu huong sinh hieu khi chua dang nhap -> 401
    [ApiFact]
    public async Task XuHuongSinhHieu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/vital-signs/trend?metric=weight");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-VITAL-09: POST ghi sinh hieu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task GhiSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/vital-signs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-10: GET danh sach sinh hieu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/vital-signs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-11: GET sinh hieu moi nhat thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task SinhHieuMoiNhat_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/vital-signs/latest");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-12: POST ghi hang loat thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task GhiHangLoatSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/vital-signs/batch", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-13: PUT sua sinh hieu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task SuaSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/vital-signs/{Vid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-14: DELETE sinh hieu chi co quyen ghi (thieu vital_sign.delete) -> 403
    [ApiFact]
    public async Task XoaSinhHieu_ChiCoQuyenGhi_Tra403()
    {
        var res = await _fx.ClientWith("vital_sign.write").DeleteAsync($"/api/v1/vital-signs/{Vid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-15: GET lich su sinh hieu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LichSuSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/vital-signs/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-16: GET xu huong sinh hieu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XuHuongSinhHieu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/vital-signs/trend?metric=weight");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-VITAL-17: GET danh sach sinh hieu voi dung quyen vital_sign.read -> khong loi he thong
    [ApiFact]
    public async Task DanhSachSinhHieu_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("vital_sign.read").GetAsync($"/api/v1/encounters/{Eid}/vital-signs");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-VITAL-18: GET lich su sinh hieu voi dung quyen vital_sign.read -> khong loi he thong
    [ApiFact]
    public async Task LichSuSinhHieu_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("vital_sign.read").GetAsync($"/api/v1/patients/{Pid}/vital-signs/history");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
