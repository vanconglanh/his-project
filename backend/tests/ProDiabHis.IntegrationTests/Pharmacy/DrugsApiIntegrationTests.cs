using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-DRUG-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API danh muc thuoc.</summary>
[Collection("Api")]
public class DrugsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public DrugsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string DrugId = "DRUG-TEST-001";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-DRUG-01: GET danh sach thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: POST import thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_ImportThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsync("/api/v1/drugs/import", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: GET tim kiem thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TimKiemThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/drugs/search?q=para");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: GET danh sach nhom thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayDanhSachNhomThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/drugs/categories");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: POST tao nhom thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoNhomThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/drugs/categories", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: POST dong bo Cuc QLD khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_DongBoCucQld_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/drugs/sync-cuc-qld", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: GET chi tiet thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XemChiTietThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/drugs/{DrugId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: PUT cap nhat thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/drugs/{DrugId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: DELETE xoa thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/drugs/{DrugId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: POST tao thuoc moi khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoThuocMoi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/drugs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: GET thuoc tuong duong khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayThuocTuongDuong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/drugs/{DrugId}/equivalents");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DRUG-01: GET tuong tac thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayTuongTacThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/drugs/{DrugId}/interactions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-DRUG-01: thieu quyen drug.read khi lay danh sach thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.import khi import thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_ImportThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsync("/api/v1/drugs/import", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.read khi tim kiem thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_TimKiemThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/drugs/search?q=para");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.read khi lay danh sach nhom thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayDanhSachNhomThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/drugs/categories");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.write khi tao nhom thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoNhomThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/drugs/categories", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.sync khi dong bo Cuc QLD phai 403
    [ApiFact]
    public async Task ThieuQuyen_DongBoCucQld_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/drugs/sync-cuc-qld", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.read khi xem chi tiet thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/drugs/{DrugId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.write khi cap nhat thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/drugs/{DrugId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.write khi xoa thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/drugs/{DrugId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.write khi tao thuoc moi phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoThuocMoi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/drugs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen drug.read khi lay thuoc tuong duong phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayThuocTuongDuong_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/drugs/{DrugId}/equivalents");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DRUG-01: thieu quyen ddi.check khi lay tuong tac thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayTuongTacThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/drugs/{DrugId}/interactions");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-DRUG-01: co quyen drug.read thi lay duoc danh sach thuoc
    [ApiFact]
    public async Task CoQuyen_LayDanhSachThuoc_KhongBiChan()
    {
        var res = await _fx.ClientWith("drug.read").GetAsync("/api/v1/drugs");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DRUG-01: co quyen drug.read thi lay duoc danh sach nhom thuoc
    [ApiFact]
    public async Task CoQuyen_LayDanhSachNhomThuoc_KhongBiChan()
    {
        var res = await _fx.ClientWith("drug.read").GetAsync("/api/v1/drugs/categories");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DRUG-01: co quyen drug.read thi tim kiem thuoc khong bi chan
    [ApiFact]
    public async Task CoQuyen_TimKiemThuoc_KhongBiChan()
    {
        var res = await _fx.ClientWith("drug.read").GetAsync("/api/v1/drugs/search?q=para");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DRUG-01: token het han khi lay danh sach thuoc phai 401
    [ApiFact]
    public async Task TokenHetHan_LayDanhSachThuoc_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
