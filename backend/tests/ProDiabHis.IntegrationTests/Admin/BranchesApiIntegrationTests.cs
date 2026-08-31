using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-BRANCH-01 — Bao mat va phan quyen cho BranchesController (/api/v1/branches).</summary>
[Collection("Api")]
public class BranchesApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public BranchesApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string UserId = "11111111-1111-1111-1111-111111111111";

    // ITC-BRANCH-01: An danh goi GET /branches phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/branches");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi GET /branches/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XemChiTietChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/branches/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi POST /branches phai 401
    [ApiFact]
    public async Task AnDanh_TaoChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/branches", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi PUT /branches/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/branches/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi PATCH /branches/{id}/status phai 401
    [ApiFact]
    public async Task AnDanh_DoiTrangThaiChiNhanh_Tra401()
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/branches/1/status")
        {
            Content = JsonContent.Create(new { isActive = true })
        };
        var res = await _fx.AnonymousClient().SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi POST /branches/{id}/set-default phai 401
    [ApiFact]
    public async Task AnDanh_DatChiNhanhMacDinh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/branches/1/set-default", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi DELETE /branches/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/branches/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi GET /branches/{id}/users phai 401
    [ApiFact]
    public async Task AnDanh_XemNhanSuChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/branches/1/users");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi POST /branches/{id}/users phai 401
    [ApiFact]
    public async Task AnDanh_GanNhanSuVaoChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/branches/1/users", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi DELETE /branches/{id}/users/{userId} phai 401
    [ApiFact]
    public async Task AnDanh_GoNhanSuKhoiChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/branches/1/users/{UserId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi GET /branches/bhyt-compliance phai 401
    [ApiFact]
    public async Task AnDanh_XemTuanThuBhyt_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/branches/bhyt-compliance");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi POST /branches/{id}/clone phai 401
    [ApiFact]
    public async Task AnDanh_NhanBanChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/branches/1/clone", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi GET /branches/{id}/readiness phai 401
    [ApiFact]
    public async Task AnDanh_XemChecklistGoLive_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/branches/1/readiness");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: An danh goi POST /branches/{id}/activate phai 401
    [ApiFact]
    public async Task AnDanh_KichHoatChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/branches/1/activate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-BRANCH-01: Thieu quyen branch.read khi GET /branches phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/branches");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.read khi GET /branches/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChiTietChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/branches/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.create khi POST /branches phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/branches", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.update khi PUT /branches/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/branches/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.update khi POST /branches/{id}/set-default phai 403
    [ApiFact]
    public async Task ThieuQuyen_DatChiNhanhMacDinh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/branches/1/set-default", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.delete khi DELETE /branches/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/branches/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.read khi GET /branches/{id}/users phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemNhanSuChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/branches/1/users");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.assign_user khi POST /branches/{id}/users phai 403
    [ApiFact]
    public async Task ThieuQuyen_GanNhanSuVaoChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/branches/1/users", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.assign_user khi DELETE /branches/{id}/users/{userId} phai 403
    [ApiFact]
    public async Task ThieuQuyen_GoNhanSuKhoiChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/branches/1/users/{UserId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.read khi GET /branches/bhyt-compliance phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTuanThuBhyt_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/branches/bhyt-compliance");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.create khi POST /branches/{id}/clone phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhanBanChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/branches/1/clone", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.read khi GET /branches/{id}/readiness phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemChecklistGoLive_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/branches/1/readiness");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Thieu quyen branch.update khi POST /branches/{id}/activate phai 403
    [ApiFact]
    public async Task ThieuQuyen_KichHoatChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/branches/1/activate", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-BRANCH-01: Dung quyen branch.read thi GET /branches khong bi chan
    [ApiFact]
    public async Task DungQuyen_XemDanhSachChiNhanh_KhongBiChan()
    {
        var res = await _fx.ClientWith("branch.read").GetAsync("/api/v1/branches");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
