using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-BILLING-01 — kiem tra bao mat, phan quyen va tiep can endpoint hoa don (billings).</summary>
[Collection("Api")]
public class BillingsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public BillingsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ITC-BILLING-01: chua dang nhap goi GET danh sach hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/billings");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap tao hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/billings", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap xem chi tiet hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/billings/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap cap nhat hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/billings/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap them dong dich vu vao hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_ThemDongHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/items", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap xoa dong hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaDongHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/billings/items/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap chot hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_ChotHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/finalize", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap huy hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_HuyHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/void", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap xem truoc hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemTruocHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/billings/{SampleId}/preview");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap xuat PDF hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XuatPdfHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/billings/{SampleId}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap ap dung BHYT phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_ApDungBhyt_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/apply-bhyt", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap lay hoa don theo luot kham phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayHoaDonTheoLuotKham_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/billings/encounter/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap sinh QR dong phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_SinhQrDong_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/qr-dynamic", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: chua dang nhap in hoa don phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_InHoaDon_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/billings/{SampleId}/print", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: token het han goi danh sach hoa don phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachHoaDon_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/billings");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BILLING-01: thieu quyen billing.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/billings");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.create khi tao hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/billings", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/billings/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.update khi cap nhat phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/billings/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.update khi them dong hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_ThemDongHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/billings/{SampleId}/items", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.update khi xoa dong hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XoaDongHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/billings/items/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.finalize khi chot hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_ChotHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/billings/{SampleId}/finalize", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.void khi huy hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_HuyHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/billings/{SampleId}/void", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.apply_bhyt khi ap dung BHYT phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_ApDungBhyt_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/billings/{SampleId}/apply-bhyt", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.print khi in hoa don phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_InHoaDon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/billings/{SampleId}/print", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: thieu quyen billing.read khi lay hoa don theo luot kham phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayHoaDonTheoLuotKham_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/billings/encounter/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BILLING-01: co quyen billing.read thi truy cap duoc danh sach hoa don
    [ApiFact]
    public async Task CoQuyen_LayDanhSachHoaDon_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("billing.read").GetAsync("/api/v1/billings");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
