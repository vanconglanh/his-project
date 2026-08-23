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
/// BUG-01 (tester phat hien sau UTC): DeleteRoleCommandHandler khong ghi audit log — xoa vai tro
/// khong de lai dau vet, vi pham CLAUDE.md (audit moi thao tac tren du lieu nhay cam). Dang chu y vi
/// role la vector vua duoc va lo hong leo thang quyen (ROLE_CODE_RESERVED). Test nay dam bao
/// IAuditService.LogAsync duoc goi dung voi action DELETE (thanh cong) va DELETE_DENIED (bi chan vi
/// role SYSTEM), pattern bam sat EmrHandlersTests (DeleteEmrTemplate).
/// </summary>
public class DeleteRoleCommandTests
{
    private static DeleteRoleCommandHandler CreateHandler(out AppDbContext db, out IAuditService audit)
    {
        db = TestDbContextFactory.Create();
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(Guid.NewGuid());
        audit = Substitute.For<IAuditService>();
        return new DeleteRoleCommandHandler(db, user, audit);
    }

    [Fact]
    public async Task Handle_XoaThanhCong_GoiAuditServiceDungMotLanVoiActionDELETE()
    {
        var handler = CreateHandler(out var db, out var audit);
        var role = new Role
        {
            Code = "QUAN_LY_KHO", Name = "Quản lý kho", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = true
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new DeleteRoleCommand("QUAN_LY_KHO"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var deleted = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "QUAN_LY_KHO");
        deleted.DeletedAt.Should().NotBeNull();
        deleted.IsActive.Should().BeFalse();

        await audit.Received(1).LogAsync("DELETE", "ROLE", role.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiRoleLaSystem_TuChoiVaGhiAuditDELETE_DENIED()
    {
        var handler = CreateHandler(out var db, out var audit);
        var role = new Role
        {
            Code = "ADMIN", Name = "Quản trị hệ thống", RoleType = RoleType.System,
            TenantId = null, IsActive = true
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new DeleteRoleCommand("ADMIN"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_SYSTEM_PROTECTED");

        // Vai tro SYSTEM khong duoc phep xoa
        var unchanged = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "ADMIN");
        unchanged.DeletedAt.Should().BeNull();
        unchanged.IsActive.Should().BeTrue();

        await audit.Received(1).LogAsync("DELETE_DENIED", "ROLE", role.Id.ToString(),
            AuditSeverity.WARN, false, Arg.Any<string?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KhiRoleKhongTonTai_KhongGoiAuditService()
    {
        var handler = CreateHandler(out _, out var audit);

        var result = await handler.Handle(new DeleteRoleCommand("KHONG_TON_TAI"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_NOT_FOUND");

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
