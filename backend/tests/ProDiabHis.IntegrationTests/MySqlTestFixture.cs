using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Persistence;
using Testcontainers.MySql;
using Xunit;

namespace ProDiabHis.IntegrationTests;

/// <summary>Fixture khoi dong MySQL container cho integration test</summary>
public class MySqlTestFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0.36")
        .WithDatabase("prodiab_his_test")
        .WithUsername("test")
        .WithPassword("test_password")
        .Build();

    public AppDbContext DbContext { get; private set; } = null!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
            .Options;

        DbContext = new AppDbContext(options, new NoopTenantProvider());
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }

    private class NoopTenantProvider : ITenantProvider
    {
        public int TenantId => 0;
        public void SetTenantId(int tenantId) { }
    }
}
