using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Auth;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

/// <summary>
/// Regression test cho 2 lo hong bao mat lien quan claim "is_super_admin":
///  1) (Da fix truoc do) Tung gan bang cach so TEN HIEN THI vai tro (vd r.Contains("Quản trị"))
///     thay vi so MA VAI TRO (role_code). Ten hien thi la free-text 1 tenant co the tu dat cho
///     role CUSTOM (vd "Quản trị kho"), nen so theo ten se cho phep gia mao super admin.
///  2) (Critical - QC phat hien tren staging) Tung chi so theo role_code MA KHONG PHAN BIET
///     RoleType — 1 tenant thuong (co quyen role.write + user.assign_role) tu tao role CUSTOM voi
///     Code = "SUPER_ADMIN" roi tu gan cho chinh minh se duoc cap is_super_admin=true khi dang
///     nhap lai. JwtService BAT BUOC phai dua vao tham so systemRoleCodes (chi chua ma cua role
///     co RoleType == System that su, khong ai tao duoc qua API tao role) — KHONG duoc suy tu
///     roleCodes (co the chua ca ma cua role CUSTOM).
/// </summary>
public class JwtServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["JWT__SECRET"].Returns("unit-test-secret-key-must-be-long-enough-1234567890");
        _configuration["Jwt:Issuer"].Returns("ProDiabHis");
        _configuration["Jwt:Audience"].Returns("ProDiabHis");
        // Khong cau hinh ConnectionStrings:DefaultConnection -> LoadPermissions se tra ve rong,
        // tranh phai ket noi MySQL that trong unit test.
        _service = new JwtService(_configuration);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@clinic.vn",
        FullName = "Nguyen Van A",
        TenantId = 1
    };

    private static bool GetIsSuperAdminClaim(string jwt)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        var claim = token.Claims.FirstOrDefault(c => c.Type == "is_super_admin");
        return claim is not null && claim.Value == "true";
    }

    [Fact]
    public void GenerateAccessToken_KhiRoleCodeLaADMINVaLaRoleHeThong_PhaiCoClaimIsSuperAdminTrue()
    {
        // Arrange: user that su la ADMIN (ma role he thong, seed trong sec_roles -> RoleType = System)
        var user = CreateUser();
        var roles = new[] { "Quản trị hệ thống" };
        var roleCodes = new[] { "ADMIN" };
        var systemRoleCodes = new[] { "ADMIN" };

        // Act
        var jwt = _service.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes);

        // Assert
        GetIsSuperAdminClaim(jwt).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_KhiRoleCodeLaSUPER_ADMINVaLaRoleHeThong_PhaiCoClaimIsSuperAdminTrue()
    {
        // Regression: role SYSTEM dung ma van phai duoc cap super admin binh thuong
        var user = CreateUser();
        var roles = new[] { "Super Admin nen tang" };
        var roleCodes = new[] { "SUPER_ADMIN" };
        var systemRoleCodes = new[] { "SUPER_ADMIN" };

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes);

        GetIsSuperAdminClaim(jwt).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_KhiTenHienThiTrungQuanTriVienNhungRoleCodeThuong_PhaiCoClaimIsSuperAdminFalse()
    {
        // Arrange: tenant tu tao role CUSTOM co TEN HIEN THI chua "Quản trị" (vd "Quản trị kho")
        // nhung MA ROLE (Code) khong phai ADMIN/SUPER_ADMIN. User nay KHONG duoc phep la super admin.
        var user = CreateUser();
        var roles = new[] { "Quản trị viên" }; // ten hien thi "gia mao"
        var roleCodes = new[] { "QUAN_TRI_KHO" }; // ma role that su (CUSTOM, khong phai he thong)

        // Act
        var jwt = _service.GenerateAccessToken(user, roles, roleCodes);

        // Assert: KHONG duoc bypass RequireSuperAdminAttribute
        GetIsSuperAdminClaim(jwt).Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_KhiRoleCUSTOMCoMaTrungSUPER_ADMIN_PhaiCoClaimIsSuperAdminFalse()
    {
        // Regression cho lo hong Critical QC phat hien tren staging: tenant thuong tu tao role
        // CUSTOM voi Code = "SUPER_ADMIN" (RoleType = Custom, khong phai role seed he thong) roi tu
        // gan cho chinh minh. roleCodes chua "SUPER_ADMIN" (dung de hien thi/role_code claim) nhung
        // systemRoleCodes KHONG chua no (vi role nay la CUSTOM) -> KHONG duoc cap is_super_admin.
        var user = CreateUser();
        var roles = new[] { "SUPER_ADMIN" };
        var roleCodes = new[] { "SUPER_ADMIN" };
        IEnumerable<string>? systemRoleCodes = Array.Empty<string>(); // role la CUSTOM -> khong nam trong systemRoleCodes

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes);

        GetIsSuperAdminClaim(jwt).Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_KhiRoleThuongKhongPhaiAdmin_PhaiCoClaimIsSuperAdminFalse()
    {
        var user = CreateUser();
        var roles = new[] { "Bác sĩ" };
        var roleCodes = new[] { "BACSI" };

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes);

        GetIsSuperAdminClaim(jwt).Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_KhiKhongTruyenRoleCodes_PhaiCoClaimIsSuperAdminFalse()
    {
        // roleCodes = null (tham so optional) -> khong duoc mac dinh cap super admin
        var user = CreateUser();
        var roles = new[] { "Quản trị hệ thống" };

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes: null);

        GetIsSuperAdminClaim(jwt).Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_KhiTruyenRoleCodesNhungKhongTruyenSystemRoleCodes_PhaiCoClaimIsSuperAdminFalse()
    {
        // roleCodes co ADMIN nhung systemRoleCodes khong truyen (null, mac dinh) -> khong duoc suy
        // luan tu roleCodes, phai coi nhu khong co role he thong nao -> false.
        var user = CreateUser();
        var roles = new[] { "Quản trị hệ thống" };
        var roleCodes = new[] { "ADMIN" };

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes: null);

        GetIsSuperAdminClaim(jwt).Should().BeFalse();
    }
}
