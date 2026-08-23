using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

/// <summary>
/// BUG-CRITICAL (QC phat hien tren staging): xoa vai tro (soft-delete Role.DeletedAt) KHONG thu hoi
/// quyen cua user dang giu vai tro do — vi RefreshTokenCommandHandler dung IgnoreQueryFilters() tren
/// query RefreshTokens kem Include(User).ThenInclude(UserRoles).ThenInclude(Role), vo tinh tat luon
/// global query filter Role.DeletedAt == null, khien role da xoa mem van duoc nap lai vao access token
/// moi khi refresh. Dung AppDbContext THAT (InMemory) de EF Core Global Query Filter that su hoat
/// dong, dam bao tai hien dung bug goc.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ILogger<RefreshTokenCommandHandler> _logger = Substitute.For<ILogger<RefreshTokenCommandHandler>>();

    private RefreshTokenCommandHandler CreateHandler(out ProDiabHis.Infrastructure.Persistence.AppDbContext db)
    {
        db = TestDbContextFactory.Create();
        return new RefreshTokenCommandHandler(db, _jwtService, _logger);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalidOrExpired_ReturnsFailure()
    {
        var handler = CreateHandler(out _);

        var result = await handler.Handle(new RefreshTokenCommand("khong-ton-tai"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Handle_WhenUserHasSoftDeletedRole_NewAccessTokenExcludesDeletedRole()
    {
        // Arrange
        var handler = CreateHandler(out var db);

        var activeRole = new Role
        {
            Code = "BACSI", Name = "Bac si", RoleType = RoleType.System, TenantId = null, IsActive = true
        };
        var deletedRole = new Role
        {
            Code = "CUSTOM_DA_XOA", Name = "Vai tro tuy chinh da xoa", RoleType = RoleType.Custom,
            TenantId = 1, IsActive = false, DeletedAt = DateTime.UtcNow
        };
        db.Roles.AddRange(activeRole, deletedRole);
        await db.SaveChangesAsync();

        var user = new User
        {
            TenantId = 1,
            Email = "doctor@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Bac si Test",
            IsActive = true,
            Status = UserStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = activeRole.Id, TenantId = 1 },
            new UserRole { UserId = user.Id, RoleId = deletedRole.Id, TenantId = 1 });

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TenantId = 1,
            Token = "refresh-token-cu",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        });
        await db.SaveChangesAsync();

        _jwtService.GenerateAccessToken(
            Arg.Any<User>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IEnumerable<string>?>()).Returns("access_token_moi");
        _jwtService.GenerateRefreshToken().Returns("refresh_token_moi");

        // Act
        var result = await handler.Handle(new RefreshTokenCommand("refresh-token-cu"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.User.RoleCodes.Should().Contain("BACSI");
        result.Value!.User.RoleCodes.Should().NotContain("CUSTOM_DA_XOA");
    }
}
