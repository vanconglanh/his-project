using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Finance;

/// <summary>ITC-CASHIER-01 — kiem tra bao mat, phan quyen va tiep can endpoint thu ngan (cashier).</summary>
[Collection("Api")]
public class CashierIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public CashierIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly Guid SampleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // ITC-CASHIER-01: chua dang nhap xem bao cao ca hom nay phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemBaoCaoHomNay_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cashier/closing/today");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap mo ca lam viec phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_MoCaLamViec_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/cashier/closing/open", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap dong ca lam viec phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_DongCaLamViec_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/cashier/closing/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap xem lich su chot ca phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemLichSuChotCa_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cashier/closing/history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap xuat PDF bien ban chot ca phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XuatPdfChotCa_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/cashier/closing/{SampleId}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap xem ca lam viec hien tai phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemCaHienTai_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cashier/shift");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap in bien lai phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_InBienLai_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/cashier/receipts/{SampleId}/print", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: chua dang nhap xem cong no benh nhan phai bi 401
    [ApiFact]
    public async Task ChuaDangNhap_XemCongNo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/cashier/debts");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: token het han xem bao cao ca hom nay phai bi 401
    [ApiFact]
    public async Task TokenHetHan_XemBaoCaoHomNay_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/cashier/closing/today");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CASHIER-01: thieu quyen cashier.report khi xem bao cao hom nay phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemBaoCaoHomNay_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cashier/closing/today");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.shift_open khi mo ca phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_MoCaLamViec_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/cashier/closing/open", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.shift_close khi dong ca phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_DongCaLamViec_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/cashier/closing/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.report khi xem lich su chot ca phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemLichSuChotCa_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cashier/closing/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.report khi xuat PDF chot ca phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XuatPdfChotCa_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/cashier/closing/{SampleId}/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.report khi xem ca hien tai phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemCaHienTai_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cashier/shift");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.print_receipt khi in bien lai phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_InBienLai_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/cashier/receipts/{SampleId}/print", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: thieu quyen cashier.debt_view khi xem cong no phai bi 403
    [ApiFact]
    public async Task ThieuQuyen_XemCongNo_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/cashier/debts");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-CASHIER-01: co quyen cashier.report thi truy cap duoc lich su chot ca
    [ApiFact]
    public async Task CoQuyen_XemLichSuChotCa_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cashier.report").GetAsync("/api/v1/cashier/closing/history");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-CASHIER-01: co quyen cashier.debt_view thi truy cap duoc danh sach cong no
    [ApiFact]
    public async Task CoQuyen_XemCongNo_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("cashier.debt_view").GetAsync("/api/v1/cashier/debts");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
