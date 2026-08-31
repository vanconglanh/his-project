using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-EMR-xx — bao mat + phan quyen cho EmrController va EmrTemplatesController.</summary>
[Collection("Api")]
public class EmrApiIntegrationTests
{
    private const string Eid = "88888888-8888-8888-8888-888888888888";
    private const string Ver = "99999999-9999-9999-9999-999999999999";
    private const string Tid = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    private readonly ApiTestFixture _fx;

    public EmrApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-EMR-01: GET benh an dien tu khi chua dang nhap -> 401
    [ApiFact]
    public async Task LayBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/emr");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-02: PUT luu nhap benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task LuuNhapBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/encounters/{Eid}/emr", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-03: POST ky benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task KyBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/emr/sign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-04: POST huy ky benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task HuyKyBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/emr/unsign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-05: GET xuat PDF benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task XuatPdfBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/emr/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-06: GET danh sach phien ban benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachPhienBan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/emr/versions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-07: GET so sanh phien ban benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task SoSanhPhienBan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/emr/versions/{Ver}/diff");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-08: GET danh sach mau benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachMauBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/emr-templates");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-09: GET chi tiet mau benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task ChiTietMauBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/emr-templates/{Tid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-10: POST tao mau benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoMauBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/emr-templates", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-11: PUT cap nhat mau benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatMauBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/emr-templates/{Tid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-12: DELETE mau benh an khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaMauBenhAn_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/emr-templates/{Tid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-13: GET danh sach mau theo route thay the /emr/templates khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachMauBenhAn_RouteThayThe_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/emr/templates");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EMR-14: GET benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LayBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/emr");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-15: PUT luu nhap benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LuuNhapBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/encounters/{Eid}/emr", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-16: POST ky benh an chi co emr.write (thieu emr.sign) -> 403
    [ApiFact]
    public async Task KyBenhAn_ChiCoQuyenGhi_Tra403()
    {
        var res = await _fx.ClientWith("emr.write").PostAsJsonAsync($"/api/v1/encounters/{Eid}/emr/sign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-17: POST huy ky benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task HuyKyBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/emr/unsign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-18: GET xuat PDF benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XuatPdfBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/emr/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-19: GET danh sach phien ban thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachPhienBan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/emr/versions");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-20: GET so sanh phien ban thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task SoSanhPhienBan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/emr/versions/{Ver}/diff");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-21: GET danh sach mau benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachMauBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/emr-templates");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-22: GET chi tiet mau benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ChiTietMauBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/emr-templates/{Tid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-23: POST tao mau benh an chi co quyen doc mau -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoMauBenhAn_ChiCoQuyenDoc_Tra403()
    {
        var res = await _fx.ClientWith("emr_template.read").PostAsJsonAsync("/api/v1/emr-templates", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-24: PUT cap nhat mau benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CapNhatMauBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/emr-templates/{Tid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-25: DELETE mau benh an thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaMauBenhAn_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/emr-templates/{Tid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EMR-26: GET danh sach mau benh an voi dung quyen emr_template.read -> 200
    [ApiFact]
    public async Task DanhSachMauBenhAn_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("emr_template.read").GetAsync("/api/v1/emr-templates");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-EMR-27: GET danh sach mau (route /emr/templates) voi dung quyen -> 200
    [ApiFact]
    public async Task DanhSachMauBenhAn_RouteThayThe_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("emr_template.read").GetAsync("/api/v1/emr/templates");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-EMR-28: GET benh an voi dung quyen emr.read -> khong loi he thong
    [ApiFact]
    public async Task LayBenhAn_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("emr.read").GetAsync($"/api/v1/encounters/{Eid}/emr");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
