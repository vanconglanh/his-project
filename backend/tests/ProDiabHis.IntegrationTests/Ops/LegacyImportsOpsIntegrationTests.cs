using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-LEGACY-01 — Kiem tra bao mat va phan quyen module Nhap du lieu he thong cu.</summary>
[Collection("Api")]
public class LegacyImportsOpsIntegrationTests
{
    private const string Rid = "66666666-6666-6666-6666-666666666666";
    private readonly ApiTestFixture _fx;

    public LegacyImportsOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-LEGACY-01: chua dang nhap tao phien nhap du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhienNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync("/api/v1/legacy-imports", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap xem danh sach phien nhap phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachPhienNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/legacy-imports");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap xem chi tiet phien nhap phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietPhienNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/legacy-imports/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap xem dong du lieu phien nhap phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DongDuLieuPhienNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/legacy-imports/{Rid}/items");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap doi chieu dong du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoiChieuDongDuLieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/legacy-imports/items/{Rid}/match", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap xac nhan dong du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XacNhanDongDuLieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync($"/api/v1/legacy-imports/items/{Rid}/confirm", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: chua dang nhap tu choi dong du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TuChoiDongDuLieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync($"/api/v1/legacy-imports/items/{Rid}/reject", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LEGACY-01: thieu quyen xem danh sach phien nhap phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachPhienNhap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/legacy-imports");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen tao phien nhap phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhienNhap_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync("/api/v1/legacy-imports", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen xem chi tiet phien nhap phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietPhienNhap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/legacy-imports/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen xem dong du lieu phien nhap phai 403
    [ApiFact]
    public async Task ThieuQuyen_DongDuLieuPhienNhap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/legacy-imports/{Rid}/items");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen doi chieu dong du lieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoiChieuDongDuLieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/legacy-imports/items/{Rid}/match", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen xac nhan dong du lieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_XacNhanDongDuLieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync($"/api/v1/legacy-imports/items/{Rid}/confirm", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: thieu quyen tu choi dong du lieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuChoiDongDuLieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync($"/api/v1/legacy-imports/items/{Rid}/reject", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LEGACY-01: dung quyen xem danh sach phien nhap khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachPhienNhap_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("legacy_import.write").GetAsync("/api/v1/legacy-imports");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
