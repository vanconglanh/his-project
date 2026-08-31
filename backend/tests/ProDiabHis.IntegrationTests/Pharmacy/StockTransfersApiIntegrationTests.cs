using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-TRANSFER-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API dieu chuyen kho noi bo.</summary>
[Collection("Api")]
public class StockTransfersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public StockTransfersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "ST-TEST-001";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-TRANSFER-01: GET danh sach phieu dieu chuyen khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachPhieuDieuChuyen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/stock-transfers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: GET chi tiet phieu dieu chuyen khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietPhieuDieuChuyen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/stock-transfers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST tao phieu dieu chuyen khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhieuDieuChuyen_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/stock-transfers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST trinh duyet phieu khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TrinhDuyetPhieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST duyet phieu khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_DuyetPhieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/approve", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST tu choi phieu khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TuChoiPhieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/reject", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST xuat hang khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XuatHang_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/ship", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST nhan hang khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_NhanHang_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/receive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST nhan hang mot phan khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_NhanHangMotPhan_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/partial-receive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST dong phieu khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_DongPhieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TRANSFER-01: POST huy phieu khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_HuyPhieu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-TRANSFER-01: thieu quyen stock_transfer.read khi lay danh sach phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachPhieuDieuChuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/stock-transfers");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.read khi xem chi tiet phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietPhieuDieuChuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/stock-transfers/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.create khi tao phieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhieuDieuChuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/stock-transfers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.create khi trinh duyet phai 403
    [ApiFact]
    public async Task ThieuQuyen_TrinhDuyetPhieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.approve khi duyet phieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DuyetPhieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/approve", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.approve khi tu choi phieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuChoiPhieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/reject", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.ship khi xuat hang phai 403
    [ApiFact]
    public async Task ThieuQuyen_XuatHang_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/ship", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.receive khi nhan hang phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhanHang_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/receive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.receive khi nhan hang mot phan phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhanHangMotPhan_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/partial-receive", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.receive khi dong phieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DongPhieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TRANSFER-01: thieu quyen stock_transfer.create khi huy phieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_HuyPhieu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/stock-transfers/{Id}/cancel", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-TRANSFER-01: co quyen stock_transfer.read thi lay duoc danh sach phieu dieu chuyen
    [ApiFact]
    public async Task CoQuyen_LayDanhSachPhieuDieuChuyen_KhongBiChan()
    {
        var res = await _fx.ClientWith("stock_transfer.read").GetAsync("/api/v1/stock-transfers");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-TRANSFER-01: token het han khi lay danh sach phieu dieu chuyen phai 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachPhieuDieuChuyen_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/stock-transfers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
