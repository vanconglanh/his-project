using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-CLSROUND-01 — Bao mat va phan quyen cho API dot chi dinh CLS (G01/G02).</summary>
[Collection("Api")]
public class ClsRoundsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ClsRoundsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Enc = Guid.NewGuid().ToString();
    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-CLSROUND-01: tao dot chi dinh khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/cls-rounds", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: danh sach dot chi dinh theo luot kham khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/cls-rounds");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: xem chi tiet dot chi dinh khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ChiTietDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/cls-rounds/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: chot dot chi dinh khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ChotDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: danh dau da thanh toan khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ThanhToanDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/pay", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: mien/no vien phi khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task MienVienPhiDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/waive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: huy dot chi dinh khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task HuyDotChiDinh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSROUND-01: token het han khong xem duoc danh sach dot chi dinh
    [ApiFact]
    public async Task DanhSachDotChiDinh_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired())
            .GetAsync($"/api/v1/encounters/{Enc}/cls-rounds");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-CLSROUND-01: thieu quyen cls_round.create khong tao duoc dot chi dinh
    [ApiFact]
    public async Task TaoDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/cls-rounds", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.read khong xem duoc danh sach dot chi dinh
    [ApiFact]
    public async Task DanhSachDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/cls-rounds");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.read khong xem duoc chi tiet dot chi dinh
    [ApiFact]
    public async Task ChiTietDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/cls-rounds/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.submit khong chot duoc dot chi dinh
    [ApiFact]
    public async Task ChotDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.pay khong thanh toan duoc dot chi dinh
    [ApiFact]
    public async Task ThanhToanDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/pay", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.waive khong mien duoc vien phi
    [ApiFact]
    public async Task MienVienPhiDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/waive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSROUND-01: thieu quyen cls_round.cancel khong huy duoc dot chi dinh
    [ApiFact]
    public async Task HuyDotChiDinh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/cls-rounds/{Id}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-CLSROUND-01: co quyen cls_round.read thi truy cap duoc danh sach dot chi dinh
    [ApiFact]
    public async Task DanhSachDotChiDinh_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cls_round.read").GetAsync($"/api/v1/encounters/{Enc}/cls-rounds");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
