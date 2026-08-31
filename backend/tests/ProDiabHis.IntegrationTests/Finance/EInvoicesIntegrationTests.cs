using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-EINVOICE-01 — kiem tra bao mat, phan quyen va tiep can endpoint hoa don dien tu.</summary>
[Collection("Api")]
public class EInvoicesIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public EInvoicesIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ITC-EINVOICE-01: chua dang nhap lay danh sach hoa don dien tu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachHoaDonDienTu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/einvoices");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: chua dang nhap phat hanh hoa don dien tu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_PhatHanhHoaDonDienTu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/einvoices/issue", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: chua dang nhap xem chi tiet hoa don dien tu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietHoaDonDienTu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/einvoices/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: chua dang nhap huy hoa don dien tu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_HuyHoaDonDienTu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/einvoices/{SampleId}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: chua dang nhap tai XML hoa don dien tu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaiXmlHoaDonDienTu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/einvoices/{SampleId}/xml-download");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: token het han lay danh sach hoa don dien tu phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachHoaDonDienTu_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/einvoices");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-EINVOICE-01: thieu quyen einvoice.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachHoaDonDienTu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/einvoices");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EINVOICE-01: thieu quyen einvoice.issue khi phat hanh phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_PhatHanhHoaDonDienTu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/einvoices/issue", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EINVOICE-01: thieu quyen einvoice.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietHoaDonDienTu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/einvoices/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EINVOICE-01: thieu quyen einvoice.cancel khi huy phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_HuyHoaDonDienTu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/einvoices/{SampleId}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EINVOICE-01: thieu quyen einvoice.read khi tai XML phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaiXmlHoaDonDienTu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/einvoices/{SampleId}/xml-download");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-EINVOICE-01: co quyen einvoice.read thi truy cap duoc danh sach hoa don dien tu
    [ApiFact]
    public async Task CoQuyen_LayDanhSachHoaDonDienTu_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("einvoice.read").GetAsync("/api/v1/einvoices");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
