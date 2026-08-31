using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-SVCPRICE-01 — kiem tra bao mat, phan quyen va tiep can endpoint gia override dich vu.</summary>
[Collection("Api")]
public class ServicePriceOverridesIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ServicePriceOverridesIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    // ITC-SVCPRICE-01: chua dang nhap lay danh sach gia override phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/service-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: chua dang nhap xem chi tiet gia override phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/service-price-overrides/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: chua dang nhap tao gia override phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/service-price-overrides", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: chua dang nhap cap nhat gia override phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/service-price-overrides/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: chua dang nhap xoa gia override phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/service-price-overrides/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: token het han lay danh sach gia override phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachGiaOverride_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/service-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SVCPRICE-01: thieu quyen service.price_override khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/service-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SVCPRICE-01: thieu quyen service.price_override khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/service-price-overrides/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SVCPRICE-01: thieu quyen service.price_override khi tao phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/service-price-overrides", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SVCPRICE-01: thieu quyen service.price_override khi cap nhat phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/service-price-overrides/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SVCPRICE-01: thieu quyen service.price_override khi xoa phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XoaGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/service-price-overrides/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SVCPRICE-01: co quyen service.price_override thi truy cap duoc danh sach gia override
    [ApiFact]
    public async Task CoQuyen_LayDanhSachGiaOverride_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("service.price_override").GetAsync("/api/v1/service-price-overrides");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
