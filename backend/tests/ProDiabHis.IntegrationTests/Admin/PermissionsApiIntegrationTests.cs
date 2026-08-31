using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-PERM-01 — Bao mat va phan quyen cho PermissionsController (/api/v1/permissions).</summary>
[Collection("Api")]
public class PermissionsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PermissionsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-PERM-01: An danh goi GET /permissions phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhMucQuyen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/permissions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PERM-01: Thieu quyen role.read khi GET /permissions phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhMucQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/permissions");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PERM-01: Dung quyen role.read thi GET /permissions khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhMucQuyen_KhongBiChan()
    {
        var res = await _fx.ClientWith("role.read").GetAsync("/api/v1/permissions");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
