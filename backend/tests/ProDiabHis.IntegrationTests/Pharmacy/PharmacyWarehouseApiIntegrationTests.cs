using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-WAREHOUSE-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API kho duoc.</summary>
[Collection("Api")]
public class PharmacyWarehouseApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PharmacyWarehouseApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string WhId = "WH-TEST-001";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-WAREHOUSE-01: GET danh sach kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachKho_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/warehouses");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: POST tao kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoKho_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/pharmacy/warehouses", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET chi tiet kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietKho_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/pharmacy/warehouses/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: PUT cap nhat kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatKho_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/pharmacy/warehouses/{WhId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: DELETE xoa kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaKho_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/pharmacy/warehouses/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET danh sach don mua khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachDonMua_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/purchase-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: POST tao don mua khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoDonMua_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/pharmacy/purchase-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: POST tao phieu nhap kho (GRN) khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhieuNhapKho_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/pharmacy/purchase-orders/{WhId}/grn", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET danh sach ton kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachTonKho_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/stocks");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET ton kho (alias so it) khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayTonKhoAlias_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/stock");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET chi tiet ton kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietTonKho_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/pharmacy/stock/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET ton kho duoi dinh muc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayTonKhoDuoiDinhMuc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/stock/low");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET thuoc sap het han khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayThuocSapHetHan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/stock/near-expiry");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: POST dieu chinh ton kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_DieuChinhTonKho_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/pharmacy/adjustments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET lich su xuat nhap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayLichSuXuatNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/movements");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: POST dieu chuyen kho khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_DieuChuyenKho_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/pharmacy/transfers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET canh bao ton thap khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayCanhBaoTonThap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/alerts/low-stock");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET canh bao sap het han khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayCanhBaoSapHetHan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/alerts/near-expiry");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET danh sach lo thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachLoThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/lots");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-WAREHOUSE-01: GET phieu kiem ke PDF khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayPhieuKiemKePdf_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/stocktake?warehouse_id=1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-WAREHOUSE-01: thieu quyen warehouse.read khi lay danh sach kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/warehouses");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.write khi tao kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/pharmacy/warehouses", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.read khi xem chi tiet kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/pharmacy/warehouses/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.write khi cap nhat kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/pharmacy/warehouses/{WhId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.write khi xoa kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/pharmacy/warehouses/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.read khi lay danh sach don mua phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachDonMua_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/purchase-orders");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.write khi tao don mua phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoDonMua_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/pharmacy/purchase-orders", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen warehouse.write khi tao phieu nhap kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhieuNhapKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/pharmacy/purchase-orders/{WhId}/grn", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay danh sach ton kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachTonKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/stocks");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi xem chi tiet ton kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietTonKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/pharmacy/stock/{WhId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay ton kho duoi dinh muc phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayTonKhoDuoiDinhMuc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/stock/low");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay thuoc sap het han phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayThuocSapHetHan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/stock/near-expiry");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.adjust khi dieu chinh ton kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_DieuChinhTonKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/pharmacy/adjustments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay lich su xuat nhap phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayLichSuXuatNhap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/movements");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.adjust khi dieu chuyen kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_DieuChuyenKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/pharmacy/transfers", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay canh bao ton thap phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayCanhBaoTonThap_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/alerts/low-stock");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay canh bao sap het han phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayCanhBaoSapHetHan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/alerts/near-expiry");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay danh sach lo thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachLoThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/lots");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-WAREHOUSE-01: thieu quyen stock.read khi lay phieu kiem ke PDF phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayPhieuKiemKePdf_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/stocktake?warehouse_id=1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-WAREHOUSE-01: co quyen warehouse.read thi lay duoc danh sach kho
    [ApiFact]
    public async Task CoQuyen_LayDanhSachKho_KhongBiChan()
    {
        var res = await _fx.ClientWith("warehouse.read").GetAsync("/api/v1/pharmacy/warehouses");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: co quyen warehouse.read thi lay duoc danh sach don mua
    [ApiFact]
    public async Task CoQuyen_LayDanhSachDonMua_KhongBiChan()
    {
        var res = await _fx.ClientWith("warehouse.read").GetAsync("/api/v1/pharmacy/purchase-orders");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: co quyen stock.read thi lay duoc danh sach ton kho
    [ApiFact]
    public async Task CoQuyen_LayDanhSachTonKho_KhongBiChan()
    {
        var res = await _fx.ClientWith("stock.read").GetAsync("/api/v1/pharmacy/stocks");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: co quyen stock.read thi lay duoc lich su xuat nhap
    [ApiFact]
    public async Task CoQuyen_LayLichSuXuatNhap_KhongBiChan()
    {
        var res = await _fx.ClientWith("stock.read").GetAsync("/api/v1/pharmacy/movements");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: co quyen stock.read thi lay duoc danh sach lo thuoc (stub)
    [ApiFact]
    public async Task CoQuyen_LayDanhSachLoThuoc_Tra200()
    {
        var res = await _fx.ClientWith("stock.read").GetAsync("/api/v1/pharmacy/lots");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-WAREHOUSE-01: co quyen stock.read thi lay duoc canh bao ton thap
    [ApiFact]
    public async Task CoQuyen_LayCanhBaoTonThap_KhongBiChan()
    {
        var res = await _fx.ClientWith("stock.read").GetAsync("/api/v1/pharmacy/alerts/low-stock");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: co quyen stock.read thi lay duoc canh bao sap het han
    [ApiFact]
    public async Task CoQuyen_LayCanhBaoSapHetHan_KhongBiChan()
    {
        var res = await _fx.ClientWith("stock.read").GetAsync("/api/v1/pharmacy/alerts/near-expiry");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-WAREHOUSE-01: token het han khi lay danh sach kho phai 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachKho_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/pharmacy/warehouses");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
