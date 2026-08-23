using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _db = Substitute.For<IApplicationDbContext>();
        _jwtService = Substitute.For<IJwtService>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _logger = Substitute.For<ILogger<LoginCommandHandler>>();
        _handler = new LoginCommandHandler(_db, _jwtService, _passwordHasher, _logger);
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

    /// <summary>
    /// BUG-CRITICAL (QC phat hien tren staging): xoa vai tro (soft-delete Role.DeletedAt) KHONG thu
    /// hoi quyen cua user dang giu vai tro do — vi LoginCommandHandler dung IgnoreQueryFilters() tren
    /// query Users kem ThenInclude(Role), vo tinh tat luon global query filter Role.DeletedAt == null,
    /// khien role da xoa mem van duoc nap vao JWT. Test nay dung AppDbContext THAT (InMemory, khong
    /// phai Substitute mock) de EF Core Global Query Filter that su hoat dong — chi voi context that,
    /// IgnoreQueryFilters() moi co tac dung that (voi DbSet mock/Substitute o cac test khac trong file
    /// nay, IgnoreQueryFilters() la no-op nen khong the bay ra bug goc).
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasSoftDeletedRole_RoleCodesExcludeDeletedRole()
    {
        // Arrange
        var db = ProDiabHis.UnitTests.TestDbContextFactory.Create();
        var handler = new LoginCommandHandler(db, _jwtService, _passwordHasher, _logger);

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
        await db.SaveChangesAsync();

        _passwordHasher.Verify("Str0ngP@ssword!", "hashed_password").Returns(true);
        _jwtService.GenerateAccessToken(
            Arg.Any<User>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IEnumerable<string>?>()).Returns("access_token_xyz");
        _jwtService.GenerateRefreshToken().Returns("refresh_token_xyz");

        var command = new LoginCommand("doctor@clinic.vn", "Str0ngP@ssword!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.User.RoleCodes.Should().Contain("BACSI");
        result.Value!.User.RoleCodes.Should().NotContain("CUSTOM_DA_XOA");
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
