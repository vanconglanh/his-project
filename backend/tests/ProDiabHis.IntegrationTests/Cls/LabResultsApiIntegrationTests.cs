using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-LABRESULT-01 — Bao mat va phan quyen cho API ket qua xet nghiem.</summary>
[Collection("Api")]
public class LabResultsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public LabResultsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-LABRESULT-01: danh sach ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-results");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: danh sach chi dinh cho ket qua khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachChiDinhChoKetQua_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-results/pending-items");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: tao ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-results", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: sua ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task SuaKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/lab-results/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: duyet ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DuyetKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/lab-results/{Id}/verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: bo duyet ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task BoDuyetKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/lab-results/{Id}/unverify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: OCR doc file ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task OcrDocKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-results/ocr-extract", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: xac nhan ket qua OCR khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XacNhanKetQuaOcr_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-results/ocr-confirm", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: nhap khau ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task NhapKhauKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-results/import", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: danh sach ket qua bat thuong khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachKetQuaBatThuong_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-results/abnormal");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: bieu do dien bien ket qua khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task BieuDoDienBienKetQua_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-results/history-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: xuat PDF ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XuatPdfKetQuaXetNghiem_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/lab-results/{Id}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: duyet hang loat ket qua XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DuyetHangLoatKetQua_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-results/batch-verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABRESULT-01: token het han khong xem duoc danh sach ket qua XN
    [ApiFact]
    public async Task DanhSachKetQuaXetNghiem_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/lab-results");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-LABRESULT-01: thieu quyen lab_result.read khong xem duoc danh sach ket qua XN
    [ApiFact]
    public async Task DanhSachKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-results");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.write khong xem duoc chi dinh cho ket qua
    [ApiFact]
    public async Task DanhSachChiDinhChoKetQua_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-results/pending-items");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.write khong tao duoc ket qua XN
    [ApiFact]
    public async Task TaoKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-results", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.write khong sua duoc ket qua XN
    [ApiFact]
    public async Task SuaKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/lab-results/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.verify khong duyet duoc ket qua XN
    [ApiFact]
    public async Task DuyetKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/lab-results/{Id}/verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.verify khong bo duyet duoc ket qua XN
    [ApiFact]
    public async Task BoDuyetKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/lab-results/{Id}/unverify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.write khong dung duoc OCR doc ket qua
    [ApiFact]
    public async Task OcrDocKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-results/ocr-extract", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.write khong xac nhan duoc ket qua OCR
    [ApiFact]
    public async Task XacNhanKetQuaOcr_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-results/ocr-confirm", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.import khong nhap khau duoc ket qua XN
    [ApiFact]
    public async Task NhapKhauKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-results/import", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.read khong xem duoc ket qua bat thuong
    [ApiFact]
    public async Task DanhSachKetQuaBatThuong_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-results/abnormal");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.read khong xem duoc bieu do dien bien
    [ApiFact]
    public async Task BieuDoDienBienKetQua_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-results/history-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.read khong xuat duoc PDF ket qua XN
    [ApiFact]
    public async Task XuatPdfKetQuaXetNghiem_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/lab-results/{Id}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABRESULT-01: thieu quyen lab_result.verify khong duyet hang loat duoc
    [ApiFact]
    public async Task DuyetHangLoatKetQua_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-results/batch-verify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-LABRESULT-01: co quyen lab_result.read thi xem duoc danh sach ket qua XN
    [ApiFact]
    public async Task DanhSachKetQuaXetNghiem_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_result.read").GetAsync("/api/v1/lab-results");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABRESULT-01: co quyen lab_result.write thi xem duoc chi dinh cho ket qua
    [ApiFact]
    public async Task DanhSachChiDinhChoKetQua_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_result.write").GetAsync("/api/v1/lab-results/pending-items?limit=5");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABRESULT-01: co quyen lab_result.read thi xem duoc ket qua bat thuong
    [ApiFact]
    public async Task DanhSachKetQuaBatThuong_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_result.read").GetAsync("/api/v1/lab-results/abnormal");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
