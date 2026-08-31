using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-CLSORDER-01 — Bao mat va phan quyen cho API chi dinh CLS (XN/CDHA).</summary>
[Collection("Api")]
public class ClsOrdersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ClsOrdersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Enc = Guid.NewGuid().ToString();
    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-CLSORDER-01: tao chi dinh XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoChiDinhXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/lab-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: xem danh sach chi dinh XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachChiDinhXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/lab-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: cap nhat trang thai chi dinh XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task CapNhatChiDinhXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PutAsJsonAsync($"/api/v1/lab-orders/{Id}", new { status = "DONE" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: xoa chi dinh XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XoaChiDinhXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/lab-orders/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: tao chi dinh CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoChiDinhChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/rad-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: xem danh sach chi dinh CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachChiDinhChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/rad-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: cap nhat trang thai chi dinh CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task CapNhatChiDinhChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PutAsJsonAsync($"/api/v1/rad-orders/{Id}", new { status = "DONE" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: xoa chi dinh CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XoaChiDinhChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/rad-orders/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: in phieu chi dinh XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task InPhieuChiDinhXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/lab-orders/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: in phieu chi dinh CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task InPhieuChiDinhChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/rad-orders/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: danh sach chi dinh XN qua han khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachChiDinhQuaHan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-orders/overdue");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: tra cuu danh muc dich vu CLS khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TraCuuDanhMucCls_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cls-catalog/tests");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSORDER-01: token het han khong duoc truy cap danh muc CLS
    [ApiFact]
    public async Task TraCuuDanhMucCls_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/cls-catalog/tests");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-CLSORDER-01: thieu quyen lab_order.read khong xem duoc danh sach chi dinh XN
    [ApiFact]
    public async Task DanhSachChiDinhXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/lab-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.create khong tao duoc chi dinh XN
    [ApiFact]
    public async Task TaoChiDinhXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/lab-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.update khong cap nhat duoc chi dinh XN
    [ApiFact]
    public async Task CapNhatChiDinhXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PutAsJsonAsync($"/api/v1/lab-orders/{Id}", new { status = "DONE" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.delete khong xoa duoc chi dinh XN
    [ApiFact]
    public async Task XoaChiDinhXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/lab-orders/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen rad_order.create khong tao duoc chi dinh CDHA
    [ApiFact]
    public async Task TaoChiDinhChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/encounters/{Enc}/rad-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen rad_order.read khong xem duoc danh sach chi dinh CDHA
    [ApiFact]
    public async Task DanhSachChiDinhChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/rad-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen rad_order.update khong cap nhat duoc chi dinh CDHA
    [ApiFact]
    public async Task CapNhatChiDinhChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PutAsJsonAsync($"/api/v1/rad-orders/{Id}", new { status = "DONE" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen rad_order.delete khong xoa duoc chi dinh CDHA
    [ApiFact]
    public async Task XoaChiDinhChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/rad-orders/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.read khong in duoc phieu chi dinh XN
    [ApiFact]
    public async Task InPhieuChiDinhXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/lab-orders/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen rad_order.read khong in duoc phieu chi dinh CDHA
    [ApiFact]
    public async Task InPhieuChiDinhChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/rad-orders/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.read khong xem duoc chi dinh qua han
    [ApiFact]
    public async Task DanhSachChiDinhQuaHan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-orders/overdue");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSORDER-01: thieu quyen lab_order.read khong tra cuu duoc danh muc CLS
    [ApiFact]
    public async Task TraCuuDanhMucCls_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cls-catalog/tests");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-CLSORDER-01: co quyen lab_order.read thi xem duoc danh sach chi dinh qua han
    [ApiFact]
    public async Task DanhSachChiDinhQuaHan_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_order.read").GetAsync("/api/v1/lab-orders/overdue");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-CLSORDER-01: co quyen lab_order.read thi tra cuu duoc danh muc CLS
    [ApiFact]
    public async Task TraCuuDanhMucCls_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_order.read").GetAsync("/api/v1/cls-catalog/tests?limit=5");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
