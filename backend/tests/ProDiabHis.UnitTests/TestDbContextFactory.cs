using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Persistence;

namespace ProDiabHis.UnitTests;

/// <summary>Factory tao AppDbContext InMemory cho unit test</summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null, int tenantId = 1)
    {
        var tenantProvider = new FakeTenantProvider(tenantId);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, tenantProvider);
    }
}

/// <summary>ITenantProvider gia lap cho unit test — tra ve TenantId = 0 (khong filter)</summary>
public class FakeTenantProvider : ITenantProvider
{
    public FakeTenantProvider(int tenantId) => TenantId = tenantId;
    public int TenantId { get; private set; }
    public void SetTenantId(int tenantId) => TenantId = tenantId;
}
