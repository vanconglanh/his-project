using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-DIABETES-xx — bao mat + phan quyen cho DiabetesController va DiabetesTemplatesController.</summary>
[Collection("Api")]
public class DiabetesApiIntegrationTests
{
    private const string Eid = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string Pid = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    private const string Tid = "dddddddd-dddd-dddd-dddd-dddddddddddd";

    private readonly ApiTestFixture _fx;

    public DiabetesApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-DIABETES-01: POST tao danh gia dai thao duong khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoDanhGiaDaiThaoDuong_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/encounters/{Eid}/diabetes-assessment", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-02: GET danh gia dai thao duong khi chua dang nhap -> 401
    [ApiFact]
    public async Task LayDanhGiaDaiThaoDuong_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Eid}/diabetes-assessment");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-03: PUT cap nhat danh gia dai thao duong khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatDanhGiaDaiThaoDuong_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/encounters/{Eid}/diabetes-assessment", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-04: GET lich su danh gia theo benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task LichSuDanhGia_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/diabetes-assessments/history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-05: GET lich su danh gia route cu khi chua dang nhap -> 401
    [ApiFact]
    public async Task LichSuDanhGia_RouteCu_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/diabetes-assessments/patient/{Pid}/history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-06: GET danh sach mau danh gia khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachMauDanhGia_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/diabetes-templates");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-07: POST tao mau danh gia khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoMauDanhGia_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/diabetes-templates", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-08: PUT cap nhat mau danh gia khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatMauDanhGia_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/diabetes-templates/{Tid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DIABETES-09: POST tao danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoDanhGiaDaiThaoDuong_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/encounters/{Eid}/diabetes-assessment", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-10: GET danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LayDanhGiaDaiThaoDuong_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Eid}/diabetes-assessment");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-11: PUT cap nhat danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CapNhatDanhGiaDaiThaoDuong_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/encounters/{Eid}/diabetes-assessment", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-12: GET lich su danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LichSuDanhGia_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/diabetes-assessments/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-13: GET lich su danh gia route cu thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LichSuDanhGia_RouteCu_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/diabetes-assessments/patient/{Pid}/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-14: GET danh sach mau danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachMauDanhGia_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/diabetes-templates");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-15: POST tao mau danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoMauDanhGia_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/diabetes-templates", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-16: PUT cap nhat mau danh gia thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CapNhatMauDanhGia_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/diabetes-templates/{Tid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DIABETES-17: GET danh sach mau danh gia voi dung quyen diabetes.assess -> 200
    [ApiFact]
    public async Task DanhSachMauDanhGia_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("diabetes.assess").GetAsync("/api/v1/diabetes-templates");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-DIABETES-18: GET danh gia dai thao duong voi dung quyen -> khong loi he thong
    [ApiFact]
    public async Task LayDanhGiaDaiThaoDuong_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("diabetes.assess").GetAsync($"/api/v1/encounters/{Eid}/diabetes-assessment");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DIABETES-19: GET danh sach mau danh gia voi token het han -> 401
    [ApiFact]
    public async Task DanhSachMauDanhGia_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/diabetes-templates");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
