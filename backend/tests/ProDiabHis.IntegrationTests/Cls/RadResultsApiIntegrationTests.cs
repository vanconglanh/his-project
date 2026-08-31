using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-RADRESULT-01 — Bao mat va phan quyen cho API ket qua chan doan hinh anh.</summary>
[Collection("Api")]
public class RadResultsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public RadResultsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-RADRESULT-01: danh sach ket qua CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachKetQuaChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/rad-results");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: tao ket qua CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoKetQuaChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/rad-results", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: sua ket qua CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task SuaKetQuaChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/rad-results/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: duyet ket qua CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DuyetKetQuaChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/rad-results/{Id}/verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: tai len anh DICOM khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaiLenAnhDicom_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/rad-results/{Id}/dicom-upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: xuat PDF ket qua CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XuatPdfKetQuaChanDoanHinhAnh_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/rad-results/{Id}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: OCR doc phieu KQ CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task OcrDocPhieuKetQuaCdha_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/rad-results/ocr-extract", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: xac nhan ket qua OCR CDHA khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XacNhanKetQuaOcrCdha_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/rad-results/ocr-confirm", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RADRESULT-01: token het han khong xem duoc danh sach ket qua CDHA
    [ApiFact]
    public async Task DanhSachKetQuaChanDoanHinhAnh_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/rad-results");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-RADRESULT-01: thieu quyen rad_result.read khong xem duoc danh sach ket qua CDHA
    [ApiFact]
    public async Task DanhSachKetQuaChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/rad-results");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.write khong tao duoc ket qua CDHA
    [ApiFact]
    public async Task TaoKetQuaChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/rad-results", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.write khong sua duoc ket qua CDHA
    [ApiFact]
    public async Task SuaKetQuaChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/rad-results/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.verify khong duyet duoc ket qua CDHA
    [ApiFact]
    public async Task DuyetKetQuaChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/rad-results/{Id}/verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.write khong tai len duoc anh DICOM
    [ApiFact]
    public async Task TaiLenAnhDicom_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/rad-results/{Id}/dicom-upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.read khong xuat duoc PDF ket qua CDHA
    [ApiFact]
    public async Task XuatPdfKetQuaChanDoanHinhAnh_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/rad-results/{Id}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.write khong dung duoc OCR doc phieu CDHA
    [ApiFact]
    public async Task OcrDocPhieuKetQuaCdha_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/rad-results/ocr-extract", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RADRESULT-01: thieu quyen rad_result.write khong xac nhan duoc ket qua OCR CDHA
    [ApiFact]
    public async Task XacNhanKetQuaOcrCdha_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/rad-results/ocr-confirm", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-RADRESULT-01: co quyen rad_result.read thi xem duoc danh sach ket qua CDHA
    [ApiFact]
    public async Task DanhSachKetQuaChanDoanHinhAnh_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("rad_result.read").GetAsync("/api/v1/rad-results");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
