using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-TELEADMIN-01 — Kiem tra bao mat va phan quyen quan tri Telehealth.</summary>
[Collection("Api")]
public class TelehealthAdminOpsIntegrationTests
{
    private const string Rid = "44444444-4444-4444-4444-444444444444";
    private readonly ApiTestFixture _fx;

    public TelehealthAdminOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-TELEADMIN-01: chua dang nhap xem mapping dich vu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachMapping_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/telehealth/service-mappings");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap tao mapping dich vu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TaoMapping_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/telehealth/service-mappings", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap cap nhat mapping dich vu phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatMapping_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/telehealth/service-mappings/{Rid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap xem ICD10 duoc phep phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DanhSachIcd10ChoPhep_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/telehealth/allowed-icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap them ICD10 duoc phep phai 401
    [ApiFact]
    public async Task ChuaDangNhap_ThemIcd10ChoPhep_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/telehealth/allowed-icd10", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap cap nhat ICD10 duoc phep phai 401
    [ApiFact]
    public async Task ChuaDangNhap_CapNhatIcd10ChoPhep_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/telehealth/allowed-icd10/{Rid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: chua dang nhap xoa ICD10 duoc phep phai 401
    [ApiFact]
    public async Task ChuaDangNhap_XoaIcd10ChoPhep_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/telehealth/allowed-icd10/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-TELEADMIN-01: thieu quyen xem mapping dich vu phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachMapping_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/telehealth/service-mappings");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: thieu quyen tao mapping dich vu phai 403
    [ApiFact]
    public async Task ThieuQuyen_TaoMapping_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/telehealth/service-mappings", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: thieu quyen cap nhat mapping dich vu phai 403
    [ApiFact]
    public async Task ThieuQuyen_CapNhatMapping_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/telehealth/service-mappings/{Rid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: thieu quyen xem ICD10 duoc phep phai 403
    [ApiFact]
    public async Task ThieuQuyen_DanhSachIcd10ChoPhep_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/telehealth/allowed-icd10");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: thieu quyen them ICD10 duoc phep phai 403
    [ApiFact]
    public async Task ThieuQuyen_ThemIcd10ChoPhep_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/telehealth/allowed-icd10", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: thieu quyen xoa ICD10 duoc phep phai 403
    [ApiFact]
    public async Task ThieuQuyen_XoaIcd10ChoPhep_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/telehealth/allowed-icd10/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-TELEADMIN-01: dung quyen xem mapping dich vu khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachMapping_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("telehealth.admin_mapping").GetAsync("/api/v1/telehealth/service-mappings");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-TELEADMIN-01: dung quyen xem ICD10 duoc phep khong loi he thong
    [ApiFact]
    public async Task DungQuyen_DanhSachIcd10ChoPhep_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("telehealth.icd10_read").GetAsync("/api/v1/telehealth/allowed-icd10");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
