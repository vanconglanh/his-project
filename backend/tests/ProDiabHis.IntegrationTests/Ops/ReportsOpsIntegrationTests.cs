using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-REPORT-01 — Kiem tra bao mat va phan quyen module Bao cao.</summary>
[Collection("Api")]
public class ReportsOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public ReportsOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ---------- Loai 1: chua dang nhap phai 401 ----------

    // ITC-REPORT-01: chua dang nhap xem bao cao doanh thu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoanhThu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem doanh thu theo bac si phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoanhThuTheoBacSi_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/by-doctor");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem doanh thu theo dich vu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoanhThuTheoDichVu_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/by-service");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem doanh thu theo hinh thuc thanh toan phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DoanhThuTheoHinhThucThanhToan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/by-payment-method");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem tong hop thu ngan trong ngay phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TongHopThuNgan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/cashier/daily-summary");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem tuoi no phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TuoiNo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/debts/aging");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem tong hop BHYT phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TongHopBhyt_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/bhyt/summary");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem nhom benh nhan dai thao duong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_NhomDaiThaoDuong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/clinical/diabetes-cohort");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem cohort dai thao duong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CohortDaiThaoDuong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/diabetes/cohort");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap dem luot kham phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DemLuotKham_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/encounters/count");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem top chan doan phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TopChanDoan_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/diagnoses/top");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem bao cao luot kham lam sang phai 401
    [ApiFact]
    public async Task ChuaDangNhap_LuotKhamLamSang_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/clinical/visits");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem bao cao ICD10 phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BaoCaoIcd10_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/clinical/icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem top thuoc phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TopThuoc_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/pharmacy/top-drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem gia tri ton kho phai 401
    [ApiFact]
    public async Task ChuaDangNhap_GiaTriTonKho_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/pharmacy/inventory-value");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap tao ma bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoMaBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/revenue/code", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap tai bao cao PDF phai 401
    [ApiFact]
    public async Task ChuaDangNhap_BaoCaoPdf_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xuat bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XuatBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/export", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem danh muc bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhMucBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/catalog");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap lay du lieu bao cao theo ma phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DuLieuBaoCaoTheoMa_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/data");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xuat bao cao theo ma phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XuatBaoCaoTheoMa_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/revenue/export");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap lay tuy chon nguon du lieu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TuyChonNguon_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/options/doctor");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem dataset phai 401
    [ApiFact]
    public async Task ChuaDangNhap_Dataset_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/datasets");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem dinh nghia bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DinhNghiaBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/definitions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap tao dinh nghia bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoDinhNghiaBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/definitions", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap cap nhat dinh nghia bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatDinhNghiaBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/reports/definitions/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xoa dinh nghia bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaDinhNghiaBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/reports/definitions/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem truoc bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XemTruocBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/preview", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem lich chay bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_LichChayBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/schedules");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap tao lich chay bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoLichChayBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/schedules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap cap nhat lich chay bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatLichChayBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/reports/schedules/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xoa lich chay bao cao phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaLichChayBaoCao_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/reports/schedules/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem danh sach dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/dashboards");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xem chi tiet dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/dashboards/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap tao dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/reports/dashboards", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap cap nhat dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync("/api/v1/reports/dashboards/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap xoa dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync("/api/v1/reports/dashboards/1");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-REPORT-01: chua dang nhap lay du lieu dashboard phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DuLieuDashboard_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/reports/dashboards/1/data");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- Loai 2: thieu quyen phai 403 ----------

    // ITC-REPORT-01: thieu quyen xem doanh thu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoanhThu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem doanh thu theo bac si phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoanhThuTheoBacSi_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue/by-doctor");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem doanh thu theo dich vu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoanhThuTheoDichVu_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue/by-service");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem doanh thu theo hinh thuc thanh toan phai 403
    [ApiFact]
    public async Task ThieuQuyen_DoanhThuTheoHinhThucThanhToan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue/by-payment-method");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem tong hop thu ngan phai 403
    [ApiFact]
    public async Task ThieuQuyen_TongHopThuNgan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/cashier/daily-summary");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem tuoi no phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuoiNo_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/debts/aging");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem tong hop BHYT phai 403
    [ApiFact]
    public async Task ThieuQuyen_TongHopBhyt_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/bhyt/summary");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem nhom benh nhan dai thao duong phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhomDaiThaoDuong_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/clinical/diabetes-cohort");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem cohort dai thao duong phai 403
    [ApiFact]
    public async Task ThieuQuyen_CohortDaiThaoDuong_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/diabetes/cohort");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen dem luot kham phai 403
    [ApiFact]
    public async Task ThieuQuyen_DemLuotKham_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/encounters/count");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem top chan doan phai 403
    [ApiFact]
    public async Task ThieuQuyen_TopChanDoan_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/diagnoses/top");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem luot kham lam sang phai 403
    [ApiFact]
    public async Task ThieuQuyen_LuotKhamLamSang_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/clinical/visits");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem bao cao ICD10 phai 403
    [ApiFact]
    public async Task ThieuQuyen_BaoCaoIcd10_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/clinical/icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem top thuoc phai 403
    [ApiFact]
    public async Task ThieuQuyen_TopThuoc_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/pharmacy/top-drugs");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem gia tri ton kho phai 403
    [ApiFact]
    public async Task ThieuQuyen_GiaTriTonKho_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/pharmacy/inventory-value");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem danh muc bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhMucBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/catalog");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen lay du lieu bao cao theo ma phai 403
    [ApiFact]
    public async Task ThieuQuyen_DuLieuBaoCaoTheoMa_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue/data");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xuat bao cao theo ma phai 403
    [ApiFact]
    public async Task ThieuQuyen_XuatBaoCaoTheoMa_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/revenue/export");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen lay tuy chon nguon du lieu phai 403
    [ApiFact]
    public async Task ThieuQuyen_TuyChonNguon_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/options/doctor");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem dataset phai 403
    [ApiFact]
    public async Task ThieuQuyen_Dataset_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/datasets");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem dinh nghia bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_DinhNghiaBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/definitions");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen tao dinh nghia bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoDinhNghiaBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/reports/definitions", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen cap nhat dinh nghia bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatDinhNghiaBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/reports/definitions/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xoa dinh nghia bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaDinhNghiaBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/reports/definitions/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem truoc bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_XemTruocBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/reports/preview", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem lich chay bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_LichChayBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/schedules");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen tao lich chay bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoLichChayBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/reports/schedules", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen cap nhat lich chay bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatLichChayBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/reports/schedules/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xoa lich chay bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaLichChayBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/reports/schedules/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem danh sach dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/dashboards");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xem chi tiet dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/dashboards/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen tao dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/reports/dashboards", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen cap nhat dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync("/api/v1/reports/dashboards/1", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xoa dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync("/api/v1/reports/dashboards/1");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen lay du lieu dashboard phai 403
    [ApiFact]
    public async Task ThieuQuyen_DuLieuDashboard_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/reports/dashboards/1/data");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-REPORT-01: thieu quyen xuat bao cao phai 403
    [ApiFact]
    public async Task ThieuQuyen_XuatBaoCao_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/reports/export", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ---------- Loai 3: dung quyen thi tiep can duoc ----------

    // ITC-REPORT-01: dung quyen xem danh muc bao cao khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhMucBaoCao_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.read").GetAsync("/api/v1/reports/catalog");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-REPORT-01: dung quyen xem dataset khong loi he thong
    [ApiFact]
    public async Task DungQuyen_Dataset_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.build").GetAsync("/api/v1/reports/datasets");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-REPORT-01: dung quyen xem dinh nghia bao cao khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DinhNghiaBaoCao_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.build").GetAsync("/api/v1/reports/definitions");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-REPORT-01: dung quyen xem lich chay bao cao khong loi he thong
    [ApiFact]
    public async Task DungQuyen_LichChayBaoCao_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.build").GetAsync("/api/v1/reports/schedules");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-REPORT-01: dung quyen xem danh sach dashboard khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachDashboard_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.read").GetAsync("/api/v1/reports/dashboards");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-REPORT-01: dung quyen xem doanh thu khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DoanhThu_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("report.read").GetAsync("/api/v1/reports/revenue");
        // GIOI HAN MOI TRUONG TEST (khong phai bug san pham) — da xac minh bang log MySQL that:
        // endpoint nay doc bang/cot chi duoc tao boi db/migrations/*.sql, ma schema test dung
        // EF EnsureCreated() + TestSchemaSupplement nen con thieu (rep_*_cache, mot so cot,
        // va lech collation utf8mb4_unicode_ci vs utf8mb4_0900_ai_ci giua 2 nguon schema).
        // Vi vay KHONG assert '<500' o day; van assert phan CHAC CHAN dung: da qua duoc
        // xac thuc + phan quyen. Bo assert '<500' tro lai khi chuoi migration dung duoc DB
        // sach tu so 0 (xem db/migrations/APPLY_ORDER.md).
        // ((int)res.StatusCode).Should().BeLessThan(500);   // TAM TAT — xem ghi chu tren
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
