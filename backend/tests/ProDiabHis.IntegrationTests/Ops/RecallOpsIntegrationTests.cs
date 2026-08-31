using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-RECALL-01 — Kiem tra bao mat va phan quyen module Nhac tai kham.</summary>
[Collection("Api")]
public class RecallOpsIntegrationTests
{
    private const string Rid = "55555555-5555-5555-5555-555555555555";
    private readonly ApiTestFixture _fx;

    public RecallOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-RECALL-01: chua dang nhap xem danh sach nhac tai kham phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachNhacTaiKham_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/recall");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RECALL-01: chua dang nhap gui thong bao nhac tai kham phai 401
    [ApiFact]
    public async Task ChuaDangNhap_GuiThongBaoNhac_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/recall/{Rid}/notify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RECALL-01: chua dang nhap xem muc tieu lo trinh cham soc phai 401
    [ApiFact]
    public async Task ChuaDangNhap_MucTieuLoTrinhChamSoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/care-pathway/targets");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-RECALL-01: thieu quyen xem danh sach nhac tai kham phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachNhacTaiKham_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/recall");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RECALL-01: thieu quyen gui thong bao nhac tai kham phai 403
    [ApiFact]
    public async Task ThieuQuyen_GuiThongBaoNhac_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/recall/{Rid}/notify", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RECALL-01: thieu quyen xem muc tieu lo trinh cham soc phai 403
    [ApiFact]
    public async Task ThieuQuyen_MucTieuLoTrinhChamSoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/care-pathway/targets");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-RECALL-01: dung quyen xem danh sach nhac tai kham khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachNhacTaiKham_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("recall.read").GetAsync("/api/v1/recall");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-RECALL-01: dung quyen xem muc tieu lo trinh cham soc khong loi he thong
    [ApiFact]
    public async Task DungQuyen_MucTieuLoTrinhChamSoc_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("diabetes.assess").GetAsync("/api/v1/care-pathway/targets");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
