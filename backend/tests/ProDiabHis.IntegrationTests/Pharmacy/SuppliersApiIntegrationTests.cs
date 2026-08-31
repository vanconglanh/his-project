using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-SUPPLIER-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API nha cung cap.</summary>
[Collection("Api")]
public class SuppliersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public SuppliersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "SUP-TEST-001";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-SUPPLIER-01: GET danh sach nha cung cap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachNhaCungCap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/suppliers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SUPPLIER-01: GET chi tiet nha cung cap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietNhaCungCap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/suppliers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SUPPLIER-01: POST tao nha cung cap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoNhaCungCap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/suppliers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SUPPLIER-01: PUT cap nhat nha cung cap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatNhaCungCap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/suppliers/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-SUPPLIER-01: DELETE xoa nha cung cap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaNhaCungCap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/suppliers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-SUPPLIER-01: thieu quyen supplier.read khi lay danh sach phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachNhaCungCap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/suppliers");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SUPPLIER-01: thieu quyen supplier.read khi xem chi tiet phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietNhaCungCap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/suppliers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SUPPLIER-01: thieu quyen supplier.write khi tao moi phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoNhaCungCap_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/suppliers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SUPPLIER-01: thieu quyen supplier.write khi cap nhat phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatNhaCungCap_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/suppliers/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-SUPPLIER-01: thieu quyen supplier.write khi xoa phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaNhaCungCap_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/suppliers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-SUPPLIER-01: co quyen supplier.read thi lay duoc danh sach nha cung cap
    [ApiFact]
    public async Task CoQuyen_LayDanhSachNhaCungCap_KhongBiChan()
    {
        var res = await _fx.ClientWith("supplier.read").GetAsync("/api/v1/suppliers");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-SUPPLIER-01: token het han khi lay danh sach nha cung cap phai 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachNhaCungCap_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/suppliers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
