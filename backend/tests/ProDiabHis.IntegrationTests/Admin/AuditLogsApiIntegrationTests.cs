using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-AUDIT-01 — Bao mat va phan quyen cho AuditLogsController (/api/v1/audit-logs).</summary>
[Collection("Api")]
public class AuditLogsApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public AuditLogsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-AUDIT-01: An danh goi GET /audit-logs phai 401
    [ApiFact]
    public async Task AnDanh_XemNhatKyThaoTac_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/audit-logs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-AUDIT-01: An danh goi GET /audit-logs/export phai 401
    [ApiFact]
    public async Task AnDanh_XuatNhatKyCsv_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/audit-logs/export");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-AUDIT-01: Thieu quyen audit.review khi GET /audit-logs phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemNhatKyThaoTac_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/audit-logs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-AUDIT-01: Thieu quyen audit.export khi GET /audit-logs/export phai 403
    [ApiFact]
    public async Task ThieuQuyen_XuatNhatKyCsv_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/audit-logs/export");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-AUDIT-01: Dung quyen audit.review thi GET /audit-logs khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemNhatKyThaoTac_KhongBiChan()
    {
        var res = await _fx.ClientWith("audit.review").GetAsync("/api/v1/audit-logs");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
