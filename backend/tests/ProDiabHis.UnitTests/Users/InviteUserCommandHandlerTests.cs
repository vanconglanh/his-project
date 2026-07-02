using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Users;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Users;

public class InviteUserCommandHandlerTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<InviteUserCommandHandler> _logger =
        Substitute.For<ILogger<InviteUserCommandHandler>>();

    private InviteUserCommandHandler CreateHandler(FakeTenantProvider tenantProvider, string dbName = "")
    {
        var db = TestDbContextFactory.Create(string.IsNullOrEmpty(dbName) ? Guid.NewGuid().ToString() : dbName);
        return new InviteUserCommandHandler(db, _emailSender, tenantProvider, _audit, _logger);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsFailure()
    {
        var tenantProvider = new FakeTenantProvider(1);
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            TenantId = 1, Email = "exists@test.vn", FullName = "Existing User",
            PasswordHash = "hash", Status = UserStatus.Active
        });
        await db.SaveChangesAsync();

        var handler = new InviteUserCommandHandler(db, _emailSender, tenantProvider, _audit, _logger);
        var result = await handler.Handle(
            new InviteUserCommand("exists@test.vn", "Test User", null, new[] { "BACSI" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_EMAIL_EXISTS");
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        var tenantProvider = new FakeTenantProvider(1);
        var db = TestDbContextFactory.Create();
        var handler = new InviteUserCommandHandler(db, _emailSender, tenantProvider, _audit, _logger);

        var result = await handler.Handle(
            new InviteUserCommand("new@test.vn", "New User", null, new[] { "INVALID_ROLE" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesUserAndSendsEmail()
    {
        var tenantProvider = new FakeTenantProvider(1);
        var db = TestDbContextFactory.Create();
        db.Roles.Add(new Role { Code = "BACSI", Name = "Bac si", RoleType = "SYSTEM" });
        await db.SaveChangesAsync();

        var handler = new InviteUserCommandHandler(db, _emailSender, tenantProvider, _audit, _logger);
        var result = await handler.Handle(
            new InviteUserCommand("doctor@clinic.vn", "Bac si Nguyen", "0901234567", new[] { "BACSI" }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("doctor@clinic.vn");
        result.Value.InviteExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));

        await _emailSender.Received(1).SendAsync(
            "doctor@clinic.vn", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Kiem tra user duoc tao trong DB
        var createdUser = db.Users.FirstOrDefault(u => u.Email == "doctor@clinic.vn");
        createdUser.Should().NotBeNull();
        createdUser!.InviteToken.Should().NotBeNullOrEmpty();
        createdUser.Status.Should().Be(UserStatus.Pending);
    }
}
