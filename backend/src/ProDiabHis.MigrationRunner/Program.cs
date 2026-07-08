using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace ProDiabHis.MigrationRunner;

/// <summary>
/// MigrationRunner cho mo hinh DB-per-tenant. Ap dung schema clean-slate (9xxx) len N tenant DB,
/// theo doi trong cat_schema_migrations (checksum) de chi chay migration con thieu.
///
/// Shell ra `mysql` client (khong dung driver) vi migration co DELIMITER/CREATE PROCEDURE
/// (pattern idempotent) — driver khong hieu DELIMITER.
///
/// Cach dung:
///   prodiab-migrate --catalog                 # ap dung schema control plane vao catalog DB
///   prodiab-migrate --tenant ABC              # ap dung schema tenant cho 1 tenant (doc db_name tu catalog)
///   prodiab-migrate --all-tenants             # ap dung cho moi tenant active
///   prodiab-migrate --new-db prodiab_t_abc --tenant-id 5   # provisioning DB moi
///   Them --dry-run de xem truoc, khong chay.
///
/// Ket noi admin (co quyen tren moi tenant DB):
///   --host --port --user --password  hoac  --conn "Server=...;Port=...;User Id=...;Password=..."
///   hoac bien moi truong CATALOG_CONNECTION. Catalog DB lay tu --catalog-db (mac dinh prodiab_catalog).
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var opts = Args.Parse(args);
            var admin = AdminConnection.Resolve(opts);
            var repoRoot = Locate.RepoRoot(opts.Get("migrations-dir"));
            var manifest = Manifest.Load(Path.Combine(repoRoot, "db", "migrations", "manifest.json"));

            var runner = new Runner(admin, repoRoot, manifest, opts.Has("dry-run"), opts.Get("mysql") ?? "mysql");

            if (opts.Has("catalog"))
                return await runner.ApplyCatalogAsync();

            if (opts.Has("new-db"))
            {
                var dbName = opts.Get("new-db")!;
                int? tenantId = int.TryParse(opts.Get("tenant-id"), out var tid) ? tid : null;
                return await runner.ProvisionNewDbAsync(dbName, tenantId);
            }

            if (opts.Has("tenant"))
                return await runner.ApplyTenantByCodeAsync(opts.Get("tenant")!);

            if (opts.Has("all-tenants"))
                return await runner.ApplyAllTenantsAsync();

            Console.Error.WriteLine("Thieu mode. Dung mot trong: --catalog | --tenant <code> | --all-tenants | --new-db <name>");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LOI] {ex.Message}");
            return 1;
        }
    }
}

/// <summary>Parse args dang --key value / --flag.</summary>
public sealed class Args
{
    private readonly Dictionary<string, string?> _map = new(StringComparer.OrdinalIgnoreCase);

    public static Args Parse(string[] args)
    {
        var a = new Args();
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) continue;
            var key = args[i][2..];
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : null;
            a._map[key] = val;
        }
        return a;
    }

    public bool Has(string key) => _map.ContainsKey(key);
    public string? Get(string key) => _map.TryGetValue(key, out var v) ? v : null;
}

/// <summary>Thong tin ket noi admin (dung cho ca query catalog lan invoke mysql client).</summary>
public sealed record AdminConnection(string Host, uint Port, string User, string Password, string CatalogDb)
{
    public string CatalogConnectionString =>
        new MySqlConnectionStringBuilder
        {
            Server = Host, Port = Port, UserID = User, Password = Password, Database = CatalogDb,
        }.ConnectionString;

    public static AdminConnection Resolve(Args opts)
    {
        var conn = opts.Get("conn") ?? Environment.GetEnvironmentVariable("CATALOG_CONNECTION");
        string host; uint port; string user; string pass;
        if (!string.IsNullOrEmpty(conn))
        {
            var b = new MySqlConnectionStringBuilder(conn);
            host = b.Server; port = b.Port == 0 ? 3306 : b.Port; user = b.UserID; pass = b.Password;
        }
        else
        {
            host = opts.Get("host") ?? "localhost";
            port = uint.TryParse(opts.Get("port"), out var p) ? p : 3306u;
            user = opts.Get("user") ?? "root";
            pass = opts.Get("password") ?? Environment.GetEnvironmentVariable("MYSQL_PWD") ?? "";
        }
        var catalogDb = opts.Get("catalog-db") ?? "prodiab_catalog";
        return new AdminConnection(host, port, user, pass, catalogDb);
    }
}

public sealed class Manifest
{
    public required string TenantIncludeGlob { get; init; }
    public required string TenantDir { get; init; }
    public required HashSet<string> ExcludeForTenant { get; init; }
    public required string CatalogDir { get; init; }
    public required string CatalogIncludeGlob { get; init; }

    public static Manifest Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var ts = root.GetProperty("tenantSchema");
        var cat = root.GetProperty("catalog");
        var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ts.TryGetProperty("excludeForTenant", out var ex))
            foreach (var e in ex.EnumerateArray()) excludes.Add(e.GetString()!);
        return new Manifest
        {
            TenantDir = ts.GetProperty("directory").GetString() ?? ".",
            TenantIncludeGlob = ts.GetProperty("include").GetString() ?? "9*.sql",
            ExcludeForTenant = excludes,
            CatalogDir = cat.GetProperty("directory").GetString() ?? "../catalog",
            CatalogIncludeGlob = cat.GetProperty("include").GetString() ?? "*.sql",
        };
    }
}

public static class Locate
{
    /// <summary>Tim root repo (chua thu muc db/migrations) tu cwd len tren, hoac dung --migrations-dir.</summary>
    public static string RepoRoot(string? overrideDir)
    {
        if (!string.IsNullOrEmpty(overrideDir))
            return Path.GetFullPath(Path.Combine(overrideDir, "..", ".."));
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "db", "migrations"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Khong tim thay db/migrations tu thu muc hien tai. Dung --migrations-dir.");
    }
}

public sealed class Runner
{
    private readonly AdminConnection _admin;
    private readonly string _repoRoot;
    private readonly Manifest _manifest;
    private readonly bool _dryRun;
    private readonly string _mysqlPath;

    public Runner(AdminConnection admin, string repoRoot, Manifest manifest, bool dryRun, string mysqlPath)
    {
        _admin = admin;
        _repoRoot = repoRoot;
        _manifest = manifest;
        _dryRun = dryRun;
        _mysqlPath = mysqlPath;
    }

    private string MigrationsDir => Path.Combine(_repoRoot, "db", "migrations");

    private List<string> TenantFiles()
    {
        var dir = Path.GetFullPath(Path.Combine(MigrationsDir, _manifest.TenantDir));
        return Directory.GetFiles(dir, _manifest.TenantIncludeGlob)
            .Select(Path.GetFullPath)
            .Where(f => !_manifest.ExcludeForTenant.Contains(Path.GetFileName(f)))
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();
    }

    private List<string> CatalogFiles()
    {
        var dir = Path.GetFullPath(Path.Combine(MigrationsDir, _manifest.CatalogDir));
        return Directory.GetFiles(dir, _manifest.CatalogIncludeGlob)
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();
    }

    public async Task<int> ApplyCatalogAsync()
    {
        Console.WriteLine($"== Catalog schema -> {_admin.CatalogDb} ==");
        await EnsureDatabaseAsync(_admin.CatalogDb);
        foreach (var f in CatalogFiles())
        {
            Console.WriteLine($"  apply {Path.GetFileName(f)}");
            if (!_dryRun) await RunSqlFileAsync(_admin.CatalogDb, f);
        }
        Console.WriteLine("== Xong catalog ==");
        return 0;
    }

    public async Task<int> ProvisionNewDbAsync(string dbName, int? tenantId)
    {
        Console.WriteLine($"== Provision DB moi: {dbName} (tenant_id={tenantId?.ToString() ?? "-"}) ==");
        await EnsureDatabaseAsync(dbName);
        return await ApplyTenantSchemaAsync(dbName, tenantId);
    }

    public async Task<int> ApplyTenantByCodeAsync(string code)
    {
        var t = await GetTenantByCodeAsync(code);
        if (t is null) { Console.Error.WriteLine($"Khong tim thay tenant '{code}' trong catalog"); return 2; }
        return await ApplyTenantSchemaAsync(t.Value.DbName, t.Value.TenantId);
    }

    public async Task<int> ApplyAllTenantsAsync()
    {
        var tenants = await GetAllActiveTenantsAsync();
        Console.WriteLine($"== Ap dung cho {tenants.Count} tenant active ==");
        var failed = 0;
        foreach (var t in tenants)
        {
            try { await ApplyTenantSchemaAsync(t.DbName, t.TenantId); }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"  [FAIL] tenant {t.TenantId} ({t.DbName}): {ex.Message}");
            }
        }
        Console.WriteLine($"== Hoan tat: {tenants.Count - failed} OK, {failed} loi ==");
        return failed == 0 ? 0 : 1;
    }

    private async Task<int> ApplyTenantSchemaAsync(string dbName, int? tenantId)
    {
        var applied = tenantId.HasValue ? await GetAppliedAsync(tenantId.Value) : new Dictionary<string, string>();
        var files = TenantFiles();
        var count = 0;
        foreach (var f in files)
        {
            var name = Path.GetFileName(f);
            var checksum = Sha256(f);
            if (applied.TryGetValue(name, out var prev) && prev == checksum)
                continue; // da apply, khong doi -> skip
            Console.WriteLine($"  [{dbName}] apply {name}");
            if (!_dryRun)
            {
                await RunSqlFileAsync(dbName, f);
                if (tenantId.HasValue) await RecordAppliedAsync(tenantId.Value, name, checksum);
            }
            count++;
        }
        Console.WriteLine($"  [{dbName}] {count} migration moi ({files.Count - count} da co)");
        return 0;
    }

    // ── mysql client invocation ─────────────────────────────
    private async Task RunSqlFileAsync(string database, string sqlFile)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _mysqlPath,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add($"--host={_admin.Host}");
        psi.ArgumentList.Add($"--port={_admin.Port}");
        psi.ArgumentList.Add($"--user={_admin.User}");
        psi.ArgumentList.Add("--protocol=TCP");
        psi.ArgumentList.Add($"--database={database}");
        psi.Environment["MYSQL_PWD"] = _admin.Password; // tranh lo password tren command line

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Khong khoi dong duoc mysql client");
        var sql = await File.ReadAllTextAsync(sqlFile);
        await proc.StandardInput.WriteAsync(sql);
        proc.StandardInput.Close();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"mysql exit {proc.ExitCode} khi chay {Path.GetFileName(sqlFile)}: {stderr.Trim()}");
    }

    private async Task EnsureDatabaseAsync(string dbName)
    {
        if (_dryRun) { Console.WriteLine($"  (dry-run) CREATE DATABASE {dbName}"); return; }
        var noDb = new MySqlConnectionStringBuilder(_admin.CatalogConnectionString) { Database = "" }.ConnectionString;
        await using var conn = new MySqlConnection(noDb);
        await conn.ExecuteAsync(
            $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci");
    }

    // ── catalog tracking ────────────────────────────────────
    private async Task<Dictionary<string, string>> GetAppliedAsync(int tenantId)
    {
        await using var conn = new MySqlConnection(_admin.CatalogConnectionString);
        var rows = await conn.QueryAsync<(string MigrationFile, string Checksum)>(
            "SELECT migration_file AS MigrationFile, checksum AS Checksum FROM cat_schema_migrations WHERE tenant_id = @tenantId",
            new { tenantId });
        return rows.ToDictionary(r => r.MigrationFile, r => r.Checksum, StringComparer.OrdinalIgnoreCase);
    }

    private async Task RecordAppliedAsync(int tenantId, string file, string checksum)
    {
        await using var conn = new MySqlConnection(_admin.CatalogConnectionString);
        await conn.ExecuteAsync(
            @"INSERT INTO cat_schema_migrations (tenant_id, migration_file, checksum, applied_at)
              VALUES (@tenantId, @file, @checksum, NOW())
              ON DUPLICATE KEY UPDATE checksum = @checksum, applied_at = NOW()",
            new { tenantId, file, checksum });
    }

    private async Task<(int TenantId, string DbName)?> GetTenantByCodeAsync(string code)
    {
        await using var conn = new MySqlConnection(_admin.CatalogConnectionString);
        var row = await conn.QueryFirstOrDefaultAsync<(int TenantId, string DbName)>(
            @"SELECT t.id AS TenantId, d.db_name AS DbName
              FROM cat_tenants t JOIN cat_tenant_databases d ON d.tenant_id = t.id
              WHERE t.code = @code AND t.deleted_at IS NULL", new { code });
        return row.TenantId == 0 ? null : row;
    }

    private async Task<List<(int TenantId, string DbName)>> GetAllActiveTenantsAsync()
    {
        await using var conn = new MySqlConnection(_admin.CatalogConnectionString);
        var rows = await conn.QueryAsync<(int TenantId, string DbName)>(
            @"SELECT t.id AS TenantId, d.db_name AS DbName
              FROM cat_tenants t JOIN cat_tenant_databases d ON d.tenant_id = t.id
              WHERE t.status = 'active' AND t.deleted_at IS NULL
              ORDER BY t.id");
        return rows.ToList();
    }

    private static string Sha256(string filePath)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(File.ReadAllBytes(filePath));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
