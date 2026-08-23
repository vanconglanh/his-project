using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
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
    private static CreateRoleCommandHandler CreateHandler(out AppDbContext db, out Permission permission, out IAuditService audit)
    {
        db = TestDbContextFactory.Create();
        permission = new Permission { Code = "patient.read", Resource = "patient", Action = "read" };
        db.Permissions.Add(permission);
        db.SaveChanges();

        var tenant = new FakeTenantProvider(1);
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(Guid.NewGuid());
        audit = Substitute.For<IAuditService>();
        return new CreateRoleCommandHandler(db, tenant, user, audit);
    }

    [Theory]
    [InlineData("SUPER_ADMIN")]
    [InlineData("ADMIN")]
    public async Task Handle_KhiCodeLaMaReserved_TuChoiVoiLoiROLE_CODE_RESERVED(string reservedCode)
    {
        var handler = CreateHandler(out _, out var permission, out _);

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
        var handler = CreateHandler(out _, out var permission, out _);

        var result = await handler.Handle(
            new CreateRoleCommand("super_admin", "Vai trò giả mạo", null, new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_CODE_RESERVED");
    }

    [Fact]
    public async Task Handle_KhiCodeHopLeVaKhongTrungReserved_TaoThanhCongVaLaRoleCustom()
    {
        var handler = CreateHandler(out var db, out var permission, out _);

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

    // ─── BUG-01: Create phai ghi audit log "CREATE" dung 1 lan khi thanh cong (role la vector
    // vua duoc va lo hong leo thang quyen ROLE_CODE_RESERVED, khong duoc phep "im lang") ───
    [Fact]
    public async Task Handle_TaoThanhCong_GoiAuditServiceDungMotLan()
    {
        var handler = CreateHandler(out _, out var permission, out var audit);

        var result = await handler.Handle(
            new CreateRoleCommand("QUAN_LY_KHO", "Quản lý kho", "Vai trò quản lý kho thuốc", new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await audit.Received(1).LogAsync("CREATE", "ROLE", Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ─── Khi bi tu choi vi ma reserved, KHONG duoc goi audit CREATE (role chua duoc tao) ───
    [Fact]
    public async Task Handle_KhiCodeLaMaReserved_KhongGoiAuditService()
    {
        var handler = CreateHandler(out _, out var permission, out var audit);

        await handler.Handle(
            new CreateRoleCommand("SUPER_ADMIN", "Vai trò giả mạo", null, new[] { permission.Code }),
            CancellationToken.None);

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ─── BUG-01 (Major, QC final review): tao lai role trung ma sau khi role cu cung ma
    // da bi xoa mem phai THANH CONG (khong con vo UNIQUE constraint tang DB -> HTTP 500,
    // xem migration 9077_fix_roles_unique_code_soft_delete.sql). Test nay xac nhan tang
    // ung dung (check DeletedAt == null) van dung sau khi fix DB — code khong con "chiem
    // cho vinh vien" boi role da xoa mem trung ma. ───
    [Fact]
    public async Task Handle_KhiRoleCuCungCodeDaXoaMem_TaoLaiThanhCong()
    {
        var handler = CreateHandler(out var db, out var permission, out var audit);

        var deletedRole = new Role
        {
            Code = "QUAN_LY_KHO", Name = "Quản lý kho (bản cũ)", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = false, DeletedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Roles.Add(deletedRole);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new CreateRoleCommand("QUAN_LY_KHO", "Quản lý kho (bản mới)", "Tạo lại sau khi xóa mềm",
                new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("QUAN_LY_KHO");

        var roles = await db.Roles.IgnoreQueryFilters()
            .Where(r => r.Code == "QUAN_LY_KHO").ToListAsync();
        roles.Should().HaveCount(2);
        var newRole = roles.Should().ContainSingle(r => r.DeletedAt == null && r.Name == "Quản lý kho (bản mới)")
            .Subject;
        newRole.Id.Should().NotBe(deletedRole.Id);

        await audit.Received(1).LogAsync("CREATE", "ROLE", newRole.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
