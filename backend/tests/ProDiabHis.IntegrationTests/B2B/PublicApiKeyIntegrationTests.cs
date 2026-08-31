using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.B2B;

/// <summary>
/// ITC-PUBLICAPI-01 — Bao mat cong B2B (PublicApiController, /api/public/v1) xac thuc bang
/// header X-Api-Key (ApiKeyAuthFilter). Day la nhom endpoint nguy co lam dung cao neu thieu auth
/// (dang ky benh nhan, dat lich thay khach) nen phai chac chan: THIEU key hoac key SAI deu bi chan.
///
/// Trong tam (di qua DUNG pipeline that: ApiKeyAuthFilter -> Controller):
///  - Goi KHONG co X-Api-Key -> 401 (chan truoc khi cham DB).
///  - Goi voi key SAI (khong ton tai trong diab_his_api_partners) -> 401.
/// </summary>
[Collection("Api")]
public class PublicApiKeyIntegrationTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fx;

    public PublicApiKeyIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // Dam bao cau SELECT cua ApiKeyStoreImpl parse duoc (them cot doc `ip_whitelist` neu thieu),
    // nho vay case "key sai" di den duoc buoc tra null -> 401 thay vi 500 do thieu cot.
    public async Task InitializeAsync() => await ApiKeyTestSeed.EnsureReadColumnsAsync(_fx.ConnectionString);
    public Task DisposeAsync() => Task.CompletedTask;

    private const string SampleAppointmentId = "22222222-2222-2222-2222-222222222222";

    private HttpClient WithApiKey(string rawKey)
    {
        var client = _fx.Client;
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", rawKey);
        return client;
    }

    // ---------------- Thieu X-Api-Key -> 401 (chan som, chua cham DB) ----------------

    // ITC-PUBLICAPI-01: dang ky benh nhan khong co API key -> 401.
    [ApiFact]
    public async Task ThieuApiKey_DangKyBenhNhan_Tra401()
    {
        var res = await _fx.Client.PostAsJsonAsync("/api/public/v1/patients/register", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await res.Content.ReadAsStringAsync()).Should().Contain("API_KEY_INVALID");
    }

    // ITC-PUBLICAPI-01: dat lich thay khach khong co API key -> 401.
    [ApiFact]
    public async Task ThieuApiKey_DatLich_Tra401()
    {
        var res = await _fx.Client.PostAsJsonAsync("/api/public/v1/appointments/book", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PUBLICAPI-01: xem chi tiet lich hen khong co API key -> 401.
    [ApiFact]
    public async Task ThieuApiKey_XemLichHen_Tra401()
    {
        var res = await _fx.Client.GetAsync($"/api/public/v1/appointments/{SampleAppointmentId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PUBLICAPI-01: tra cuu danh muc dich vu khong co API key -> 401.
    [ApiFact]
    public async Task ThieuApiKey_XemDanhMucDichVu_Tra401()
    {
        var res = await _fx.Client.GetAsync("/api/public/v1/catalog/services");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PUBLICAPI-01: header rong ("") cung coi nhu thieu -> 401.
    [ApiFact]
    public async Task ApiKeyRong_DangKyBenhNhan_Tra401()
    {
        var res = await WithApiKey("").PostAsJsonAsync("/api/public/v1/patients/register", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------- Key SAI (khong ton tai trong DB) -> 401 ----------------

    // ITC-PUBLICAPI-01: dang ky benh nhan voi key khong ton tai -> 401.
    [ApiFact]
    public async Task ApiKeySai_DangKyBenhNhan_Tra401()
    {
        var res = await WithApiKey("khong-ton-tai-" + Guid.NewGuid())
            .PostAsJsonAsync("/api/public/v1/patients/register", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await res.Content.ReadAsStringAsync()).Should().Contain("API_KEY_INVALID");
    }

    // ITC-PUBLICAPI-01: xem lich hen voi key khong ton tai -> 401 (khong lo du lieu).
    [ApiFact]
    public async Task ApiKeySai_XemLichHen_Tra401()
    {
        var res = await WithApiKey("sai-key-" + Guid.NewGuid())
            .GetAsync($"/api/public/v1/appointments/{SampleAppointmentId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
