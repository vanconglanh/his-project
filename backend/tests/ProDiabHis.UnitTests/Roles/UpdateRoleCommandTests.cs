using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Roles;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.UnitTests.Roles;

/// <summary>
/// BUG-01 (tester phat hien sau UTC): UpdateRoleCommandHandler khong ghi audit log — sua/khoa vai tro
/// khong de lai dau vet, vi pham CLAUDE.md (audit moi thao tac tren du lieu nhay cam). Dang chu y vi
/// role la vector vua duoc va lo hong leo thang quyen (ROLE_CODE_RESERVED). Test nay dam bao
/// IAuditService.LogAsync duoc goi dung voi action UPDATE (thanh cong) va UPDATE_DENIED (bi chan vi
/// role SYSTEM), pattern bam sat EmrHandlersTests (UpdateEmrTemplate).
/// </summary>
public class UpdateRoleCommandTests
{
    private static UpdateRoleCommandHandler CreateHandler(out AppDbContext db, out IAuditService audit)
    {
        db = TestDbContextFactory.Create();
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(Guid.NewGuid());
        audit = Substitute.For<IAuditService>();
        return new UpdateRoleCommandHandler(db, user, audit);
    }

    [Fact]
    public async Task Handle_CapNhatThanhCong_GoiAuditServiceDungMotLanVoiActionUPDATE()
    {
        var handler = CreateHandler(out var db, out var audit);
        var permission = new Permission { Code = "patient.read", Resource = "patient", Action = "read" };
        var role = new Role
        {
            Code = "QUAN_LY_KHO", Name = "Quản lý kho", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = true
        };
        db.Permissions.Add(permission);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new UpdateRoleCommand("QUAN_LY_KHO", "Quản lý kho (đã sửa)", null, new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await audit.Received(1).LogAsync("UPDATE", "ROLE", role.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiRoleLaSystem_TuChoiVaGhiAuditUPDATE_DENIED()
    {
        var handler = CreateHandler(out var db, out var audit);
        var role = new Role
        {
            Code = "ADMIN", Name = "Quản trị hệ thống", RoleType = RoleType.System,
            TenantId = null, IsActive = true
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new UpdateRoleCommand("ADMIN", "Tên bị sửa trái phép", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_SYSTEM_PROTECTED");

        // Noi dung role SYSTEM khong duoc phep thay doi
        var unchanged = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "ADMIN");
        unchanged.Name.Should().Be("Quản trị hệ thống");

        await audit.Received(1).LogAsync("UPDATE_DENIED", "ROLE", role.Id.ToString(),
            AuditSeverity.WARN, false, Arg.Any<string?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiRoleKhongTonTai_KhongGoiAuditService()
    {
        var handler = CreateHandler(out _, out var audit);

        var result = await handler.Handle(
            new UpdateRoleCommand("KHONG_TON_TAI", "Tên mới", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_NOT_FOUND");

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
