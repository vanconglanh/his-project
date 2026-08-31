using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-ENCADMIN-01 — Bao mat va phan quyen cho EncryptionAdminController (/api/v1/admin/encryption).</summary>
[Collection("Api")]
public class EncryptionAdminApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public EncryptionAdminApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-ENCADMIN-01: An danh goi POST /admin/encryption/pii-backfill phai 401
    [ApiFact]
    public async Task AnDanh_BackfillPii_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/admin/encryption/pii-backfill", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCADMIN-01: An danh goi POST /admin/encryption/rotate-key phai 401
    [ApiFact]
    public async Task AnDanh_RotateKey_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/admin/encryption/rotate-key", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCADMIN-01: An danh goi GET /admin/encryption/keys phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachKey_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/admin/encryption/keys");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ENCADMIN-01: Thieu quyen encryption.rotate khi POST pii-backfill phai 403
    [ApiFact]
    public async Task ThieuQuyen_BackfillPii_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/admin/encryption/pii-backfill", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCADMIN-01: Thieu quyen encryption.rotate khi POST rotate-key phai 403
    [ApiFact]
    public async Task ThieuQuyen_RotateKey_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/admin/encryption/rotate-key", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCADMIN-01: Thieu quyen encryption.rotate khi GET keys phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachKey_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/admin/encryption/keys");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ENCADMIN-01: Dung quyen encryption.rotate thi GET keys khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachKey_KhongBiChan()
    {
        var res = await _fx.ClientWith("encryption.rotate").GetAsync("/api/v1/admin/encryption/keys");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
