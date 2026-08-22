using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Auth;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

/// <summary>
/// Regression test cho lo hong bao mat: claim "is_super_admin" tung duoc gan bang cach so
/// TEN HIEN THI vai tro (vd r.Contains("Quản trị")) thay vi so MA VAI TRO (role_code). Ten hien
/// thi la free-text 1 tenant co the tu dat cho role CUSTOM (vd "Quản trị kho"), nen so theo ten
/// se cho phep user thuong gia mao thanh super admin va bypass RequireSuperAdminAttribute.
/// JwtService phai CHI dua vao tham so roleCodes (Role.Code, on dinh, UNIQUE trong sec_roles).
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
    public void GenerateAccessToken_KhiRoleCodeLaADMIN_PhaiCoClaimIsSuperAdminTrue()
    {
        // Arrange: user that su la ADMIN (ma role he thong, seed trong sec_roles)
        var user = CreateUser();
        var roles = new[] { "Quản trị hệ thống" };
        var roleCodes = new[] { "ADMIN" };

        // Act
        var jwt = _service.GenerateAccessToken(user, roles, roleCodes);

        // Assert
        GetIsSuperAdminClaim(jwt).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_KhiRoleCodeLaSUPER_ADMIN_PhaiCoClaimIsSuperAdminTrue()
    {
        var user = CreateUser();
        var roles = new[] { "Super Admin nen tang" };
        var roleCodes = new[] { "SUPER_ADMIN" };

        var jwt = _service.GenerateAccessToken(user, roles, roleCodes);

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
}
