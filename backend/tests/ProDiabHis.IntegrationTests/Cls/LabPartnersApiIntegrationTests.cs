using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-LABPARTNER-01 — Bao mat va phan quyen cho API doi tac xet nghiem.</summary>
[Collection("Api")]
public class LabPartnersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public LabPartnersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-LABPARTNER-01: danh sach doi tac XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-partners");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: tao doi tac XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-partners", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: xem chi tiet doi tac XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ChiTietDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/lab-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: cap nhat doi tac XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task CapNhatDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/lab-partners/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: xoa doi tac XN khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XoaDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/lab-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: kiem tra ket noi doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task KiemTraKetNoiDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/test-connection", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: cap nhat thong tin xac thuc doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task CapNhatThongTinXacThuc_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PutAsJsonAsync($"/api/v1/lab-partners/{Id}/credentials", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: xoay khoa API doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XoayKhoaApiDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/credentials/rotate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: danh sach chi phi doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachChiPhiDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/lab-partners/{Id}/costs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: tao chi phi doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoChiPhiDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/lab-partner-costs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: cap nhat chi phi doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task CapNhatChiPhiDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/lab-partner-costs/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: danh sach ky doi soat khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachKyDoiSoat_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/lab-partners/{Id}/reconciliations");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: tao ky doi soat khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task TaoKyDoiSoat_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/reconciliations", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: doi trang thai ky doi soat khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DoiTrangThaiKyDoiSoat_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PutAsJsonAsync($"/api/v1/lab-partner-reconciliations/{Id}/status", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABPARTNER-01: token het han khong xem duoc danh sach doi tac
    [ApiFact]
    public async Task DanhSachDoiTac_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/lab-partners");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-LABPARTNER-01: thieu quyen lab_partner.read khong xem duoc danh sach doi tac
    [ApiFact]
    public async Task DanhSachDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-partners");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.write khong tao duoc doi tac
    [ApiFact]
    public async Task TaoDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-partners", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.read khong xem duoc chi tiet doi tac
    [ApiFact]
    public async Task ChiTietDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/lab-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.write khong cap nhat duoc doi tac
    [ApiFact]
    public async Task CapNhatDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/lab-partners/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.admin khong xoa duoc doi tac
    [ApiFact]
    public async Task XoaDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/lab-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.write khong kiem tra duoc ket noi
    [ApiFact]
    public async Task KiemTraKetNoiDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/test-connection", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.admin khong sua duoc thong tin xac thuc
    [ApiFact]
    public async Task CapNhatThongTinXacThuc_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PutAsJsonAsync($"/api/v1/lab-partners/{Id}/credentials", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.admin khong xoay duoc khoa API
    [ApiFact]
    public async Task XoayKhoaApiDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/credentials/rotate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_read khong xem duoc chi phi doi tac
    [ApiFact]
    public async Task DanhSachChiPhiDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/lab-partners/{Id}/costs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_write khong tao duoc chi phi
    [ApiFact]
    public async Task TaoChiPhiDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/lab-partner-costs", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_write khong cap nhat duoc chi phi
    [ApiFact]
    public async Task CapNhatChiPhiDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/lab-partner-costs/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_read khong xem duoc ky doi soat
    [ApiFact]
    public async Task DanhSachKyDoiSoat_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/lab-partners/{Id}/reconciliations");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_write khong tao duoc ky doi soat
    [ApiFact]
    public async Task TaoKyDoiSoat_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-partners/{Id}/reconciliations", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABPARTNER-01: thieu quyen lab_partner.finance_write khong doi duoc trang thai ky doi soat
    [ApiFact]
    public async Task DoiTrangThaiKyDoiSoat_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PutAsJsonAsync($"/api/v1/lab-partner-reconciliations/{Id}/status", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-LABPARTNER-01: co quyen lab_partner.read thi xem duoc danh sach doi tac
    [ApiFact]
    public async Task DanhSachDoiTac_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_partner.read").GetAsync("/api/v1/lab-partners");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABPARTNER-01: co quyen lab_partner.finance_read thi xem duoc chi phi doi tac
    [ApiFact]
    public async Task DanhSachChiPhiDoiTac_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_partner.finance_read").GetAsync($"/api/v1/lab-partners/{Id}/costs");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABPARTNER-01: co quyen lab_partner.finance_read thi xem duoc ky doi soat
    [ApiFact]
    public async Task DanhSachKyDoiSoat_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_partner.finance_read")
            .GetAsync($"/api/v1/lab-partners/{Id}/reconciliations");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
