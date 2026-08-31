using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-ROLE-01 — Bao mat va phan quyen cho RolesController (/api/v1/roles).</summary>
[Collection("Api")]
public class RolesApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public RolesApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-ROLE-01: An danh goi GET /roles phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachRole_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/roles");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROLE-01: An danh goi POST /roles phai 401
    [ApiFact]
    public async Task AnDanh_TaoRole_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/roles", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROLE-01: An danh goi GET /roles/{code} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietRole_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROLE-01: An danh goi PUT /roles/{code} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatRole_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/roles/BAC_SI", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROLE-01: An danh goi DELETE /roles/{code} phai 401
    [ApiFact]
    public async Task AnDanh_XoaRole_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROLE-01: Thieu quyen role.read khi GET /roles phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/roles");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROLE-01: Thieu quyen role.read khi GET /roles/{code} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROLE-01: Thieu quyen role.write khi POST /roles phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/roles", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROLE-01: Thieu quyen role.write khi PUT /roles/{code} phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/roles/BAC_SI", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROLE-01: Thieu quyen role.write khi DELETE /roles/{code} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROLE-01: Dung quyen role.read thi GET /roles khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachRole_KhongBiChan()
    {
        var res = await _fx.ClientWith("role.read").GetAsync("/api/v1/roles");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
