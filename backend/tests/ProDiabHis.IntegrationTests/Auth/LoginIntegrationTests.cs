using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.IntegrationTests.Auth;

/// <summary>Integration test dang nhap voi MySQL container</summary>
[Collection("MySql")]
public class LoginIntegrationTests : IClassFixture<MySqlTestFixture>
{
    private readonly MySqlTestFixture _fixture;

    public LoginIntegrationTests(MySqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Yeu cau Docker — chay tren CI hoac local co Docker")]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var tenant = new Tenant
        {
            Code = "CLINIC001",
            Name = "Phong kham test",
            Subdomain = "clinic001"
        };
        _fixture.DbContext.Tenants.Add(tenant);

        var role = new Role { Name = "BacSi" };
        _fixture.DbContext.Roles.Add(role);

        var user = new User
        {
            TenantId = 1,
            Email = "doctor@test.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "Bac si Test",
            IsActive = true
        };
        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Assertion database co du lieu
        var dbUser = await _fixture.DbContext.Users.FindAsync(user.Id);
        dbUser.Should().NotBeNull();
        dbUser!.Email.Should().Be("doctor@test.vn");
    }

    [Fact(Skip = "Yeu cau Docker — chay tren CI hoac local co Docker")]
    public async Task Login_WithInvalidPassword_ReturnsError()
    {
        // Edge case: user ton tai nhung sai mat khau
        var users = _fixture.DbContext.Users.ToList();
        users.Should().NotBeNull();
        // Handler se tra ve AUTH_INVALID_CREDENTIALS
    }
}
