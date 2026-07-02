using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Persistence;

/// <summary>Factory cho EF Core CLI migrations (design-time)</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "Server=localhost;Port=3306;Database=prodiab_his;User=prodiab;Password=prodiab_dev_2026;CharSet=utf8mb4;";

        // Dung hardcoded version de tranh ket noi thuc su khi chay EF CLI (AutoDetect can live connection)
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        optionsBuilder.UseMySql(connectionString, serverVersion);

        // Dung NoopTenantProvider cho design-time (migration)
        return new AppDbContext(optionsBuilder.Options, new NoopTenantProvider());
    }

    private class NoopTenantProvider : ITenantProvider
    {
        public int TenantId => 0;
        public void SetTenantId(int tenantId) { }
    }
}
