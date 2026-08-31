using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-DOCSCHED-01 — Kiem tra bao mat va phan quyen module Lich lam viec bac si.</summary>
[Collection("Api")]
public class DoctorSchedulesOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public DoctorSchedulesOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-DOCSCHED-01: chua dang nhap xem lich lam viec phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachLich_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/doctor-schedules");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap tao lich lam viec phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoLich_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/doctor-schedules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap cap nhat lich lam viec phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatLich_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/doctor-schedules/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap xoa lich lam viec phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaLich_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/doctor-schedules/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap xem khung gio nghi phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachBlock_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/doctor-schedules/blocks");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap tao khung gio nghi phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoBlock_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/doctor-schedules/blocks", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: chua dang nhap xoa khung gio nghi phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaBlock_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/doctor-schedules/blocks/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCSCHED-01: thieu quyen xem lich lam viec phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachLich_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/doctor-schedules");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen tao lich lam viec phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoLich_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/doctor-schedules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen cap nhat lich lam viec phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatLich_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/doctor-schedules/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen xoa lich lam viec phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaLich_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/doctor-schedules/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen xem khung gio nghi phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachBlock_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/doctor-schedules/blocks");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen tao khung gio nghi phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoBlock_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/doctor-schedules/blocks", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: thieu quyen xoa khung gio nghi phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaBlock_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/doctor-schedules/blocks/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCSCHED-01: dung quyen xem lich lam viec khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachLich_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/doctor-schedules");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DOCSCHED-01: dung quyen xem khung gio nghi khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachBlock_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/doctor-schedules/blocks");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
