using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-PAYMENT-01 — kiem tra bao mat, phan quyen va tiep can endpoint thanh toan (payments).</summary>
[Collection("Api")]
public class PaymentsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PaymentsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ITC-PAYMENT-01: chua dang nhap lay danh sach thanh toan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/payments");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap tao phieu thu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhieuThu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/payments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap xem chi tiet thanh toan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/payments/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap hoan tien phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_HoanTien_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/payments/{SampleId}/refund", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap huy phieu thu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_HuyPhieuThu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/payments/{SampleId}/void", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap lay danh muc hinh thuc thanh toan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayHinhThucThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/payments/methods");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap sinh QR thanh toan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_SinhQrThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/payments/qr/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap tra cuu trang thai QR phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TraCuuTrangThaiQr_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/payments/qr/{SampleId}/status");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: chua dang nhap quet the phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_QuetTheThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/payments/card/charge", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: webhook QR la endpoint cong khai, khong duoc tra 401
    [ApiFact]
    public async Task WebhookQr_LaCongKhai_KhongTra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/payments/qr/webhook/vietqr", new { });
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        ((int)res.StatusCode).Should().BeLessThan(500);
    }

    // ITC-PAYMENT-01: token het han goi danh sach thanh toan phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachThanhToan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/payments");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PAYMENT-01: thieu quyen payment.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/payments");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.collect khi tao phieu thu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhieuThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/payments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/payments/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.refund khi hoan tien phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_HoanTien_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/payments/{SampleId}/refund", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.void khi huy phieu thu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_HuyPhieuThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/payments/{SampleId}/void", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.read khi lay danh muc hinh thuc thanh toan phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayHinhThucThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/payments/methods");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment_qr.generate khi sinh QR phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_SinhQrThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/payments/qr/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.read khi tra cuu trang thai QR phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TraCuuTrangThaiQr_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/payments/qr/{SampleId}/status");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: thieu quyen payment.collect khi quet the phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_QuetTheThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/payments/card/charge", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PAYMENT-01: co quyen payment.read thi truy cap duoc danh sach thanh toan
    [ApiFact]
    public async Task CoQuyen_LayDanhSachThanhToan_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("payment.read").GetAsync("/api/v1/payments");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-PAYMENT-01: co quyen payment.read thi truy cap duoc danh muc hinh thuc thanh toan
    [ApiFact]
    public async Task CoQuyen_LayHinhThucThanhToan_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("payment.read").GetAsync("/api/v1/payments/methods");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
