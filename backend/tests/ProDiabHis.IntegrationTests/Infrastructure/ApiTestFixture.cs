using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Infrastructure.Persistence;
using Testcontainers.MySql;
using Xunit;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture dung chung cho TOAN BO integration test HTTP:
///  1. Khoi dong 1 container MySQL 8 that (env parity voi prod — khong dung SQLite/InMemory)
///  2. Tao schema tu EF model (EnsureCreated) — cac entity deu ToTable(...) dung ten bang that
///     nen ca EF lan Dapper raw SQL doc duoc
///  3. Boot API that bang WebApplicationFactory&lt;Program&gt; — request di qua DUNG pipeline
///     production: JwtBearer -> TenantScope -> BranchScope -> Authorization -> Controller -> MediatR
///
/// Container + host duoc chia se cho ca collection "Api" nen chi khoi dong 1 lan cho ca test run.
/// </summary>
public class ApiTestFixture : IAsyncLifetime
{
    private MySqlContainer? _container;
    private WebApplicationFactory<Program>? _factory;

    /// <summary>Connection string toi MySQL container (null neu khong co Docker).</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>HttpClient goi API that. Chi hop le khi Docker san sang.</summary>
    public HttpClient Client => _factory?.CreateClient()
        ?? throw new InvalidOperationException("Test host chua khoi dong (thieu Docker).");

    public WebApplicationFactory<Program> Factory => _factory
        ?? throw new InvalidOperationException("Test host chua khoi dong (thieu Docker).");

    /// <summary>Tao HttpClient da gan san Bearer token.</summary>
    public HttpClient ClientWithToken(string token)
    {
        var client = Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Client voi quyen super admin — dung cho case happy path khong quan tam phan quyen.</summary>
    public HttpClient AdminClient(int tenantId = 1)
        => ClientWithToken(TestTokens.ForSuperAdmin(tenantId));

    /// <summary>Client chi co dung cac permission chi dinh — dung test phan quyen.</summary>
    public HttpClient ClientWith(params string[] permissions)
        => ClientWithToken(TestTokens.ForPermissions(1, Guid.NewGuid(), permissions));

    /// <summary>Client da dang nhap nhung KHONG co quyen nao — ky vong 403.</summary>
    public HttpClient ClientNoPermission()
        => ClientWithToken(TestTokens.WithNoPermission());

    /// <summary>Client chua dang nhap — ky vong 401.</summary>
    public HttpClient AnonymousClient() => Client;

    /// <summary>Mo 1 DbContext moi tro toi DB test — dung de seed va de assert lop DB.</summary>
    public AppDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
            .Options;
        return new AppDbContext(options, new NoopTenantProvider(), new NoopBranchProvider());
    }

    public async Task InitializeAsync()
    {
        // Khong co Docker -> khong khoi dong gi ca; moi test dung [ApiFact] se tu Skip.
        if (!DockerProbe.IsAvailable) return;

        _container = new MySqlBuilder()
            .WithImage("mysql:8.0.36")
            .WithDatabase("prodiab_his_test")
            .WithUsername("root")
            .WithPassword("test_password")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString() + ";AllowUserVariables=true;UseAffectedRows=false;";

        // Tao schema truoc khi API boot (Hangfire se tu tao bang hangfire_* cua no).
        await using (var db = NewDbContext())
        {
            await db.Database.EnsureCreatedAsync();

            // EnsureCreated chi tao bang co entity EF. Nhieu bang CO THAT trong he thong lai
            // chi duoc tao boi db/migrations/*.sql (dict, reception queue, package, view legacy...)
            // va read-side Dapper doc thang vao do -> thieu se 500 "Table doesn't exist".
            // Ghi chu: KHONG the chay thang db/migrations vi APPLY_ORDER.md ghi nhan chuoi
            // migration hien CHUA dung duoc DB sach tu so 0 (30/150 file loi SQL that).
            // Vi vay nap DDL bo sung trich nguyen van tu migrations (xem TestSchemaSupplement).
            foreach (var stmt in TestSchemaSupplement.Statements)
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync(stmt);
                }
                catch (Exception ex)
                {
                    // Khong lam hong ca test run vi 1 cau DDL — in ra de con truy vet duoc.
                    Console.WriteLine($"[TestSchemaSupplement] BO QUA cau DDL loi: {ex.Message}");
                }
            }
        }

        // QUAN TRONG: Program.cs dung minimal hosting (WebApplication.CreateBuilder) va goi
        // AddInfrastructure(builder.Configuration) NGAY trong than Main — chay TRUOC khi callback
        // ConfigureAppConfiguration cua WebApplicationFactory duoc ap dung. Vi vay override config
        // qua BIEN MOI TRUONG (CreateBuilder doc env var ngay tu dau) moi an toan.
        var env = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__DefaultConnection"] = ConnectionString,
            // Redis rong -> Infrastructure bo qua Redis, IRateLimiter fallback in-memory
            ["ConnectionStrings__Redis"] = string.Empty,
            ["JWT__SECRET"] = TestTokens.Secret,
            ["Jwt__Secret"] = TestTokens.Secret,
            ["Jwt__Issuer"] = TestTokens.Issuer,
            ["Jwt__Audience"] = TestTokens.Audience,
            // Khoa PII test (base64 32 byte) — bat blind index de tra cuu CCCD/SDT chay that
            ["Encryption__MasterKey"] = "6/RnabdldwkoYFKx0gW9iJA4nJujUgBF0HTnbbB/8Zk=",
            ["Encryption__BlindIndexKey"] = "gMUh+wYPgtiEJp1ABrBK6ThSE2gbxYmbgB4qM234Tvs=",
            ["Security__BCryptWorkFactor"] = "4", // nhanh hon cho test
            ["Sentry__Dsn"] = string.Empty,
            ["SignatureProvider__Type"] = "Mock",
            ["Serilog__MinimumLevel__Default"] = "Warning"
        };
        foreach (var (k, v) in env) Environment.SetEnvironmentVariable(k, v);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureTestServices(services =>
                {
                    // Chi thay HA TANG, khong thay logic nghiep vu:
                    // rate limit that la 100 req/phut/user — hang tram IT chay lien tuc se dinh 429
                    // gia (khong phai bug san pham). Thay bang limiter cho phep tat ca.
                    services.RemoveAll<IRateLimiter>();
                    services.AddSingleton<IRateLimiter, AlwaysAllowRateLimiter>();
                });
            });

        // Ep host khoi tao ngay de loi boot lo ra o day (thay vi o test dau tien).
        _ = _factory.Services.GetRequiredService<IConfiguration>();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }

    private sealed class AlwaysAllowRateLimiter : IRateLimiter
    {
        public Task<bool> AllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<long> GetCountAsync(string key, TimeSpan window, CancellationToken ct = default)
            => Task.FromResult(0L);
    }

    private sealed class NoopTenantProvider : ITenantProvider
    {
        public int TenantId => 0;
        public void SetTenantId(int tenantId) { }
    }

    private sealed class NoopBranchProvider : IBranchProvider
    {
        public int BranchId => 0;
        public bool IgnoreBranchFilter => true;
        public IReadOnlyList<int> AllowedBranchIds => Array.Empty<int>();
        public void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds) { }
    }
}

/// <summary>Collection dung chung — dam bao chi 1 container + 1 API host cho ca test run.</summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiTestFixture> { }
