using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Users;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Users;

public class AcceptInviteCommandHandlerTests
{
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private AcceptInviteCommandHandler CreateHandler(out ProDiabHis.Infrastructure.Persistence.AppDbContext db)
    {
        db = TestDbContextFactory.Create();
        return new AcceptInviteCommandHandler(db, _passwordHasher, _jwtService);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsExpiredError()
    {
        var handler = CreateHandler(out _);
        var result = await handler.Handle(
            new AcceptInviteCommand("invalid_token", "M@tKhau12345!", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_INVITE_EXPIRED");
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ReturnsExpiredError()
    {
        var handler = CreateHandler(out var db);
        db.Users.Add(new User
        {
            TenantId = 1, Email = "test@test.vn", FullName = "Test", PasswordHash = "h",
            InviteToken = "validtoken123",
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new AcceptInviteCommand("validtoken123", "M@tKhau12345!", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_INVITE_EXPIRED");
    }

    [Fact]
    public async Task Handle_WhenWeakPassword_ReturnsPasswordTooWeakError()
    {
        var handler = CreateHandler(out var db);
        db.Users.Add(new User
        {
            TenantId = 1, Email = "test@test.vn", FullName = "Test", PasswordHash = "h",
            InviteToken = "validtoken456",
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new AcceptInviteCommand("validtoken456", "weak", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PASSWORD_TOO_WEAK");
    }

    [Fact]
    public async Task Handle_WhenValidTokenAndStrongPassword_ActivatesUser()
    {
        var handler = CreateHandler(out var db);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, TenantId = 1, Email = "test@test.vn", FullName = "Test", PasswordHash = "h",
            InviteToken = "validtoken789",
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = UserStatus.Pending
        });
        await db.SaveChangesAsync();

        _passwordHasher.Hash("M@tKhauManh2026!").Returns("hashed_pwd");
        _jwtService.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>?>()).Returns("access_token_xyz");
        _jwtService.GenerateRefreshToken().Returns("refresh_token_xyz");

        var result = await handler.Handle(
            new AcceptInviteCommand("validtoken789", "M@tKhauManh2026!", "Updated Name"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access_token_xyz");

        var user = db.Users.IgnoreQueryFilters().First(u => u.Id == userId);
        user.Status.Should().Be(UserStatus.Active);
        user.InviteToken.Should().BeNull();
        user.FullName.Should().Be("Updated Name");
    }
}
