using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Application.Pharmacy.Warehouse;
using ProDiabHis.Infrastructure.Pharmacy;
using Testcontainers.MySql;
using Xunit;

namespace ProDiabHis.IntegrationTests.Pharmacy;

/// <summary>
/// Write-path kho duoc phai ghi vao bo bang ACTIVE diab_his_pha_stock / diab_his_pha_stock_movements
/// (GUID) de khop read-path ton kho: GRN khong PO, dieu chinh, va FEFO (dung strategy cua dispense).
/// </summary>
public class WarehouseWritePathIntegrationTests : IAsyncLifetime
{
    private const int TenantId = 1;
    private const string DrugId = "11111111-1111-1111-1111-111111111111";

    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0.36").WithDatabase("prodiab_wh_test").WithUsername("test").WithPassword("test_password").Build();

    private Factory _factory = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _factory = new Factory(_container.GetConnectionString());
        using var c = new MySqlConnection(_container.GetConnectionString());
        await c.ExecuteAsync(@"
CREATE TABLE diab_his_pha_drugs (id CHAR(36) PRIMARY KEY, tenant_id INT NOT NULL, code VARCHAR(50), name VARCHAR(200), name_vi VARCHAR(200));
CREATE TABLE diab_his_pha_stock (id CHAR(36) PRIMARY KEY, tenant_id INT NOT NULL, branch_id INT NULL, drug_id CHAR(36) NOT NULL,
  lot_number VARCHAR(50) NOT NULL, mfg_date DATE NULL, exp_date DATE NOT NULL, quantity INT NOT NULL DEFAULT 0,
  import_price DECIMAL(12,2) NOT NULL DEFAULT 0, created_at DATETIME DEFAULT CURRENT_TIMESTAMP, updated_at DATETIME DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE diab_his_pha_stock_movements (id INT AUTO_INCREMENT PRIMARY KEY, tenant_id INT, stock_id CHAR(36) NOT NULL,
  warehouse_id VARCHAR(36) NOT NULL, movement_type VARCHAR(20) NOT NULL, quantity DECIMAL(12,3) NOT NULL, unit_price DECIMAL(15,2) NULL,
  reason TEXT NULL, reference_type VARCHAR(50) NULL, reference_id VARCHAR(36) NULL, movement_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  performed_by VARCHAR(36) NULL, created_at DATETIME DEFAULT CURRENT_TIMESTAMP, updated_at DATETIME DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE diab_his_pha_grn (id CHAR(36) PRIMARY KEY, tenant_id INT NOT NULL, purchase_order_id CHAR(36) NULL,
  received_at DATETIME NOT NULL, note TEXT NULL, created_at DATETIME DEFAULT CURRENT_TIMESTAMP, updated_at DATETIME DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE diab_his_pha_purchase_orders (id CHAR(36) PRIMARY KEY, tenant_id INT, status VARCHAR(20), updated_at DATETIME);
INSERT INTO diab_his_pha_drugs VALUES ('" + DrugId + "', 1, 'T001', 'Metformin', 'Metformin 500mg');");
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private Task<Result<GrnResponse>> Grn(string batch, string expiry, decimal qty, string? warehouseId = "1") =>
        new CreateGrnHandler(_factory, new User(), new NoopCuc(), new Branch()).Handle(
            new CreateGrnCommand(null, new GoodsReceivedRequest(DateTime.UtcNow, "test",
                [new GrnItemRequest(DrugId, batch, null, DateOnly.Parse(expiry), qty, 1000m)], warehouseId)),
            CancellationToken.None);

    private async Task<int> Qty(string batch)
    {
        using var c = new MySqlConnection(_container.GetConnectionString());
        return await c.ExecuteScalarAsync<int>("SELECT COALESCE(SUM(quantity),0) FROM diab_his_pha_stock WHERE drug_id=@d AND lot_number=@b", new { d = DrugId, b = batch });
    }

    [DockerAvailableFact]
    public async Task GrnKhongPo_TaoLoMoi_RoiCongDonKhiNhapLaiCungLo_VaGhiMovementImport()
    {
        var r1 = await Grn("LOT-A", "2030-01-01", 100);
        r1.IsSuccess.Should().BeTrue(r1.ErrorMessage);
        r1.Value!.PoStatus.Should().Be("NONE");
        r1.Value.StocksUpdated.Single().DrugId.Should().Be(DrugId);
        (await Qty("LOT-A")).Should().Be(100);

        (await Grn("LOT-A", "2030-01-01", 50)).IsSuccess.Should().BeTrue();
        (await Qty("LOT-A")).Should().Be(150);

        using var c = new MySqlConnection(_container.GetConnectionString());
        (await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM diab_his_pha_stock_movements WHERE movement_type='IMPORT'")).Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task GrnKhongPo_ThieuKho_TraLoiValidation_VaKhongGhiGi()
    {
        var r = await Grn("LOT-X", "2030-01-01", 10, warehouseId: null);
        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("VALIDATION_ERROR");
        (await Qty("LOT-X")).Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task DieuChinh_DoiTonDungLo_VaKhongChoAmTon()
    {
        await Grn("LOT-B", "2030-01-01", 20);
        var h = new CreateAdjustmentHandler(_factory, new User());

        var ok = await h.Handle(new CreateAdjustmentCommand(new StockAdjustmentRequest(1, "KIEM_KE", "n",
            [new StockAdjustmentItem(DrugId, "LOT-B", -5)])), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue(ok.ErrorMessage);
        (await Qty("LOT-B")).Should().Be(15);

        var bad = await h.Handle(new CreateAdjustmentCommand(new StockAdjustmentRequest(1, "KIEM_KE", "n",
            [new StockAdjustmentItem(DrugId, "LOT-B", -999)])), CancellationToken.None);
        bad.IsSuccess.Should().BeFalse();
        (await Qty("LOT-B")).Should().Be(15);
    }

    [DockerAvailableFact]
    public async Task Fefo_PhatThuocUuTienLoHetHanSom_TrenBangActive()
    {
        await Grn("LOT-LATE", "2031-01-01", 100);
        await Grn("LOT-EARLY", "2029-01-01", 30);

        var picks = await new FefoStrategyImpl(_factory, NullLogger<FefoStrategyImpl>.Instance)
            .PickAsync("1", TenantId, DrugId, 40);

        picks.Select(p => p.BatchNo).Should().Equal("LOT-EARLY", "LOT-LATE");
        picks[0].Quantity.Should().Be(30);
        picks[1].Quantity.Should().Be(10);
    }

    private sealed class Factory(string cs) : IDapperConnectionFactory
    {
        public System.Data.IDbConnection CreateConnection() => new MySqlConnection(cs);
    }

    private sealed class User : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public int? TenantId => 1;
        public string? Email => null;
        public IReadOnlyList<string> Roles => [];
        public IReadOnlyList<string> RoleCodes => [];
        public bool IsAuthenticated => true;
    }

    private sealed class Branch : IBranchProvider
    {
        public int BranchId => 0;
        public bool IgnoreBranchFilter => true;
        public IReadOnlyList<int> AllowedBranchIds => [];
        public void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds) { }
    }

    private sealed class NoopCuc : ICucQldLienThong
    {
        public Task ReportImportAsync(Guid grnId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ReportExportAsync(Guid dispenseRecordId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
