using FluentAssertions;
using FluentValidation;
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

    // ─── BUG-03 (Minor, QC final review): UpdateRoleCommandHandler cap nhat thanh cong
    // nhung khong gan UpdatedBy -> mat dau vet ai la nguoi sua vai tro gan nhat, khac voi
    // pattern audit field da dung o cac handler khac (vd EmrHandlers.cs UpdateEmrTemplate).
    // Test nay dam bao UpdatedBy duoc gan dung bang ICurrentUser.UserId sau khi Update. ───
    [Fact]
    public async Task Handle_CapNhatThanhCong_GanDungUpdatedBy()
    {
        var db = TestDbContextFactory.Create();
        var currentUserId = Guid.NewGuid();
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(currentUserId);
        var audit = Substitute.For<IAuditService>();
        var handler = new UpdateRoleCommandHandler(db, user, audit);

        var permission = new Permission { Code = "patient.read", Resource = "patient", Action = "read" };
        var role = new Role
        {
            Code = "QUAN_LY_KHO", Name = "Quản lý kho", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = true, UpdatedBy = null
        };
        db.Permissions.Add(permission);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new UpdateRoleCommand("QUAN_LY_KHO", "Quản lý kho (đã sửa)", null, new[] { permission.Code }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == "QUAN_LY_KHO");
        updated.UpdatedBy.Should().Be(currentUserId);
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

/// <summary>
/// BUG-02 (Major, QC final review + tester UTC): UpdateRoleCommand truoc day khong co
/// validator (khac voi CreateRoleCommand da co). Test nay dam bao Name/Description khi
/// KHONG gui len (null) van hop le (optional field), nhung khi CO gui len ma vuot
/// MaximumLength thi phai bi tu choi — va khi chay qua ValidationBehavior (pipeline that
/// su dung trong app) se throw FluentValidation.ValidationException (-> HTTP 400 qua
/// ErrorHandlingMiddleware) THAY VI vo constraint DB (name VARCHAR(100)) roi tra ve 500.
/// </summary>
public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    [Fact]
    public void Validator_KhiNameVaDescriptionNull_HopLe()
    {
        var result = _validator.Validate(new UpdateRoleCommand("QUAN_LY_KHO", null, null, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_KhiNameGuiLenRong_TraLoi()
    {
        var result = _validator.Validate(new UpdateRoleCommand("QUAN_LY_KHO", "", null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRoleCommand.Name));
    }

    [Fact]
    public void Validator_KhiNameVuotQua100KyTu_TraLoi()
    {
        var result = _validator.Validate(new UpdateRoleCommand("QUAN_LY_KHO", new string('A', 101), null, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRoleCommand.Name));
    }

    [Fact]
    public void Validator_KhiDescriptionVuotQua500KyTu_TraLoi()
    {
        var result = _validator.Validate(
            new UpdateRoleCommand("QUAN_LY_KHO", "Tên hợp lệ", new string('B', 501), null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRoleCommand.Description));
    }

    [Fact]
    public async Task Pipeline_KhiNameVuotQuaMaxLength_ThrowValidationException_KhongGoiHandler()
    {
        var behavior = new ValidationBehavior<UpdateRoleCommand, Result<RoleResponse>>(
            new[] { new UpdateRoleCommandValidator() });

        var command = new UpdateRoleCommand("QUAN_LY_KHO", new string('A', 200), null, null);
        var handlerCalled = false;

        Func<Task> act = () => behavior.Handle(command, () =>
        {
            handlerCalled = true;
            return Task.FromResult(Result<RoleResponse>.Success(null!));
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        handlerCalled.Should().BeFalse();
    }
}
