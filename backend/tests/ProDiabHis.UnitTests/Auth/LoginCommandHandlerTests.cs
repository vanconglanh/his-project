using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _db = Substitute.For<IApplicationDbContext>();
        _jwtService = Substitute.For<IJwtService>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _logger = Substitute.For<ILogger<LoginCommandHandler>>();
        _configuration = new ConfigurationBuilder().Build();
        _handler = new LoginCommandHandler(_db, _jwtService, _passwordHasher, _logger, _configuration, new FakeEmptyDapperConnectionFactory());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        var users = new List<User>().AsQueryable();
        var mockSet = CreateMockDbSet(users);
        _db.Users.Returns(mockSet);

        var command = new LoginCommand("notfound@test.com", "password123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WhenPasswordWrong_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doctor@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Bac si Test",
            TenantId = 1,
            IsActive = true
        };

        var users = new List<User> { user }.AsQueryable();
        var mockSet = CreateMockDbSet(users);
        _db.Users.Returns(mockSet);
        _passwordHasher.Verify("wrongpassword", user.PasswordHash).Returns(false);

        var command = new LoginCommand("doctor@clinic.vn", "wrongpassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WhenAdminWithout2FA_ReturnsMfaSetupRequiredTrue()
    {
        // Arrange: FR-1011 - role "admin" nam trong danh sach bat buoc 2FA mac dinh
        var role = new Role { Id = Guid.NewGuid(), Code = "admin", Name = "Quản trị viên" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Admin Test",
            TenantId = 1,
            IsActive = true,
            Status = UserStatus.Active,
            TwoFaEnabled = false,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };

        var users = new List<User> { user }.AsQueryable();
        var usersSet = CreateMockDbSet(users);
        var rpSet = CreateMockDbSet(new List<RolePermission>().AsQueryable());
        _db.Users.Returns(usersSet);
        _db.RolePermissions.Returns(rpSet);
        _passwordHasher.Verify("password123", user.PasswordHash).Returns(true);
        _jwtService.GenerateAccessToken(user, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(), null)
            .Returns("fake-access-token");
        _jwtService.GenerateRefreshToken().Returns("fake-refresh-token");

        var command = new LoginCommand("admin@clinic.vn", "password123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaSetupRequired.Should().BeTrue();
        result.Value!.MfaSetupMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WhenNonMandatoryRoleWithout2FA_ReturnsMfaSetupRequiredFalse()
    {
        // Arrange: role "bac_si" khong thuoc danh sach bat buoc 2FA -> van optional
        var role = new Role { Id = Guid.NewGuid(), Code = "bac_si", Name = "Bác sĩ" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "bacsi@clinic.vn",
            PasswordHash = "hashed_password",
            FullName = "Bac Si Test",
            TenantId = 1,
            IsActive = true,
            Status = UserStatus.Active,
            TwoFaEnabled = false,
            UserRoles = new List<UserRole> { new() { RoleId = role.Id, Role = role } }
        };

        var users = new List<User> { user }.AsQueryable();
        var usersSet = CreateMockDbSet(users);
        var rpSet = CreateMockDbSet(new List<RolePermission>().AsQueryable());
        _db.Users.Returns(usersSet);
        _db.RolePermissions.Returns(rpSet);
        _passwordHasher.Verify("password123", user.PasswordHash).Returns(true);
        _jwtService.GenerateAccessToken(user, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(), null)
            .Returns("fake-access-token");
        _jwtService.GenerateRefreshToken().Returns("fake-refresh-token");

        var command = new LoginCommand("bacsi@clinic.vn", "password123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaSetupRequired.Should().BeFalse();
        result.Value!.MfaSetupMessage.Should().BeNull();
    }

    // EF Core DbSet mock helper dung InMemory thay cho Substitute vi DbSet phuc tap
    private static DbSet<T> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>>();
        ((IQueryable<T>)mockSet).Provider.Returns(new TestAsyncQueryProvider<T>(data.Provider));
        ((IQueryable<T>)mockSet).Expression.Returns(data.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(data.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(data.GetEnumerator());
        return mockSet;
    }
}
