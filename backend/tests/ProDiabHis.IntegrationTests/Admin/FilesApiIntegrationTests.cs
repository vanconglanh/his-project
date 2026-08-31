using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Admin;

/// <summary>ITC-FILE-01 — Bao mat va phan quyen cho FilesController (/api/v1/files).</summary>
[Collection("Api")]
public class FilesApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public FilesApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const string FileId = "11111111-1111-1111-1111-111111111111";
    private const string AnnoId = "22222222-2222-2222-2222-222222222222";

    // ITC-FILE-01: An danh goi POST /files/upload phai 401
    [ApiFact]
    public async Task AnDanh_TaiTepLen_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/files/upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi GET /files/{id}/signed-url phai 401
    [ApiFact]
    public async Task AnDanh_LaySignedUrl_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/files/{FileId}/signed-url");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi DELETE /files/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaTep_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/files/{FileId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi GET /files/{fileId}/annotations phai 401
    [ApiFact]
    public async Task AnDanh_XemDanhSachAnnotation_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/files/{FileId}/annotations");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi POST /files/{fileId}/annotations phai 401
    [ApiFact]
    public async Task AnDanh_TaoAnnotation_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/files/{FileId}/annotations", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi PUT /files/{fileId}/annotations/{id} phai 401
    [ApiFact]
    public async Task AnDanh_CapNhatAnnotation_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/files/{FileId}/annotations/{AnnoId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: An danh goi DELETE /files/{fileId}/annotations/{id} phai 401
    [ApiFact]
    public async Task AnDanh_XoaAnnotation_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/files/{FileId}/annotations/{AnnoId}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FILE-01: Thieu quyen file.upload khi POST /files/upload phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaiTepLen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/files/upload", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FILE-01: Thieu quyen file.delete khi DELETE /files/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaTep_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/files/{FileId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FILE-01: Thieu quyen file_annotation.read khi GET annotations phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemDanhSachAnnotation_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/files/{FileId}/annotations");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FILE-01: Thieu quyen file_annotation.write khi POST annotations phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoAnnotation_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/files/{FileId}/annotations", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FILE-01: Thieu quyen file_annotation.write khi PUT annotations/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatAnnotation_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/files/{FileId}/annotations/{AnnoId}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FILE-01: Thieu quyen file_annotation.delete khi DELETE annotations/{id} phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaAnnotation_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/files/{FileId}/annotations/{AnnoId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }
}
