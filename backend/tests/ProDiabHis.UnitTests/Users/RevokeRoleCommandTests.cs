using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Users;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Users;

/// <summary>
/// BUG-CRITICAL (QC phat hien tren staging): xoa vai tro (soft-delete Role.DeletedAt) khong thu hoi
/// duoc quyen cua user dang giu vai tro do, va Admin cung KHONG tu don dep duoc qua API vi
/// RevokeRoleCommandHandler query UserRoles.Include(ur => ur.Role) khong IgnoreQueryFilters() —
/// khi role da bi xoa mem, navigation ur.Role bi Global Query Filter loc mat, handler khong tim thay
/// UserRole nen tra ve 404 USER_ROLE_NOT_FOUND, khong xoa duoc UserRole thua. Test nay dung
/// AppDbContext THAT (InMemory) de EF Core Global Query Filter that su hoat dong.
/// </summary>
public class RevokeRoleCommandTests
{
    private static RevokeRoleCommandHandler CreateHandler(
        out ProDiabHis.Infrastructure.Persistence.AppDbContext db, out IAuditService audit)
    {
        db = TestDbContextFactory.Create();
        audit = Substitute.For<IAuditService>();
        return new RevokeRoleCommandHandler(db, audit);
    }

    [Fact]
    public async Task Handle_KhiRoleDaBiXoaMemNhungUserVanConUserRole_XoaThanhCong()
    {
        // Arrange
        var handler = CreateHandler(out var db, out var audit);

        var role = new Role
        {
            Code = "CUSTOM_DA_XOA", Name = "Vai tro tuy chinh da xoa", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = false, DeletedAt = DateTime.UtcNow
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var user = new User
        {
            TenantId = 1, Email = "user.x@clinic.vn", FullName = "User X", PasswordHash = "h", IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, TenantId = 1 });
        await db.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new RevokeRoleCommand(user.Id, "CUSTOM_DA_XOA"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var remaining = await db.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id && ur.RoleId == role.Id)
            .ToListAsync();
        remaining.Should().BeEmpty();

        await audit.Received(1).LogAsync(AuditAction.Update, "user", user.Id.ToString(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());

        // Vai tro van bi xoa mem — Revoke KHONG duoc phuc hoi role hay cap quyen gi them
        var roleAfter = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "CUSTOM_DA_XOA");
        roleAfter.DeletedAt.Should().NotBeNull();
        roleAfter.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_KhiUserKhongCoRoleDo_TraVeUserRoleNotFound()
    {
        var handler = CreateHandler(out var db, out var audit);

        var user = new User
        {
            TenantId = 1, Email = "user.y@clinic.vn", FullName = "User Y", PasswordHash = "h", IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new RevokeRoleCommand(user.Id, "KHONG_TON_TAI"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_ROLE_NOT_FOUND");

        await audit.DidNotReceive().LogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
