using FluentAssertions;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>
/// Chung minh migration 9120_merge_dispense_records.sql hop nhat du lieu that tu 2 bang
/// cap phat thuoc (diab_his_pha_dispense_records + diab_his_pha_dispenses) tren MySQL that
/// (Testcontainers), co xu ly trung lap va chay lai duoc nhieu lan khong nhan doi.
///
/// Bang chuan (canonical) sau khi hop nhat: diab_his_pha_dispense_records — day la bang duy
/// nhat con duoc code nghiep vu song (DispensingHandlers, BillingCalculatorImpl) doc/ghi;
/// diab_his_pha_dispenses chi con DbSet EF chet (da go khoi AppDbContext), khong bi DROP,
/// giu nguyen lam ban sao an toan.
///
/// LUU Y: test nay KHONG dung MySqlTestFixture/AppDbContext.EnsureCreatedAsync() vi EF model
/// hien tai co bug rieng — DispenseItemConfiguration.DrugId van khai INT trong khi migration
/// 9025_fix_dispense_fk_types.sql da doi cot that trong DB production sang CHAR(36) UUID (khop
/// voi diab_his_pha_drugs.id). EnsureCreated se dung theo EF model (sai) chu khong theo migration
/// that (dung) -> test se dung sai schema neu dung chung fixture. Test nay tu dung container
/// MySQL rieng va tao bang bang DDL khop CHINH XAC voi cac file migration that (0038, 9005,
/// 9025) de mo phong dung trang thai schema production.
/// </summary>
public class DispenseMergeIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0.36")
        .WithDatabase("prodiab_his_test_9120")
        .WithUsername("test")
        .WithPassword("test_password")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    // Schema khop dung migration 0038 (dispense_records/items, da fix kieu boi 9025) +
    // 9005 (dispenses).
    private const string CreateSchemaSql = """
        CREATE TABLE IF NOT EXISTS diab_his_pha_dispense_records (
            id CHAR(36) NOT NULL DEFAULT (UUID()),
            tenant_id INT NOT NULL,
            prescription_id CHAR(36) NOT NULL,
            warehouse_id VARCHAR(36) NOT NULL,
            dispensed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            dispensed_by INT NULL,
            status ENUM('DISPENSED','REJECTED','RETURNED','PARTIAL') NOT NULL DEFAULT 'DISPENSED',
            note TEXT NULL,
            total_amount DECIMAL(15,2) NOT NULL DEFAULT 0,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            created_by INT NULL,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            updated_by INT NULL,
            deleted_at DATETIME NULL,
            deleted_by INT NULL,
            PRIMARY KEY (id),
            UNIQUE KEY uk_dispense_prescription (prescription_id, tenant_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

        CREATE TABLE IF NOT EXISTS diab_his_pha_dispense_items (
            id CHAR(36) NOT NULL DEFAULT (UUID()),
            tenant_id INT NOT NULL,
            dispense_record_id CHAR(36) NOT NULL,
            prescription_item_id CHAR(36) NOT NULL,
            drug_id CHAR(36) NOT NULL,
            batch_no VARCHAR(50) NOT NULL,
            expiry_date DATE NOT NULL,
            quantity DECIMAL(10,2) NOT NULL,
            unit_cost DECIMAL(15,2) NOT NULL DEFAULT 0,
            is_returned TINYINT(1) NOT NULL DEFAULT 0,
            returned_quantity DECIMAL(10,2) NOT NULL DEFAULT 0,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            deleted_at DATETIME NULL,
            PRIMARY KEY (id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

        CREATE TABLE IF NOT EXISTS diab_his_pha_dispenses (
            id CHAR(36) NOT NULL,
            tenant_id INT NOT NULL,
            prescription_id CHAR(36) NOT NULL,
            dispensed_by CHAR(36) NOT NULL,
            dispensed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            items_json JSON NOT NULL,
            note TEXT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            created_by CHAR(36) NULL,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            PRIMARY KEY (id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """;

    private static readonly string MigrationSql = File.ReadAllText(
        FindMigrationFile("9120_merge_dispense_records.sql"));

    private static string FindMigrationFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "db", "migrations")))
            dir = dir.Parent;

        if (dir == null)
            throw new FileNotFoundException($"Khong tim thay thu muc db/migrations tu {AppContext.BaseDirectory}");

        return Path.Combine(dir.FullName, "db", "migrations", fileName);
    }

    [DockerAvailableFact]
    public async Task Migration_HopNhat_DuLieuThat_KhongNhanDoi_KhiChayLaiNhieuLan()
    {
        await using var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync();

        await ExecuteBatchAsync(conn, CreateSchemaSql);
        await SeedAsync(conn);

        var beforeDr = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispense_records");
        var beforeD = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispenses");
        beforeDr.Should().Be(12, "du lieu that tren production: dispense_records co 12 dong");
        beforeD.Should().Be(15, "du lieu that tren production: dispenses co 15 dong");

        // Chay migration LAN 1
        await ExecuteBatchAsync(conn, MigrationSql);

        var afterRun1Dr = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispense_records");
        var afterRun1D = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispenses");
        var afterRun1Items = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispense_items");

        // 12 dong goc + 12 dong moi (3 dong trung prescription_id bi loai, khong nhan doi)
        afterRun1Dr.Should().Be(24, "12 dong goc + 12 dong khong trung tu bang phu (3 dong trung bi loai)");
        afterRun1D.Should().Be(15, "bang phu KHONG bi xoa/drop, van giu nguyen du lieu goc");
        afterRun1Items.Should().Be(12, "moi header moi chep sang co 1 dong item tu items_json");

        // Chay migration LAN 2 — phai idempotent, khong nhan doi
        await ExecuteBatchAsync(conn, MigrationSql);

        var afterRun2Dr = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispense_records");
        var afterRun2D = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispenses");
        var afterRun2Items = await ScalarAsync(conn, "SELECT COUNT(*) FROM diab_his_pha_dispense_items");

        afterRun2Dr.Should().Be(afterRun1Dr, "chay migration lan 2 khong duoc nhan doi du lieu header");
        afterRun2D.Should().Be(afterRun1D, "bang phu van giu nguyen sau nhieu lan chay");
        afterRun2Items.Should().Be(afterRun1Items, "chay migration lan 2 khong duoc nhan doi du lieu item");
    }

    private static async Task SeedAsync(MySqlConnection conn)
    {
        // 12 dong o dispense_records (tenant 1)
        for (var i = 1; i <= 12; i++)
        {
            await using var cmd = new MySqlCommand(
                """
                INSERT INTO diab_his_pha_dispense_records
                    (id, tenant_id, prescription_id, warehouse_id, dispensed_at, dispensed_by, status, note, total_amount, created_at, updated_at)
                VALUES
                    (UUID(), 1, @presId, 'default', NOW(), 0, 'DISPENSED', @note, 100000, NOW(), NOW())
                """, conn);
            cmd.Parameters.AddWithValue("@presId", $"11111111-1111-1111-1111-111111111{i:D3}");
            cmd.Parameters.AddWithValue("@note", $"don {i}");
            await cmd.ExecuteNonQueryAsync();
        }

        // 3 dong trung prescription_id voi dispense_records (dedup phai loai)
        for (var i = 1; i <= 3; i++)
        {
            await using var cmd = new MySqlCommand(
                """
                INSERT INTO diab_his_pha_dispenses (id, tenant_id, prescription_id, dispensed_by, dispensed_at, items_json, note, created_at, updated_at)
                VALUES (UUID(), 1, @presId, '22222222-2222-2222-2222-222222222222', NOW(),
                    JSON_ARRAY(JSON_OBJECT('drug_id','33333333-3333-3333-3333-333333333333','batch_no','LOT-A','qty',2)),
                    @note, NOW(), NOW())
                """, conn);
            cmd.Parameters.AddWithValue("@presId", $"11111111-1111-1111-1111-111111111{i:D3}");
            cmd.Parameters.AddWithValue("@note", $"legacy trung don {i}");
            await cmd.ExecuteNonQueryAsync();
        }

        // 12 dong rieng chi co o dispenses (du lieu mo coi can duoc chep sang)
        for (var i = 1; i <= 12; i++)
        {
            await using var cmd = new MySqlCommand(
                """
                INSERT INTO diab_his_pha_dispenses (id, tenant_id, prescription_id, dispensed_by, dispensed_at, items_json, note, created_at, updated_at)
                VALUES (UUID(), 1, @presId, '22222222-2222-2222-2222-222222222222', NOW(),
                    JSON_ARRAY(JSON_OBJECT('drug_id','44444444-4444-4444-4444-444444444444','batch_no','LOT-B','qty',5)),
                    @note, NOW(), NOW())
                """, conn);
            cmd.Parameters.AddWithValue("@presId", $"99999999-9999-9999-9999-999999999{i:D3}");
            cmd.Parameters.AddWithValue("@note", $"legacy rieng {i}");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<long> ScalarAsync(MySqlConnection conn, string sql)
    {
        await using var cmd = new MySqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <summary>Chay tung statement rieng (tach theo dau ";" sau khi loai bo cac dong comment
    /// "-- ..."). KHONG dung cho script co DELIMITER $$ vi migration 9120 khong dung stored
    /// procedure nen an toan de tach don gian theo dau ";".</summary>
    private static async Task ExecuteBatchAsync(MySqlConnection conn, string sql)
    {
        var withoutComments = string.Join('\n', sql
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("--")));

        var statements = withoutComments
            .Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        foreach (var statement in statements)
        {
            await using var cmd = new MySqlCommand(statement, conn) { CommandTimeout = 120 };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
