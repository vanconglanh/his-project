using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-FFLAG-01 — Bao mat cho FeatureFlagsController (/api/v1/admin/feature-flags).
/// Toan bo endpoint dung RequireSuperAdmin nen chi kiem tra 401 khi an danh.</summary>
[Collection("Api")]
public class FeatureFlagsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public FeatureFlagsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-FFLAG-01: An danh goi GET /admin/feature-flags phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachFlag_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/admin/feature-flags");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FFLAG-01: An danh goi GET /admin/feature-flags/{key} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietFlag_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/admin/feature-flags/demo_flag");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FFLAG-01: An danh goi PUT /admin/feature-flags/{key} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatFlag_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/admin/feature-flags/demo_flag", new { enabled = true });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
