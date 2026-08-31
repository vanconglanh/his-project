using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-PACKAGE-01 — kiem tra bao mat, phan quyen va tiep can endpoint goi dinh muc tra truoc.</summary>
[Collection("Api")]
public class PackagesIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PackagesIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    // ITC-PACKAGE-01: chua dang nhap lay danh sach goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/packages");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: chua dang nhap xem chi tiet goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: chua dang nhap tao goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/packages", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: chua dang nhap cap nhat goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/packages/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: chua dang nhap xoa goi phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaGoi_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: token het han lay danh sach goi phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachGoi_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/packages");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PACKAGE-01: thieu quyen package.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/packages");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PACKAGE-01: thieu quyen package.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PACKAGE-01: thieu quyen package.create khi tao goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/packages", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PACKAGE-01: thieu quyen package.update khi cap nhat goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/packages/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PACKAGE-01: thieu quyen package.delete khi xoa goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XoaGoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PACKAGE-01: co quyen package.read thi truy cap duoc danh sach goi
    [ApiFact]
    public async Task CoQuyen_LayDanhSachGoi_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("package.read").GetAsync("/api/v1/packages");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
