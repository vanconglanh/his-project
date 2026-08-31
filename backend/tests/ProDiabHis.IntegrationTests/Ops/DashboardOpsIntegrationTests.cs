using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-DASHBOARD-01 — Kiem tra bao mat va phan quyen module Dashboard tong quan.</summary>
[Collection("Api")]
public class DashboardOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public DashboardOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-DASHBOARD-01: chua dang nhap xem tong quan phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TongQuan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/overview");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem bieu do doanh thu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BieuDoDoanhThu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/charts/revenue-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem bieu do luot kham phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BieuDoLuotKham_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/charts/encounters-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem top bac si phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TopBacSi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/charts/top-doctors");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem top thuoc phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TopThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/charts/top-drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem bieu do HbA1c phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BieuDoHba1c_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/charts/diabetes-hba1c");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem canh bao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CanhBao_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/alerts");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem xep hang chi nhanh phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XepHangChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/branch-ranking");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: chua dang nhap xem chi tiet chi nhanh phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietChiNhanh_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/dashboard/branch/1/detail");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-DASHBOARD-01: thieu quyen xem tong quan phai 403
    [ApiFact]
    public async Task ThieuQuyen_TongQuan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/overview");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem bieu do doanh thu phai 403
    [ApiFact]
    public async Task ThieuQuyen_BieuDoDoanhThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/charts/revenue-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem bieu do luot kham phai 403
    [ApiFact]
    public async Task ThieuQuyen_BieuDoLuotKham_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/charts/encounters-trend");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem top bac si phai 403
    [ApiFact]
    public async Task ThieuQuyen_TopBacSi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/charts/top-doctors");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem top thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_TopThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/charts/top-drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem bieu do HbA1c phai 403
    [ApiFact]
    public async Task ThieuQuyen_BieuDoHba1c_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/charts/diabetes-hba1c");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem canh bao phai 403
    [ApiFact]
    public async Task ThieuQuyen_CanhBao_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/alerts");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem xep hang chi nhanh phai 403
    [ApiFact]
    public async Task ThieuQuyen_XepHangChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/branch-ranking");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: thieu quyen xem chi tiet chi nhanh phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietChiNhanh_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/dashboard/branch/1/detail");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-DASHBOARD-01: dung quyen xem tong quan khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TongQuan_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("dashboard.read").GetAsync("/api/v1/dashboard/overview");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-DASHBOARD-01: dung quyen xem canh bao khong loi he thong
    [ApiFact]
    public async Task DungQuyen_CanhBao_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("dashboard.read").GetAsync("/api/v1/dashboard/alerts");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
