using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-PKGSUB-01 — kiem tra bao mat, phan quyen va tiep can endpoint dang ky goi cua benh nhan.</summary>
[Collection("Api")]
public class PackageSubscriptionsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PackageSubscriptionsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // ITC-PKGSUB-01: chua dang nhap lay danh sach dang ky goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachDangKyGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/package-subscriptions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap xem chi tiet dang ky goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietDangKyGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/package-subscriptions/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap ban goi cho benh nhan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_BanGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/package-subscriptions", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap thu tien goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_ThuTienGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/payments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap huy goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_HuyGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap gia han goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_GiaHanGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/extend", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: chua dang nhap xem tong hop goi cua benh nhan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemTongHopGoiBenhNhan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{SampleId}/package-summary");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: token het han lay danh sach dang ky goi phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachDangKyGoi_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/package-subscriptions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachDangKyGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/package-subscriptions");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietDangKyGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/package-subscriptions/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.sell khi ban goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_BanGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/package-subscriptions", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.collect khi thu tien goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_ThuTienGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/payments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.cancel khi huy goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_HuyGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.extend khi gia han goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_GiaHanGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/package-subscriptions/{SampleId}/extend", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: thieu quyen package_subscription.read khi xem tong hop goi benh nhan phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemTongHopGoiBenhNhan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{SampleId}/package-summary");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PKGSUB-01: co quyen package_subscription.read thi truy cap duoc danh sach dang ky goi
    [ApiFact]
    public async Task CoQuyen_LayDanhSachDangKyGoi_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("package_subscription.read").GetAsync("/api/v1/package-subscriptions");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
