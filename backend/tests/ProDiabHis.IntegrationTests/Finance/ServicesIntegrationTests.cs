using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-SERVICE-01 — kiem tra bao mat, phan quyen va tiep can endpoint danh muc dich vu.</summary>
[Collection("Api")]
public class ServicesCatalogIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ServicesCatalogIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    // ITC-SERVICE-01: chua dang nhap lay danh sach dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/services");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap tao dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/services", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap tim kiem dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TimKiemDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/services/search?q=kham");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap lay danh muc nhom dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayNhomDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/services/categories");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap import dich vu tu Excel phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_ImportDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync("/api/v1/services/import", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap xem chi tiet dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/services/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap cap nhat dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/services/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap xoa dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/services/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: token het han lay danh sach dich vu phai bi 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachDichVu_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/services");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: thieu quyen service.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/services");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.write khi tao dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/services", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.read khi tim kiem dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TimKiemDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/services/search?q=kham");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.write khi import dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_ImportDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync("/api/v1/services/import", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.read khi xem chi tiet dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/services/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.write khi cap nhat dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/services/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service.write khi xoa dich vu phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XoaDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/services/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: co quyen service.read thi truy cap duoc danh sach dich vu
    [ApiFact]
    public async Task CoQuyen_LayDanhSachDichVu_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("service.read").GetAsync("/api/v1/services");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-SERVICE-01: endpoint nhom dich vu chi can dang nhap, khong can permission
    [ApiFact]
    public async Task DaDangNhap_LayNhomDichVu_KhongLoiHeThong()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/services/categories");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}

/// <summary>ITC-SERVICE-01 — kiem tra bao mat, phan quyen va tiep can endpoint goi gia dich vu (service-packages).</summary>
[Collection("Api")]
public class ServicePackagesCatalogIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ServicePackagesCatalogIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("78787878-7878-7878-7878-787878787878");

    // ITC-SERVICE-01: chua dang nhap lay danh sach goi gia dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachGoiDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/service-packages");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap tao goi gia dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoGoiDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/service-packages", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap xem chi tiet goi gia dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietGoiDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/service-packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap cap nhat goi gia dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatGoiDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/service-packages/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: chua dang nhap xoa goi gia dich vu phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaGoiDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/service-packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SERVICE-01: thieu quyen service_package.read khi lay danh sach phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachGoiDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/service-packages");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service_package.write khi tao goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_TaoGoiDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/service-packages", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service_package.read khi xem chi tiet phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietGoiDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/service-packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service_package.write khi cap nhat goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatGoiDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/service-packages/{SampleId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: thieu quyen service_package.write khi xoa goi phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XoaGoiDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/service-packages/{SampleId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SERVICE-01: co quyen service_package.read thi truy cap duoc danh sach goi gia dich vu
    [ApiFact]
    public async Task CoQuyen_LayDanhSachGoiDichVu_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("service_package.read").GetAsync("/api/v1/service-packages");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
