using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Cls;

/// <summary>ITC-LABINT-01 — Bao mat va phan quyen cho API tich hop XN voi doi tac (outbound/inbound/webhook).</summary>
[Collection("Api")]
public class LabIntegrationApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public LabIntegrationApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Id = Guid.NewGuid().ToString();

    // ── Loai 1: chua dang nhap phai 401 ─────────────────────────────

    // ITC-LABINT-01: gui chi dinh sang doi tac khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task GuiChiDinhSangDoiTac_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-integration/outbound/send/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: danh sach ban tin gui di khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachBanTinGuiDi_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-integration/outbound");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: gui lai ban tin gui di khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task GuiLaiBanTinGuiDi_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-integration/outbound/{Id}/retry", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: danh sach ban tin nhan ve khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task DanhSachBanTinNhanVe_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-integration/inbound");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: xu ly lai ban tin nhan ve khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XuLyLaiBanTinNhanVe_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync($"/api/v1/lab-integration/inbound/{Id}/reprocess", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: xem payload goc ban tin nhan ve khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task XemPayloadGocBanTinNhanVe_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/lab-integration/inbound/{Id}/raw");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: thong ke tich hop khi chua dang nhap phai bi tu choi
    [ApiFact]
    public async Task ThongKeTichHop_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/lab-integration/stats");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-LABINT-01: token het han khong xem duoc danh sach ban tin gui di
    [ApiFact]
    public async Task DanhSachBanTinGuiDi_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired())
            .GetAsync("/api/v1/lab-integration/outbound");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Webhook cong khai (AllowAnonymous) — khong ap dung loai 1/2 ──

    // ITC-LABINT-01: webhook doi tac thieu header X-Partner-Api-Key phai bi tu choi 401
    [ApiFact]
    public async Task WebhookDoiTac_ThieuApiKey_Tra401()
    {
        var res = await _fx.AnonymousClient()
            .PostAsJsonAsync("/api/public/v1/lab-results/webhook/PARTNER_TEST", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await res.Content.ReadAsStringAsync()).Should().Contain("LAB_WEBHOOK_INVALID_SIGNATURE");
    }

    // ── Loai 2: thieu quyen phai 403 PERMISSION_DENIED ──────────────

    // ITC-LABINT-01: thieu quyen lab_integration.send khong gui duoc chi dinh sang doi tac
    [ApiFact]
    public async Task GuiChiDinhSangDoiTac_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-integration/outbound/send/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.send khong xem duoc ban tin gui di
    [ApiFact]
    public async Task DanhSachBanTinGuiDi_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-integration/outbound");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.retry khong gui lai duoc ban tin
    [ApiFact]
    public async Task GuiLaiBanTinGuiDi_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-integration/outbound/{Id}/retry", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.send khong xem duoc ban tin nhan ve
    [ApiFact]
    public async Task DanhSachBanTinNhanVe_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-integration/inbound");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.retry khong xu ly lai duoc ban tin nhan ve
    [ApiFact]
    public async Task XuLyLaiBanTinNhanVe_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission()
            .PostAsJsonAsync($"/api/v1/lab-integration/inbound/{Id}/reprocess", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.send khong xem duoc payload goc
    [ApiFact]
    public async Task XemPayloadGocBanTinNhanVe_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/lab-integration/inbound/{Id}/raw");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-LABINT-01: thieu quyen lab_integration.send khong xem duoc thong ke tich hop
    [ApiFact]
    public async Task ThongKeTichHop_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/lab-integration/stats");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ── Loai 3: dung quyen thi tiep can duoc ────────────────────────

    // ITC-LABINT-01: co quyen lab_integration.send thi xem duoc ban tin gui di
    [ApiFact]
    public async Task DanhSachBanTinGuiDi_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_integration.send").GetAsync("/api/v1/lab-integration/outbound");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABINT-01: co quyen lab_integration.send thi xem duoc ban tin nhan ve
    [ApiFact]
    public async Task DanhSachBanTinNhanVe_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_integration.send").GetAsync("/api/v1/lab-integration/inbound");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-LABINT-01: co quyen lab_integration.send thi xem duoc thong ke tich hop
    [ApiFact]
    public async Task ThongKeTichHop_DungQuyen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("lab_integration.send").GetAsync("/api/v1/lab-integration/stats?days=7");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
