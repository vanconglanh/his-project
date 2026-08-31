using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-REFERRAL-01, ITC-CDSS-01, ITC-DOCUMENT-01, ITC-AI-01 — Kiem tra bao mat va phan quyen cac module ho tro lam sang.</summary>
[Collection("Api")]
public class ClinicalSupportOpsIntegrationTests
{
    private const string Rid = "77777777-7777-7777-7777-777777777777";
    private readonly ApiTestFixture _fx;

    public ClinicalSupportOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ---------- ITC-REFERRAL-01 ----------

    // ITC-REFERRAL-01: chua dang nhap tao phieu chuyen noi bo phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhieuChuyenNoiBo_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/internal-referrals", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REFERRAL-01: chua dang nhap xem phieu chuyen den phai 401
    [ApiFact]
    public async Task ChuaDangNhap_PhieuChuyenDen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/internal-referrals/incoming");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REFERRAL-01: thieu quyen tao phieu chuyen noi bo phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhieuChuyenNoiBo_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/internal-referrals", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REFERRAL-01: thieu quyen xem phieu chuyen den phai 403
    [ApiFact]
    public async Task ThieuQuyen_PhieuChuyenDen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/internal-referrals/incoming");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REFERRAL-01: dung quyen xem phieu chuyen den khong loi he thong
    [ApiFact]
    public async Task DungQuyen_PhieuChuyenDen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("internal_referral.read").GetAsync("/api/v1/internal-referrals/incoming");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ---------- ITC-CDSS-01 ----------

    // ITC-CDSS-01: chua dang nhap kiem tra canh bao CDSS phai 401
    [ApiFact]
    public async Task ChuaDangNhap_KiemTraCdss_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/cdss/check", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CDSS-01: chua dang nhap ghi nhan bo qua canh bao CDSS phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BoQuaCanhBaoCdss_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/cdss/override", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CDSS-01: chua dang nhap xem danh sach luat CDSS phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachLuatCdss_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cdss/rules");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CDSS-01: chua dang nhap tao luat CDSS phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoLuatCdss_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/cdss/rules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CDSS-01: thieu quyen kiem tra canh bao CDSS phai 403
    [ApiFact]
    public async Task ThieuQuyen_KiemTraCdss_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/cdss/check", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CDSS-01: thieu quyen bo qua canh bao CDSS phai 403
    [ApiFact]
    public async Task ThieuQuyen_BoQuaCanhBaoCdss_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/cdss/override", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CDSS-01: thieu quyen xem danh sach luat CDSS phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachLuatCdss_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cdss/rules");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CDSS-01: thieu quyen tao luat CDSS phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoLuatCdss_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/cdss/rules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CDSS-01: dung quyen xem danh sach luat CDSS khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachLuatCdss_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cdss.admin").GetAsync("/api/v1/cdss/rules");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ---------- ITC-DOCUMENT-01 ----------

    // ITC-DOCUMENT-01: chua dang nhap tai tai lieu thong minh phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaiTaiLieuThongMinh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/documents/smart-upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCUMENT-01: thieu quyen tai tai lieu thong minh phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaiTaiLieuThongMinh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/documents/smart-upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ---------- ITC-AI-01 ----------

    // ITC-AI-01: chua dang nhap goi y dieu tri bang AI phai 401
    [ApiFact]
    public async Task ChuaDangNhap_GoiYDieuTriAi_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Rid}/ai/treatment-suggestion", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-AI-01: thieu quyen goi y dieu tri bang AI phai 403
    [ApiFact]
    public async Task ThieuQuyen_GoiYDieuTriAi_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/patients/{Rid}/ai/treatment-suggestion", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-AI-01: token het han goi y dieu tri bang AI phai 401
    [ApiFact]
    public async Task TokenHetHan_GoiYDieuTriAi_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired())
            .PostAsJsonAsync($"/api/v1/patients/{Rid}/ai/treatment-suggestion", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
