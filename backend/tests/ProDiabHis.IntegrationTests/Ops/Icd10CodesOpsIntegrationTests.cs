using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-ICD10-01, ITC-CODE-01 — Kiem tra bao mat va phan quyen danh muc ICD-10 va Ma he thong.</summary>
[Collection("Api")]
public class Icd10CodesOpsIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public Icd10CodesOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-ICD10-01: chua dang nhap xem danh sach ICD10 phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachIcd10_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ICD10-01: chua dang nhap tim kiem ICD10 phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TimKiemIcd10_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/icd10/search?keyword=E11");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ICD10-01: chua dang nhap xem nhom chuong ICD10 phai 401
    [ApiFact]
    public async Task ChuaDangNhap_NhomChuongIcd10_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/icd10/categories");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ICD10-01: chua dang nhap xem chi tiet ma ICD10 phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ChiTietMaIcd10_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/icd10/E11");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-ICD10-01: thieu quyen xem danh sach ICD10 phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachIcd10_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ICD10-01: thieu quyen tim kiem ICD10 phai 403
    [ApiFact]
    public async Task ThieuQuyen_TimKiemIcd10_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/icd10/search?keyword=E11");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ICD10-01: thieu quyen xem nhom chuong ICD10 phai 403
    [ApiFact]
    public async Task ThieuQuyen_NhomChuongIcd10_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/icd10/categories");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ICD10-01: thieu quyen xem chi tiet ma ICD10 phai 403
    [ApiFact]
    public async Task ThieuQuyen_ChiTietMaIcd10_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/icd10/E11");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-ICD10-01: dung quyen xem danh sach ICD10 khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachIcd10_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("icd10.read").GetAsync("/api/v1/icd10");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-ICD10-01: dung quyen xem nhom chuong ICD10 khong loi he thong
    [ApiFact]
    public async Task DungQuyen_NhomChuongIcd10_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("icd10.read").GetAsync("/api/v1/icd10/categories");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-CODE-01: chua dang nhap xem danh sach ma he thong phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachMaHeThong_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/codes");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CODE-01: chua dang nhap lay ma he thong theo lo phai 401
    [ApiFact]
    public async Task ChuaDangNhap_MaHeThongTheoLo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/codes/batch");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CODE-01: chua dang nhap lay ma he thong theo nhom phai 401
    [ApiFact]
    public async Task ChuaDangNhap_MaHeThongTheoNhom_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/codes/GENDER");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-CODE-01: token het han lay ma he thong phai 401
    [ApiFact]
    public async Task TokenHetHan_DanhSachMaHeThong_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/codes");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
