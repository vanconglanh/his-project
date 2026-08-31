using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-NOTIFYCH-01 — Bao mat va phan quyen cho NotificationChannelsController
/// (/api/v1/notification-channels).</summary>
[Collection("Api")]
public class NotificationChannelsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public NotificationChannelsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "SMS";

    // ITC-NOTIFYCH-01: An danh goi GET /notification-channels phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/notification-channels");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: An danh goi GET /notification-channels/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/notification-channels/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: An danh goi POST /notification-channels phai 401
    [ApiFact]
    public async Task AnDanh_TaoKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/notification-channels", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: An danh goi PUT /notification-channels/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/notification-channels/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: An danh goi DELETE /notification-channels/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/notification-channels/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: An danh goi POST /notification-channels/{id}/test phai 401
    [ApiFact]
    public async Task AnDanh_TestKetNoiKenh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/notification-channels/{Id}/test", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.read khi GET danh sach phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/notification-channels");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.read khi GET chi tiet phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/notification-channels/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.write khi POST phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/notification-channels", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.write khi PUT phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/notification-channels/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.write khi DELETE phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/notification-channels/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Thieu quyen notification_channel.write khi POST /test phai 403
    [ApiFact]
    public async Task ThieuQuyen_TestKetNoiKenh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/notification-channels/{Id}/test", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-NOTIFYCH-01: Dung quyen notification_channel.read thi GET danh sach khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachKenh_KhongBiChan()
    {
        var res = await _fx.ClientWith("notification_channel.read").GetAsync("/api/v1/notification-channels");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
