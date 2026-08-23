using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Roles;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.UnitTests.Roles;

/// <summary>
/// Regression test cho lo hong Critical QC phat hien tren staging: tenant thuong (co quyen
/// role.write + user.assign_role, khong can admin) tao role CUSTOM voi Code = "SUPER_ADMIN"
/// roi tu gan cho chinh minh -> chiem quyen super admin sau khi dang nhap lai. Fix: CreateRoleCommand
/// phai tu choi tao role neu Code nam trong danh sach ReservedRoleCodes (ADMIN, SUPER_ADMIN...).
/// </summary>
public class CreateRoleCommandTests
{
    private static CreateRoleCommandHandler CreateHandler(out AppDbContext db, out Permission permission)
    {
        db = TestDbContextFactory.Create();
        permission = new Permission { Code = "patient.read", Resource = "patient", Action = "read" };
        db.Permissions.Add(permission);
        db.SaveChanges();

        var tenant = new FakeTenantProvider(1);
        return new CreateRoleCommandHandler(db, tenant);
    }

    [Theory]
    [InlineData("SUPER_ADMIN")]
    [InlineData("ADMIN")]
    public async Task Handle_KhiCodeLaMaReserved_TuChoiVoiLoiROLE_CODE_RESERVED(string reservedCode)
    {
        var handler = CreateHandler(out _, out var permission);

        var result = await handler.Handle(
            new CreateRoleCommand(reservedCode, "Vai trò giả mạo", null, new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_CODE_RESERVED");
    }

    [Fact]
    public async Task Handle_KhiCodeReservedVietThuong_VanBiTuChoi()
    {
        // Reserved-check khong phan biet hoa/thuong, tranh bypass bang "super_admin"
        var handler = CreateHandler(out _, out var permission);

        var result = await handler.Handle(
            new CreateRoleCommand("super_admin", "Vai trò giả mạo", null, new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_CODE_RESERVED");
    }

    [Fact]
    public async Task Handle_KhiCodeHopLeVaKhongTrungReserved_TaoThanhCongVaLaRoleCustom()
    {
        var handler = CreateHandler(out var db, out var permission);

        var result = await handler.Handle(
            new CreateRoleCommand("QUAN_LY_KHO", "Quản lý kho", "Vai trò quản lý kho thuốc", new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("QUAN_LY_KHO");
        result.Value!.RoleType.Should().Be(RoleType.Custom);

        var saved = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "QUAN_LY_KHO");
        saved.RoleType.Should().Be(RoleType.Custom);
        saved.TenantId.Should().Be(1);
    }
}
