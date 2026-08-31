using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-APIPARTNER-01 — Bao mat va phan quyen cho ApiPartnersController (/api/v1/api-partners).</summary>
[Collection("Api")]
public class ApiPartnersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ApiPartnersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string Id = "11111111-1111-1111-1111-111111111111";

    // ITC-APIPARTNER-01: An danh goi GET /api-partners phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachDoiTac_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/api-partners");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi GET /api-partners/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietDoiTac_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/api-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi POST /api-partners phai 401
    [ApiFact]
    public async Task AnDanh_TaoDoiTac_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/api-partners", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi PUT /api-partners/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatDoiTac_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/api-partners/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi DELETE /api-partners/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaDoiTac_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/api-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi POST /api-partners/{id}/regenerate-key phai 401
    [ApiFact]
    public async Task AnDanh_TaoLaiApiKey_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/api-partners/{Id}/regenerate-key", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi POST /api-partners/{id}/test-call phai 401
    [ApiFact]
    public async Task AnDanh_TestCall_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/api-partners/{Id}/test-call", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi GET /api-partners/{id}/usage-stats phai 401
    [ApiFact]
    public async Task AnDanh_XemThongKeSuDung_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/api-partners/{Id}/usage-stats");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: An danh goi GET /api-partners/{id}/request-logs phai 401
    [ApiFact]
    public async Task AnDanh_XemNhatKyRequest_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/api-partners/{Id}/request-logs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.read khi GET /api-partners phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachDoiTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/api-partners");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.read khi GET /api-partners/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietDoiTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/api-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.write khi POST /api-partners phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoDoiTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/api-partners", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.write khi PUT /api-partners/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatDoiTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/api-partners/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.write khi DELETE /api-partners/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaDoiTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/api-partners/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.admin khi POST regenerate-key phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoLaiApiKey_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/api-partners/{Id}/regenerate-key", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.admin khi POST test-call phai 403
    [ApiFact]
    public async Task ThieuQuyen_TestCall_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/api-partners/{Id}/test-call", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.read khi GET usage-stats phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemThongKeSuDung_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/api-partners/{Id}/usage-stats");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Thieu quyen api_partner.read khi GET request-logs phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemNhatKyRequest_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/api-partners/{Id}/request-logs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APIPARTNER-01: Dung quyen api_partner.read thi GET /api-partners khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachDoiTac_KhongBiChan()
    {
        var res = await _fx.ClientWith("api_partner.read").GetAsync("/api/v1/api-partners");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
