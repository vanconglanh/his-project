using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Users;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.UnitTests.Users;

/// <summary>
/// Lo hong bao mat (fix dot truoc, chua co test): AssignRolesCommandHandler tra cuu Role bang
/// IgnoreQueryFilters() — bat buoc phai the vi role SYSTEM co TenantId = NULL nen bi EF Core Global
/// Query Filter loc mat. Hau qua phu: cot Code la UNIQUE TOAN CUC, nen tenant A doan/biet duoc ma
/// role CUSTOM cua tenant B (xem Defect#7 utc-vai-tro.md: POST trung code tra ROLE_CODE_TAKEN) thi
/// co the gan role CUSTOM cua tenant B cho user cua minh => leo thang quyen xuyen tenant.
/// Handler nay chan bang ROLE_TENANT_MISMATCH. Test dung AppDbContext THAT (InMemory) de Global
/// Query Filter thuc su hoat dong, giong RevokeRoleCommandTests / DeleteRoleCommandTests.
/// </summary>
public class AssignRolesCommandTests
{
    private const int TenantHienTai = 1;
    private const int TenantKhac = 999;

    private static AssignRolesCommandHandler CreateHandler(
        out AppDbContext db, out IAuditService audit, int tenantId = TenantHienTai)
    {
        db = TestDbContextFactory.Create(tenantId: tenantId);
        audit = Substitute.For<IAuditService>();
        return new AssignRolesCommandHandler(db, new FakeTenantProvider(tenantId), audit);
    }

    private static async Task<User> ThemUserAsync(AppDbContext db, string email, int tenantId = TenantHienTai)
    {
        var user = new User
        {
            TenantId = tenantId, Email = email, FullName = "Nguoi dung test",
            PasswordHash = "hash", IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Role> ThemRoleAsync(AppDbContext db, string code, string roleType, int? tenantId)
    {
        var role = new Role
        {
            Code = code, Name = "Vai tro " + code, RoleType = roleType,
            TenantId = tenantId, IsActive = true
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    [Fact]
    public async Task Handle_GanRoleSystemChoUserCungTenant_ThanhCong()
    {
        // Arrange — role SYSTEM dung chung moi tenant nen TenantId = NULL
        var handler = CreateHandler(out var db, out var audit);
        var role = await ThemRoleAsync(db, "bac_si", RoleType.System, null);
        var user = await ThemUserAsync(db, "bacsi.cung.tenant@clinic.vn");

        // Act
        var result = await handler.Handle(
            new AssignRolesCommand(user.Id, new[] { "bac_si" }), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorCode.Should().BeNull();

        var granted = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id).ToListAsync();
        granted.Should().ContainSingle();
        granted[0].RoleId.Should().Be(role.Id);
        granted[0].TenantId.Should().Be(TenantHienTai, "tenant_id phai lay tu ITenantProvider, khong trust client");

        await audit.Received(1).LogAsync(AuditAction.Update, "user", user.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GanRoleCustomDungTenantHienTai_ThanhCong()
    {
        // Arrange — role CUSTOM thuoc DUNG tenant dang dang nhap
        var handler = CreateHandler(out var db, out var audit);
        var role = await ThemRoleAsync(db, "QUAN_LY_KHO", RoleType.Custom, TenantHienTai);
        var user = await ThemUserAsync(db, "duocsi.cung.tenant@clinic.vn");

        // Act
        var result = await handler.Handle(
            new AssignRolesCommand(user.Id, new[] { "QUAN_LY_KHO" }), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var granted = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id).ToListAsync();
        granted.Should().ContainSingle();
        granted[0].RoleId.Should().Be(role.Id);

        await audit.Received(1).LogAsync(AuditAction.Update, "user", user.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GanRoleCustomCuaTenantKhac_TraVeRoleTenantMismatch()
    {
        // Arrange — role CUSTOM thuoc tenant KHAC (999), user thuoc tenant hien tai (1)
        var handler = CreateHandler(out var db, out var audit);
        var roleTenantKhac = await ThemRoleAsync(db, "KE_TOAN_TRUONG_T999", RoleType.Custom, TenantKhac);
        var user = await ThemUserAsync(db, "ketoan.tenant1@clinic.vn");

        // Act
        var result = await handler.Handle(
            new AssignRolesCommand(user.Id, new[] { "KE_TOAN_TRUONG_T999" }), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_TENANT_MISMATCH");
        result.ErrorMessage.Should().Be("Không thể gán vai trò không thuộc phòng khám hiện tại");

        // Khong duoc ghi bat ky UserRole nao
        var granted = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id).ToListAsync();
        granted.Should().BeEmpty();

        // Role cua tenant khac phai nguyen ven
        var roleAfter = await db.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.Code == "KE_TOAN_TRUONG_T999");
        roleAfter.TenantId.Should().Be(TenantKhac);
        roleAfter.Id.Should().Be(roleTenantKhac.Id);

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GanLoRoleSystemLanRoleCustomTenantKhac_ChanCaLo_AllOrNothing()
    {
        // Arrange — tron 1 role hop le va 1 role cua tenant khac trong cung 1 request
        var handler = CreateHandler(out var db, out var audit);
        await ThemRoleAsync(db, "bac_si", RoleType.System, null);
        await ThemRoleAsync(db, "ROLE_LEN_T999", RoleType.Custom, TenantKhac);
        var user = await ThemUserAsync(db, "tron.role@clinic.vn");

        // Act
        var result = await handler.Handle(
            new AssignRolesCommand(user.Id, new[] { "bac_si", "ROLE_LEN_T999" }), CancellationToken.None);

        // Assert — khong duoc gan "mot phan", ke ca role hop le cung phai bi tu choi
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_TENANT_MISMATCH");

        var granted = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id).ToListAsync();
        granted.Should().BeEmpty();

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiRoleKhongTonTai_TraVeRoleNotFound()
    {
        var handler = CreateHandler(out var db, out var audit);
        var user = await ThemUserAsync(db, "role.la@clinic.vn");

        var result = await handler.Handle(
            new AssignRolesCommand(user.Id, new[] { "KHONG_TON_TAI" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_NOT_FOUND");

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiUserThuocTenantKhac_TraVeUserNotFound()
    {
        // Global Query Filter tren User phai chan doc user cua tenant khac
        var handler = CreateHandler(out var db, out var audit);
        await ThemRoleAsync(db, "bac_si", RoleType.System, null);
        var userTenantKhac = await ThemUserAsync(db, "nguoi.la@tenant999.vn", TenantKhac);

        var result = await handler.Handle(
            new AssignRolesCommand(userTenantKhac.Id, new[] { "bac_si" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}