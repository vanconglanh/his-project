using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-APPT-01 — Kiem tra bao mat va phan quyen module Lich hen.</summary>
[Collection("Api")]
public class AppointmentsOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public AppointmentsOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-APPT-01: chua dang nhap xem danh sach lich hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachLichHen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/appointments");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap xem chi tiet lich hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietLichHen_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/appointments/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap in phieu hen PDF phai 401
    [ApiFact]
    public async Task ChuaDangNhap_PhieuHenPdf_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/appointments/1/slip-pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap tao lich hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoLichHen_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/appointments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap cap nhat lich hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatLichHen_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/appointments/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap xem danh sach bac si de dat hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TuyChonBacSi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/appointments/options/doctors");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: chua dang nhap xem danh sach benh nhan de dat hen phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TuyChonBenhNhan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/appointments/options/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-APPT-01: thieu quyen xem danh sach lich hen phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachLichHen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/appointments");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: thieu quyen xem chi tiet lich hen phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietLichHen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/appointments/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: thieu quyen tao lich hen phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoLichHen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/appointments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: thieu quyen cap nhat lich hen phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatLichHen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/appointments/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: thieu quyen xem tuy chon bac si phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuyChonBacSi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/appointments/options/doctors");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: thieu quyen xem tuy chon benh nhan phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuyChonBenhNhan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/appointments/options/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-APPT-01: dung quyen xem danh sach lich hen khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachLichHen_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/appointments");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-APPT-01: dung quyen xem tuy chon bac si khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TuyChonBacSi_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/appointments/options/doctors");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-APPT-01: dung quyen xem tuy chon benh nhan khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TuyChonBenhNhan_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/appointments/options/patients");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
