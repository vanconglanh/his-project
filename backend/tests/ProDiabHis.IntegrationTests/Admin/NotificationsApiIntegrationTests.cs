using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-NOTIFY-01 — Bao mat va phan quyen cho NotificationsController (/api/v1/notifications).
/// Endpoint /web-push/vapid-public-key la AllowAnonymous nen khong co case 401.</summary>
[Collection("Api")]
public class NotificationsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public NotificationsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "11111111-1111-1111-1111-111111111111";

    // ITC-NOTIFY-01: An danh goi GET /notifications/inbox phai 401
    [ApiFact]
    public async Task AnDanh_XemHopThu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notifications/inbox");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi GET /notifications/unread-count phai 401
    [ApiFact]
    public async Task AnDanh_DemChuaDoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notifications/unread-count");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi POST /notifications/{id}/mark-read phai 401
    [ApiFact]
    public async Task AnDanh_DanhDauDaDoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/notifications/{Id}/mark-read", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi POST /notifications/mark-all-read phai 401
    [ApiFact]
    public async Task AnDanh_DanhDauTatCaDaDoc_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/notifications/mark-all-read", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi DELETE /notifications/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaThongBao_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/notifications/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi POST /notifications/web-push/subscribe phai 401
    [ApiFact]
    public async Task AnDanh_DangKyWebPush_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/notifications/web-push/subscribe", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi DELETE /notifications/web-push/unsubscribe phai 401
    [ApiFact]
    public async Task AnDanh_HuyDangKyWebPush_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/notifications/web-push/unsubscribe?endpoint=abc");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi GET /notifications/vapid/status phai 401
    [ApiFact]
    public async Task AnDanh_XemTrangThaiVapid_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notifications/vapid/status");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi POST /notifications/vapid/generate phai 401
    [ApiFact]
    public async Task AnDanh_SinhKhoaVapid_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/notifications/vapid/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi GET /notifications/logs phai 401
    [ApiFact]
    public async Task AnDanh_XemNhatKyThongBao_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notifications/logs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi POST /notifications/test-send phai 401
    [ApiFact]
    public async Task AnDanh_GuiThongBaoThu_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/notifications/test-send", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi GET /notifications/preferences phai 401
    [ApiFact]
    public async Task AnDanh_XemTuyChon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notifications/preferences");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: An danh goi PUT /notifications/preferences phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatTuyChon_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/notifications/preferences", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi GET inbox phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemHopThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notifications/inbox");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi GET unread-count phai 403
    [ApiFact]
    public async Task ThieuQuyen_DemChuaDoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notifications/unread-count");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi POST mark-read phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhDauDaDoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/notifications/{Id}/mark-read", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi POST mark-all-read phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhDauTatCaDaDoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/notifications/mark-all-read", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi DELETE /notifications/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaThongBao_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/notifications/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi POST web-push/subscribe phai 403
    [ApiFact]
    public async Task ThieuQuyen_DangKyWebPush_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/notifications/web-push/subscribe", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi DELETE web-push/unsubscribe phai 403
    [ApiFact]
    public async Task ThieuQuyen_HuyDangKyWebPush_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/notifications/web-push/unsubscribe?endpoint=abc");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.config khi GET vapid/status phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTrangThaiVapid_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notifications/vapid/status");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.config khi POST vapid/generate phai 403
    [ApiFact]
    public async Task ThieuQuyen_SinhKhoaVapid_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/notifications/vapid/generate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi GET /notifications/logs phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemNhatKyThongBao_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notifications/logs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.send khi POST test-send phai 403
    [ApiFact]
    public async Task ThieuQuyen_GuiThongBaoThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/notifications/test-send", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi GET preferences phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTuyChon_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notifications/preferences");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFY-01: Thieu quyen notification.read khi PUT preferences phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatTuyChon_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/notifications/preferences", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }
}
