using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-DOCTOR-01, ITC-ROOM-01 — Kiem tra bao mat va phan quyen danh muc Bac si va Phong.</summary>
[Collection("Api")]
public class DoctorsRoomsOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public DoctorsRoomsOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-DOCTOR-01: chua dang nhap tra cuu bac si phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TraCuuBacSi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/doctors/lookup");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DOCTOR-01: thieu quyen tra cuu bac si phai 403
    [ApiFact]
    public async Task ThieuQuyen_TraCuuBacSi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/doctors/lookup");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DOCTOR-01: dung quyen tra cuu bac si khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TraCuuBacSi_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("appointment.read").GetAsync("/api/v1/doctors/lookup");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-ROOM-01: chua dang nhap xem danh sach phong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachPhong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/rooms");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROOM-01: chua dang nhap xem chi tiet phong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietPhong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/rooms/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROOM-01: chua dang nhap tao phong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoPhong_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/rooms", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROOM-01: chua dang nhap cap nhat phong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatPhong_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/rooms/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROOM-01: chua dang nhap xoa phong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaPhong_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/rooms/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ROOM-01: thieu quyen xem danh sach phong phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachPhong_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/rooms");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROOM-01: thieu quyen xem chi tiet phong phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietPhong_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/rooms/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROOM-01: thieu quyen tao phong phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoPhong_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/rooms", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROOM-01: thieu quyen cap nhat phong phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatPhong_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/rooms/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROOM-01: thieu quyen xoa phong phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaPhong_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/rooms/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ROOM-01: dung quyen xem danh sach phong khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachPhong_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("room.read").GetAsync("/api/v1/rooms");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
