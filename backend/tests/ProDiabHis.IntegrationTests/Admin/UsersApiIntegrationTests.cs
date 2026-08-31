using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-USER-01 — Bao mat va phan quyen cho UsersController (/api/v1/users).</summary>
[Collection("Api")]
public class UsersApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public UsersApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private static readonly string Id = "11111111-1111-1111-1111-111111111111";

    // ITC-USER-01: An danh goi GET /users phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachUser_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/users");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/invite phai 401
    [ApiFact]
    public async Task AnDanh_MoiUser_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/users/invite", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi GET /users/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietUser_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/users/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi PUT /users/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatUser_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/users/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi DELETE /users/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaUser_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/users/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/{id}/roles phai 401
    [ApiFact]
    public async Task AnDanh_GanRole_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/users/{Id}/roles", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi DELETE /users/{id}/roles/{code} phai 401
    [ApiFact]
    public async Task AnDanh_ThuHoiRole_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/users/{Id}/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi GET /users/{id}/branches phai 401
    [ApiFact]
    public async Task AnDanh_XemChiNhanhCuaUser_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/users/{Id}/branches");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi PUT /users/{id}/branches phai 401
    [ApiFact]
    public async Task AnDanh_GanChiNhanhChoUser_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/users/{Id}/branches", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/{id}/disable phai 401
    [ApiFact]
    public async Task AnDanh_KhoaUser_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/users/{Id}/disable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/{id}/enable phai 401
    [ApiFact]
    public async Task AnDanh_MoKhoaUser_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/users/{Id}/enable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi GET /users/me phai 401
    [ApiFact]
    public async Task AnDanh_XemProfileBanThan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/users/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi PUT /users/me phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatProfileBanThan_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/users/me", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/me/change-password phai 401
    [ApiFact]
    public async Task AnDanh_DoiMatKhau_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/users/me/change-password", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/me/2fa/setup phai 401
    [ApiFact]
    public async Task AnDanh_KhoiTao2FA_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/users/me/2fa/setup", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/me/2fa/enable phai 401
    [ApiFact]
    public async Task AnDanh_KichHoat2FA_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/users/me/2fa/enable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: An danh goi POST /users/me/2fa/disable phai 401
    [ApiFact]
    public async Task AnDanh_Tat2FA_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/users/me/2fa/disable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-USER-01: Thieu quyen user.read khi GET /users phai 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/users");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.read khi GET /users/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/users/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.invite khi POST /users/invite phai 403
    [ApiFact]
    public async Task ThieuQuyen_MoiUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/users/invite", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.write khi PUT /users/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/users/{Id}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.delete khi DELETE /users/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/users/{Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.assign_role khi POST /users/{id}/roles phai 403
    [ApiFact]
    public async Task ThieuQuyen_GanRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/users/{Id}/roles", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.assign_role khi DELETE /users/{id}/roles/{code} phai 403
    [ApiFact]
    public async Task ThieuQuyen_ThuHoiRole_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/users/{Id}/roles/BAC_SI");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.read khi GET /users/{id}/branches phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiNhanhCuaUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/users/{Id}/branches");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen branch.assign_user khi PUT /users/{id}/branches phai 403
    [ApiFact]
    public async Task ThieuQuyen_GanChiNhanhChoUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/users/{Id}/branches", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.write khi POST /users/{id}/disable phai 403
    [ApiFact]
    public async Task ThieuQuyen_KhoaUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/users/{Id}/disable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Thieu quyen user.write khi POST /users/{id}/enable phai 403
    [ApiFact]
    public async Task ThieuQuyen_MoKhoaUser_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/users/{Id}/enable", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-USER-01: Dung quyen user.read thi GET /users khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachUser_KhongBiChan()
    {
        var res = await _fx.ClientWith("user.read").GetAsync("/api/v1/users");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
