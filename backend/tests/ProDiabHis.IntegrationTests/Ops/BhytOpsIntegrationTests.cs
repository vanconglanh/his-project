using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-BHYTEXP-01, ITC-BHYTREC-01 — Kiem tra bao mat va phan quyen module Xuat va doi soat BHYT.</summary>
[Collection("Api")]
public class BhytOpsIntegrationTests
{
    private const string Rid = "22222222-2222-2222-2222-222222222222";
    private readonly ApiTestFixture _fx;

    public BhytOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ---------- ITC-BHYTEXP-01: loai 1 ----------

    // ITC-BHYTEXP-01: chua dang nhap tao ho so xuat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoHoSoXuat_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap xem danh sach ho so xuat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachHoSoXuat_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/exports");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap xem chi tiet ho so xuat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietHoSoXuat_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/exports/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap xoa ho so xuat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaHoSoXuat_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/bhyt/exports/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap sinh XML phai 401
    [ApiFact]
    public async Task ChuaDangNhap_SinhXml_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports/1/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap sinh lai XML phai 401
    [ApiFact]
    public async Task ChuaDangNhap_SinhLaiXml_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports/1/regenerate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap kiem tra XML phai 401
    [ApiFact]
    public async Task ChuaDangNhap_KiemTraXml_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports/1/validate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap ky so ho so phai 401
    [ApiFact]
    public async Task ChuaDangNhap_KySoHoSo_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports/1/sign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap gui ho so phai 401
    [ApiFact]
    public async Task ChuaDangNhap_GuiHoSo_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/exports/1/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap tai XML theo bang phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XmlTheoBang_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/exports/1/xml/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap tai toan bo XML phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XmlToanBo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/exports/1/xml/all");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap xem dong du lieu theo bang phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DongDuLieuTheoBang_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/exports/1/items/table/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTEXP-01: chua dang nhap xem chi tiet 1 dong du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietDongDuLieu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/bhyt/exports/1/items/table/1/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- ITC-BHYTEXP-01: loai 2 ----------

    // ITC-BHYTEXP-01: thieu quyen xem danh sach ho so xuat phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachHoSoXuat_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/exports");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen xem chi tiet ho so xuat phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietHoSoXuat_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/exports/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen tao ho so xuat phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoHoSoXuat_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen xoa ho so xuat phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaHoSoXuat_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/bhyt/exports/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen sinh XML phai 403
    [ApiFact]
    public async Task ThieuQuyen_SinhXml_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports/1/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen sinh lai XML phai 403
    [ApiFact]
    public async Task ThieuQuyen_SinhLaiXml_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports/1/regenerate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen kiem tra XML phai 403
    [ApiFact]
    public async Task ThieuQuyen_KiemTraXml_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports/1/validate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen ky so phai 403
    [ApiFact]
    public async Task ThieuQuyen_KySoHoSo_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports/1/sign", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen gui ho so phai 403
    [ApiFact]
    public async Task ThieuQuyen_GuiHoSo_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/exports/1/submit", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen tai toan bo XML phai 403
    [ApiFact]
    public async Task ThieuQuyen_XmlToanBo_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/exports/1/xml/all");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: thieu quyen xem dong du lieu theo bang phai 403
    [ApiFact]
    public async Task ThieuQuyen_DongDuLieuTheoBang_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/exports/1/items/table/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTEXP-01: dung quyen xem danh sach ho so xuat khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachHoSoXuat_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("bhyt.read").GetAsync("/api/v1/bhyt/exports");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ---------- ITC-BHYTREC-01 ----------

    // ITC-BHYTREC-01: chua dang nhap nhap ket qua giam dinh phai 401
    [ApiFact]
    public async Task ChuaDangNhap_NhapKetQuaGiamDinh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/bhyt/reconcile/import", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTREC-01: chua dang nhap xem doi soat theo ho so phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoiSoatTheoHoSo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/reconcile/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTREC-01: chua dang nhap khieu nai dong doi soat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_KhieuNaiDongDoiSoat_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/bhyt/reconcile/{Rid}/dispute", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTREC-01: chua dang nhap chap nhan dong doi soat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChapNhanDongDoiSoat_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/bhyt/reconcile/{Rid}/accept", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTREC-01: chua dang nhap xem tong hop doi soat phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TongHopDoiSoat_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/bhyt/reconcile/1/summary");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BHYTREC-01: thieu quyen nhap ket qua giam dinh phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhapKetQuaGiamDinh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/bhyt/reconcile/import", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTREC-01: thieu quyen xem doi soat theo ho so phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoiSoatTheoHoSo_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/reconcile/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTREC-01: thieu quyen khieu nai dong doi soat phai 403
    [ApiFact]
    public async Task ThieuQuyen_KhieuNaiDongDoiSoat_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/bhyt/reconcile/{Rid}/dispute", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTREC-01: thieu quyen chap nhan dong doi soat phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChapNhanDongDoiSoat_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/bhyt/reconcile/{Rid}/accept", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BHYTREC-01: thieu quyen xem tong hop doi soat phai 403
    [ApiFact]
    public async Task ThieuQuyen_TongHopDoiSoat_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/bhyt/reconcile/1/summary");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }
}
