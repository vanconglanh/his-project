using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Tenants;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Tenants;

public class CreateTenantCommandHandlerTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();
    private readonly ILogger<CreateTenantCommandHandler> _logger =
        Substitute.For<ILogger<CreateTenantCommandHandler>>();

    private CreateTenantCommandHandler CreateHandler(string dbName)
    {
        var db = TestDbContextFactory.Create(dbName);
        return new CreateTenantCommandHandler(db, _emailSender, _passwordHasher, _config, _logger);
    }

    [Fact]
    public async Task Handle_WhenSubdomainTaken_ReturnsFailure()
    {
        var db = TestDbContextFactory.Create();
        db.Tenants.Add(new Tenant
        {
            Code = "PK000", Name = "Existing", Subdomain = "anbinh", Status = "ACTIVE"
        });
        await db.SaveChangesAsync();

        var handler = new CreateTenantCommandHandler(db, _emailSender, _passwordHasher, _config, _logger);
        var result = await handler.Handle(CreateCommand("PK001", "anbinh"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TENANT_SUBDOMAIN_TAKEN");
    }

    [Fact]
    public async Task Handle_WhenCodeTaken_ReturnsFailure()
    {
        var db = TestDbContextFactory.Create();
        db.Tenants.Add(new Tenant
        {
            Code = "PK001", Name = "Existing", Subdomain = "existing", Status = "ACTIVE"
        });
        await db.SaveChangesAsync();

        var handler = new CreateTenantCommandHandler(db, _emailSender, _passwordHasher, _config, _logger);
        var result = await handler.Handle(CreateCommand("PK001", "newsubdomain"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("TENANT_CODE_TAKEN");
    }

    [Fact]
    public async Task Handle_WhenValidData_ReturnsTenantResponse()
    {
        var db = TestDbContextFactory.Create();
        db.Roles.Add(new Role { Code = "admin", Name = "Administrator", RoleType = "SYSTEM" });
        await db.SaveChangesAsync();

        var handler = new CreateTenantCommandHandler(db, _emailSender, _passwordHasher, _config, _logger);
        var result = await handler.Handle(CreateCommand("PK001", "anbinh"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("PK001");
        result.Value.Subdomain.Should().Be("anbinh");

        await _emailSender.Received(1).SendAsync(
            "admin@anbinh.vn", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static CreateTenantCommand CreateCommand(string code, string subdomain) =>
        new(code, "Phong kham An Binh", null, null, null, null,
            "info@anbinh.vn", subdomain, 20, "admin@anbinh.vn", "Nguyen Van A", null);
}
