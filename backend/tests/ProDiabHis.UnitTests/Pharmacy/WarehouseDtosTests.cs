using FluentAssertions;
using ProDiabHis.Application.Pharmacy.Warehouse;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Regression test cho contract cua GrnItemRequest/StockAdjustmentItem/TransferItem sau khi fix
/// BUG Fix3 (WarehouseHandlers.cs: CreateGrnHandler/CreateAdjustmentHandler/CreateTransferHandler).
///
/// Truoc fix: DrugId trong 3 DTO nay la `int`, trong khi diab_his_pha_drugs.id /
/// diab_his_pha_stock.drug_id la CHAR(36) (GUID) co FK constraint (fk_stock_drug). Ghi mot gia tri
/// int (vd 5) vao cot drug_id se luon vi pham FOREIGN KEY constraint (khong co thuoc nao co id la
/// chuoi "5") -> 500 ngay ca sau khi da sua ten cot. OpenAPI spec (docs/api/openapi/
/// pharmacy-warehouse.yaml) cung dinh nghia drug_id: { type: string, format: uuid } cho ca 3
/// endpoint nay, xac nhan `string` moi la kieu dung.
///
/// Test nay chi la "compile-time contract guard": neu ai do vo tinh doi DrugId ve lai `int`,
/// file test se KHONG BIEN DICH (build error) — day la muc dich chinh, cac assertion o duoi chi de
/// xUnit khong bao "empty test".
/// </summary>
public class WarehouseDtosTests
{
    [Fact]
    public void GrnItemRequest_DrugId_IsGuidString_NotInt()
    {
        var drugId = Guid.NewGuid().ToString();
        var item = new GrnItemRequest(drugId, "LOT-001", DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddYears(2)), 100m, 1500m);

        item.DrugId.Should().Be(drugId);
        Guid.TryParse(item.DrugId, out _).Should().BeTrue("DrugId phai la GUID hop le, khop diab_his_pha_drugs.id CHAR(36)");
    }

    [Fact]
    public void StockAdjustmentItem_DrugId_IsGuidString_NotInt()
    {
        var drugId = Guid.NewGuid().ToString();
        var item = new StockAdjustmentItem(drugId, "LOT-001", -5m);

        item.DrugId.Should().Be(drugId);
        Guid.TryParse(item.DrugId, out _).Should().BeTrue();
    }

    [Fact]
    public void TransferItem_DrugId_IsGuidString_NotInt()
    {
        var drugId = Guid.NewGuid().ToString();
        var item = new TransferItem(drugId, "LOT-001", 10m);

        item.DrugId.Should().Be(drugId);
        Guid.TryParse(item.DrugId, out _).Should().BeTrue();
    }

    [Fact]
    public void StockResponse_DrugId_IsString_MatchesRealSchema()
    {
        // diab_his_pha_stock.drug_id la CHAR(36) — StockResponse.DrugId phai la string de map dung.
        var drugId = Guid.NewGuid().ToString();
        var stock = new StockResponse(
            Guid.NewGuid().ToString(), 1, 1, drugId, "Paracetamol 500mg", "LOT-001",
            DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            100m, 0m, 1500m, 365, false, false);

        stock.DrugId.Should().Be(drugId);
    }
}
