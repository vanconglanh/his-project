using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-TENANT-01 — Bao mat cho TenantsController (/api/v1/tenants).
/// Cac endpoint SUPER_ADMIN chi kiem tra 401 vi thong diep loi khac PERMISSION_DENIED.</summary>
[Collection("Api")]
public class TenantsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public TenantsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-TENANT-01: An danh goi GET /tenants phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/tenants");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi POST /tenants phai 401
    [ApiFact]
    public async Task AnDanh_TaoTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/tenants", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi GET /tenants/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/tenants/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi PUT /tenants/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/tenants/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi DELETE /tenants/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/tenants/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi POST /tenants/{id}/suspend phai 401
    [ApiFact]
    public async Task AnDanh_TamNgungTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/tenants/1/suspend", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi POST /tenants/{id}/activate phai 401
    [ApiFact]
    public async Task AnDanh_KichHoatTenant_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/tenants/1/activate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi GET /tenants/current phai 401
    [ApiFact]
    public async Task AnDanh_XemTenantHienTai_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/tenants/current");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi GET /tenants/me phai 401
    [ApiFact]
    public async Task AnDanh_XemTenantCuaMinh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/tenants/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi GET /tenants/me/letterhead phai 401
    [ApiFact]
    public async Task AnDanh_XemLetterhead_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/tenants/me/letterhead");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: An danh goi PUT /tenants/me phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatTenantCuaMinh_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/tenants/me", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TENANT-01: Thieu quyen tenant.read khi GET /tenants/current phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTenantHienTai_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/tenants/current");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TENANT-01: Thieu quyen tenant.read khi GET /tenants/me phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTenantCuaMinh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/tenants/me");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TENANT-01: Thieu quyen tenant.read khi GET /tenants/me/letterhead phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemLetterhead_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/tenants/me/letterhead");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TENANT-01: Thieu quyen tenant.write khi PUT /tenants/me phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatTenantCuaMinh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/tenants/me", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }
}
