using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Clinical;

/// <summary>ITC-PATIENT-xx — bao mat + phan quyen cho PatientsController (/api/v1/patients).</summary>
[Collection("Api")]
public class PatientsApiIntegrationTests
{
    private const string Pid = "11111111-1111-1111-1111-111111111111";
    private const string Sid = "22222222-2222-2222-2222-222222222222";

    private readonly ApiTestFixture _fx;

    public PatientsApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-PATIENT-01: GET danh sach benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-02: GET tim kiem benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task TimKiemBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/patients/search?q=abc");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-03: GET check trung CCCD khi chua dang nhap -> 401
    [ApiFact]
    public async Task CheckTrungCccd_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/v1/patients/check-cccd-duplicate?id_number=001099001234");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-04: GET chi tiet benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task ChiTietBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-05: GET lo trinh ngoai he thong khi chua dang nhap -> 401
    [ApiFact]
    public async Task LoTrinhNgoai_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/external-pathway");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-06: POST tao benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task TaoBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/v1/patients", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-07: PUT cap nhat benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/patients/{Pid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-08: PUT ap dung truong CCCD khi chua dang nhap -> 401
    [ApiFact]
    public async Task ApDungTruongCccd_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/patients/{Pid}/apply-cccd-fields", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-09: DELETE benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaBenhNhan_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/patients/{Pid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-10: GET lich su kham cua benh nhan khi chua dang nhap -> 401
    [ApiFact]
    public async Task LichSuKham_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-11: POST upload anh dai dien khi chua dang nhap -> 401
    [ApiFact]
    public async Task UploadAnhDaiDien_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Pid}/avatar", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-12: GET danh sach di ung khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachDiUng_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/allergies");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-13: POST them di ung khi chua dang nhap -> 401
    [ApiFact]
    public async Task ThemDiUng_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Pid}/allergies", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-14: DELETE di ung khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaDiUng_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/patients/{Pid}/allergies/{Sid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-15: GET danh sach nguoi giam ho khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachNguoiGiamHo_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/guardians");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-16: GET danh sach the BHYT khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachTheBhyt_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/insurance");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-17: POST them the BHYT khi chua dang nhap -> 401
    [ApiFact]
    public async Task ThemTheBhyt_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Pid}/insurance", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-18: PUT cap nhat the BHYT khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatTheBhyt_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/patients/{Pid}/insurance/{Sid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-19: DELETE the BHYT khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaTheBhyt_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/patients/{Pid}/insurance/{Sid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-20: GET danh ba khan cap khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhBaKhanCap_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/emergency-contacts");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-21: POST them lien he khan cap khi chua dang nhap -> 401
    [ApiFact]
    public async Task ThemLienHeKhanCap_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Pid}/emergency-contacts", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-22: PUT cap nhat lien he khan cap khi chua dang nhap -> 401
    [ApiFact]
    public async Task CapNhatLienHeKhanCap_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/patients/{Pid}/emergency-contacts/{Sid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-23: DELETE lien he khan cap khi chua dang nhap -> 401
    [ApiFact]
    public async Task XoaLienHeKhanCap_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().DeleteAsync($"/api/v1/patients/{Pid}/emergency-contacts/{Sid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-24: GET danh sach dong y (consent) khi chua dang nhap -> 401
    [ApiFact]
    public async Task DanhSachDongY_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/consents");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-25: POST them ban dong y khi chua dang nhap -> 401
    [ApiFact]
    public async Task ThemBanDongY_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync($"/api/v1/patients/{Pid}/consents", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-26: PUT ghi chu tiep don khi chua dang nhap -> 401
    [ApiFact]
    public async Task GhiChuTiepDon_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().PutAsJsonAsync($"/api/v1/patients/{Pid}/reception-note", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-27: GET trang thai CGM khi chua dang nhap -> 401
    [ApiFact]
    public async Task TrangThaiCgm_ChuaDangNhap_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/v1/patients/{Pid}/cgm-status");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PATIENT-28: GET danh sach benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-29: GET tim kiem benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TimKiemBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/patients/search?q=abc");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-30: GET check trung CCCD thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CheckTrungCccd_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/v1/patients/check-cccd-duplicate?id_number=001099001234");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-31: GET chi tiet benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ChiTietBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-32: GET lo trinh ngoai he thong thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LoTrinhNgoai_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/external-pathway");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-33: POST tao benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TaoBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync("/api/v1/patients", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-34: PUT cap nhat benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task CapNhatBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/patients/{Pid}", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-35: PUT ap dung truong CCCD thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ApDungTruongCccd_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/patients/{Pid}/apply-cccd-fields", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-36: DELETE benh nhan thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaBenhNhan_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/patients/{Pid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-37: GET lich su kham thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task LichSuKham_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/encounters");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-38: GET danh sach di ung thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachDiUng_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/allergies");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-39: POST them di ung thieu quyen lam sang -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ThemDiUng_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/patients/{Pid}/allergies", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-40: POST them di ung chi co patient.write (khong co patient.clinical.write) -> 403
    [ApiFact]
    public async Task ThemDiUng_ChiCoQuyenHanhChinh_Tra403()
    {
        var res = await _fx.ClientWith("patient.write").PostAsJsonAsync($"/api/v1/patients/{Pid}/allergies", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-41: DELETE di ung thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaDiUng_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/patients/{Pid}/allergies/{Sid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-42: GET nguoi giam ho thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachNguoiGiamHo_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/guardians");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-43: GET the BHYT thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachTheBhyt_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/insurance");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-44: POST them the BHYT thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ThemTheBhyt_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/patients/{Pid}/insurance", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-45: DELETE the BHYT thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task XoaTheBhyt_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().DeleteAsync($"/api/v1/patients/{Pid}/insurance/{Sid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-46: GET danh ba khan cap thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhBaKhanCap_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/emergency-contacts");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-47: POST lien he khan cap thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task ThemLienHeKhanCap_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PostAsJsonAsync($"/api/v1/patients/{Pid}/emergency-contacts", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-48: GET consents thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task DanhSachDongY_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/consents");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-49: PUT ghi chu tiep don thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task GhiChuTiepDon_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().PutAsJsonAsync($"/api/v1/patients/{Pid}/reception-note", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-50: GET trang thai CGM thieu quyen -> 403 PERMISSION_DENIED
    [ApiFact]
    public async Task TrangThaiCgm_ThieuQuyen_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/v1/patients/{Pid}/cgm-status");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-PATIENT-51: GET danh sach benh nhan voi dung quyen patient.read -> 200
    [ApiFact]
    public async Task DanhSachBenhNhan_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("patient.read").GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-PATIENT-52: GET tim kiem benh nhan voi dung quyen patient.read -> 200
    [ApiFact]
    public async Task TimKiemBenhNhan_DungQuyen_Tra200()
    {
        var res = await _fx.ClientWith("patient.read").GetAsync("/api/v1/patients/search?q=nguyen");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ITC-PATIENT-53: GET danh sach benh nhan voi token het han -> 401
    [ApiFact]
    public async Task DanhSachBenhNhan_TokenHetHan_Tra401()
    {
        var res = await _fx.ClientWithToken(TestTokens.Expired()).GetAsync("/api/v1/patients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
