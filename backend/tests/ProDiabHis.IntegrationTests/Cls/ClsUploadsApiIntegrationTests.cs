using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-CLSUPLOAD-01 — Bao mat va phan quyen cho API tai len ho so CLS.</summary>
[Collection("Api")]
public class ClsUploadsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ClsUploadsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Pat = Guid.NewGuid().ToString();
    private static readonly string Enc = Guid.NewGuid().ToString();
    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-CLSUPLOAD-01: danh sach file CLS cua benh nhan khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachFileClsTheoBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pat}/cls-uploads");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSUPLOAD-01: tai file CLS len khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaiLenFileCls_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsync($"/api/v1/patients/{Pat}/cls-uploads", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSUPLOAD-01: xem chi tiet file CLS khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ChiTietFileCls_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pat}/cls-uploads/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSUPLOAD-01: xoa file CLS khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XoaFileCls_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/patients/{Pat}/cls-uploads/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSUPLOAD-01: danh sach file CLS theo luot kham khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachFileClsTheoLuotKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/encounters/{Enc}/cls-uploads");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CLSUPLOAD-01: token het han khong xem duoc danh sach file CLS
    [ApiFact]
    public async Task DanhSachFileClsTheoBenhNhan_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired())
            .GetAsync($"/api/v1/patients/{Pat}/cls-uploads");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-CLSUPLOAD-01: thieu quyen cls_upload.read khong xem duoc danh sach file CLS
    [ApiFact]
    public async Task DanhSachFileClsTheoBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pat}/cls-uploads");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSUPLOAD-01: thieu quyen cls_upload.create khong tai len duoc file CLS
    [ApiFact]
    public async Task TaiLenFileCls_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsync($"/api/v1/patients/{Pat}/cls-uploads", TestContent.File());
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSUPLOAD-01: thieu quyen cls_upload.read khong xem duoc chi tiet file CLS
    [ApiFact]
    public async Task ChiTietFileCls_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pat}/cls-uploads/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSUPLOAD-01: thieu quyen cls_upload.delete khong xoa duoc file CLS
    [ApiFact]
    public async Task XoaFileCls_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/patients/{Pat}/cls-uploads/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CLSUPLOAD-01: thieu quyen cls_upload.read khong xem duoc file CLS theo luot kham
    [ApiFact]
    public async Task DanhSachFileClsTheoLuotKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/encounters/{Enc}/cls-uploads");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-CLSUPLOAD-01: co quyen cls_upload.read thi truy cap duoc danh sach file CLS
    [ApiFact]
    public async Task DanhSachFileClsTheoBenhNhan_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cls_upload.read").GetAsync($"/api/v1/patients/{Pat}/cls-uploads");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-CLSUPLOAD-01: co quyen cls_upload.read thi truy cap duoc file CLS theo luot kham
    [ApiFact]
    public async Task DanhSachFileClsTheoLuotKham_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cls_upload.read").GetAsync($"/api/v1/encounters/{Enc}/cls-uploads");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
