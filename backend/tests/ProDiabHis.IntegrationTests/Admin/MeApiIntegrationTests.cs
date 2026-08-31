using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-ME-01 — Bao mat cho MeController (/api/v1/me).
/// Cac endpoint chi co [Authorize] (khong RequirePermission) nen chi kiem tra 401.</summary>
[Collection("Api")]
public class MeApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public MeApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-ME-01: An danh goi GET /me/branch-context phai 401
    [ApiFact]
    public async Task AnDanh_XemBranchContext_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/me/branch-context");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ME-01: An danh goi POST /me/switch-branch phai 401
    [ApiFact]
    public async Task AnDanh_DoiChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/me/switch-branch", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ME-01: Token het han goi GET /me/branch-context phai 401
    [ApiFact]
    public async Task TokenHetHan_XemBranchContext_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/me/branch-context");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
