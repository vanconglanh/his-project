using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-DRUGPRICE-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API gia thuoc theo chi nhanh.</summary>
[Collection("Api")]
public class DrugPriceOverridesApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public DrugPriceOverridesApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "22222222-2222-2222-2222-222222222222";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-DRUGPRICE-01: GET danh sach gia override khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/drug-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUGPRICE-01: GET chi tiet gia override khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/drug-price-overrides/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUGPRICE-01: POST tao gia override khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/drug-price-overrides", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUGPRICE-01: PUT cap nhat gia override khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/drug-price-overrides/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUGPRICE-01: DELETE xoa gia override khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaGiaOverride_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/drug-price-overrides/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-DRUGPRICE-01: thieu quyen drug.price_override khi lay danh sach phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/drug-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUGPRICE-01: thieu quyen drug.price_override khi xem chi tiet phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/drug-price-overrides/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUGPRICE-01: thieu quyen drug.price_override khi tao moi phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/drug-price-overrides", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUGPRICE-01: thieu quyen drug.price_override khi cap nhat phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/drug-price-overrides/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUGPRICE-01: thieu quyen drug.price_override khi xoa phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaGiaOverride_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/drug-price-overrides/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-DRUGPRICE-01: co quyen drug.price_override thi lay duoc danh sach gia override
    [ApiFact]
    public async Task CoQuyen_LayDanhSachGiaOverride_KhongBiChan()
    {
        var res = await _fx.ClientWith("drug.price_override").GetAsync("/api/v1/drug-price-overrides");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DRUGPRICE-01: token het han khi lay danh sach gia override phai 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachGiaOverride_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/drug-price-overrides");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
