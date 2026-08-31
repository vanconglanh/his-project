using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>ITC-DISPENSE-01 — Kiem tra bao mat, phan quyen va kha nang tiep can API cap phat thuoc.</summary>
[Collection("Api")]
public class PharmacyDispensingApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PharmacyDispensingApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "DISP-TEST-001";

    // ── Loai 1: chua dang nhap phai 401 ──────────────────────────────────────

    // ITC-DISPENSE-01: GET hang doi cap phat khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayHangDoiCapPhat_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/dispense/queue");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DISPENSE-01: GET lich su cap phat khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayLichSuCapPhat_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/pharmacy/dispense/history");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DISPENSE-01: POST cap phat thuoc khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_CapPhatThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DISPENSE-01: POST tu choi cap phat khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TuChoiCapPhat_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}/reject", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DISPENSE-01: POST tra lai thuoc da cap phat khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_TraLaiThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}/return", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DISPENSE-01: GET phieu cap phat PDF khi chua dang nhap phai tra 401
    [ApiFact]
    public async Task ChuaDangNhap_LayPhieuCapPhatPdf_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/pharmacy/dispense/{Id}/receipt-pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ───────────────────────

    // ITC-DISPENSE-01: thieu quyen dispense.queue khi lay hang doi phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayHangDoiCapPhat_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/dispense/queue");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DISPENSE-01: thieu quyen dispense.queue khi lay lich su cap phat phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayLichSuCapPhat_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/pharmacy/dispense/history");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DISPENSE-01: thieu quyen dispense.perform khi cap phat thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapPhatThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DISPENSE-01: thieu quyen dispense.reject khi tu choi cap phat phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuChoiCapPhat_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}/reject", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DISPENSE-01: thieu quyen dispense.return khi tra lai thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_TraLaiThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/pharmacy/dispense/{Id}/return", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DISPENSE-01: thieu quyen dispense.queue khi lay phieu cap phat PDF phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayPhieuCapPhatPdf_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/pharmacy/dispense/{Id}/receipt-pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ─────────────────────────────────

    // ITC-DISPENSE-01: co quyen dispense.queue thi lay duoc hang doi cap phat
    [ApiFact]
    public async Task CoQuyen_LayHangDoiCapPhat_KhongBiChan()
    {
        var res = await _fx.ClientWith("dispense.queue").GetAsync("/api/v1/pharmacy/dispense/queue");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DISPENSE-01: co quyen dispense.queue thi lay duoc lich su cap phat
    [ApiFact]
    public async Task CoQuyen_LayLichSuCapPhat_KhongBiChan()
    {
        var res = await _fx.ClientWith("dispense.queue").GetAsync("/api/v1/pharmacy/dispense/history");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DISPENSE-01: token het han khi lay hang doi cap phat phai 401
    [ApiFact]
    public async Task TokenHetHan_LayHangDoiCapPhat_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/pharmacy/dispense/queue");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
