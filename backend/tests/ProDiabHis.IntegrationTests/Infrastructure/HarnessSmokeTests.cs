using System.Net;
using FluentAssertions;
using Xunit;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// Smoke test cua chinh bo khung test: chung minh API that boot duoc tren MySQL container
/// va pipeline xac thuc hoat dong dung. Neu cac test nay do thi moi IT khac deu vo nghia.
/// </summary>
[Collection("Api")]
public class HarnessSmokeTests
{
    private readonly ApiTestFixture _fx;

    public HarnessSmokeTests(ApiTestFixture fx) => _fx = fx;

    // ITC-HARNESS-01: API boot + ket noi DB that
    [ApiFact]
    public async Task Health_Anonymous_TraVe200_VaDbOk()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/health");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
        body.Should().Contain("\"db\":\"OK\"");
    }

    // ITC-HARNESS-02: endpoint co [Authorize] tu choi request khong token
    [ApiFact]
    public async Task EndpointCoAuthorize_KhongToken_TraVe401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-HARNESS-03: token hop le nhung thieu permission -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TokenThieuQuyen_TraVe403_PermissionDenied()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/patients");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-HARNESS-04: token het han -> 401 (ClockSkew = 0)
    [ApiFact]
    public async Task TokenHetHan_TraVe401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-HARNESS-05: token dung quyen -> vao duoc controller that
    [ApiFact]
    public async Task TokenDungQuyen_VaoDuocController()
    {
        var res = await _fx.ClientWith("patient.read").GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-HARNESS-06: super admin bypass moi permission check
    [ApiFact]
    public async Task SuperAdmin_BypassPermission()
    {
        var res = await _fx.AdminClient().GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
