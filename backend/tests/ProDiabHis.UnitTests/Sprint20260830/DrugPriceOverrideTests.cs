using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy.Drugs;
using ProDiabHis.Domain.Entities;            // PriceOverrideScope
using ProDiabHis.Domain.Entities.Pharmacy;   // DrugBranchPrice
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC — override gia + an/hien THUOC theo chi nhanh (migration 9185). Mirror ServicePriceOverride.
/// Trong tam: guard quyen drug.price_override, BR-72 khong cho 2 override cung scope/thuoc GIAO NHAU,
/// va khac chi nhanh thi khong coi la trung.
/// Ghi chu: CreateHandler kiem tra thuoc ton tai bang Dapper (bang diab_his_pha_drugs chay Dapper),
///   khong test duoc tren InMemory EF -> Create happy/overlap/DRUG_NOT_FOUND verify qua DB that
///   (browser evidence). O day test FindOverlap qua duong Update (dung chung EF logic) + cac guard.
/// </summary>
public class DrugPriceOverrideTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _perm = Substitute.For<IPermissionChecker>();
    private readonly IDapperConnectionFactory _dapper = Substitute.For<IDapperConnectionFactory>();

    private const string DrugId = "11111111-1111-1111-1111-111111111111";

    public DrugPriceOverrideTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _perm.HasPermission("drug.price_override").Returns(true);
    }

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private CreateDrugPriceOverrideHandler CreateHandler(AppDbContext db)
        => new(db, _dapper, _tenant, _user, _perm);
    private UpdateDrugPriceOverrideHandler UpdateHandler(AppDbContext db)
        => new(db, _tenant, _user, _perm);
    private DeleteDrugPriceOverrideHandler DeleteHandler(AppDbContext db)
        => new(db, _tenant, _user, _perm);

    private static DrugBranchPrice Seed(AppDbContext db, DateOnly from, DateOnly? to,
        int branchId = 1, decimal price = 12_000m, bool isActive = true)
    {
        var e = new DrugBranchPrice
        {
            TenantId = 1, DrugId = DrugId, Scope = PriceOverrideScope.Branch,
            BranchId = branchId, Price = price, IsActive = isActive,
            EffectiveFrom = from, EffectiveTo = to
        };
        db.DrugBranchPrices.Add(e);
        db.SaveChanges();
        return e;
    }

    // UTC-DPO-01 — Create khong co quyen -> FORBIDDEN (khong cham DB/Dapper)
    [Fact]
    public async Task Create_KhongCoQuyen_TraVe_FORBIDDEN()
    {
        _perm.HasPermission("drug.price_override").Returns(false);
        using var db = TestDbContextFactory.Create(tenantId: 1);

        var r = await CreateHandler(db).Handle(new CreateDrugPriceOverrideCommand(
            new CreateDrugPriceOverrideRequest(DrugId, PriceOverrideScope.Branch, 1, null, 12_000m, true,
                D(2026, 9, 1), null, null)), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("FORBIDDEN");
        db.DrugBranchPrices.Should().BeEmpty();
    }

    // UTC-DPO-02 — Update khong co quyen -> FORBIDDEN
    [Fact]
    public async Task Update_KhongCoQuyen_TraVe_FORBIDDEN()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var e = Seed(db, D(2026, 9, 1), D(2026, 12, 31));
        _perm.HasPermission("drug.price_override").Returns(false);

        var r = await UpdateHandler(db).Handle(new UpdateDrugPriceOverrideCommand(e.Id,
            new UpdateDrugPriceOverrideRequest(15_000m, true, D(2026, 9, 1), D(2026, 12, 31), null)),
            CancellationToken.None);

        r.ErrorCode.Should().Be("FORBIDDEN");
    }

    // UTC-DPO-03 — Update khoang giao nhau voi override khac cung thuoc/chi nhanh -> PRICE_OVERLAP
    [Fact]
    public async Task Update_KhoangHieuLucGiaoNhau_TraVe_PRICE_OVERLAP()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        Seed(db, D(2026, 9, 1), D(2026, 12, 31));                 // override A
        var b = Seed(db, D(2027, 1, 1), D(2027, 3, 31));          // override B (se sua)

        // sua B lui ve giao voi A
        var r = await UpdateHandler(db).Handle(new UpdateDrugPriceOverrideCommand(b.Id,
            new UpdateDrugPriceOverrideRequest(20_000m, true, D(2026, 12, 1), D(2027, 2, 1), null)),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("PRICE_OVERLAP");
    }

    // UTC-DPO-04 — Update sang khoang khong giao -> thanh cong, cap nhat gia + is_active
    [Fact]
    public async Task Update_KhongGiao_DoiGiaVaAnHien_ThanhCong()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var e = Seed(db, D(2026, 9, 1), D(2026, 12, 31), price: 12_000m, isActive: true);

        var r = await UpdateHandler(db).Handle(new UpdateDrugPriceOverrideCommand(e.Id,
            new UpdateDrugPriceOverrideRequest(18_000m, false, D(2026, 9, 1), D(2026, 12, 31), "An o chi nhanh 1")),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Price.Should().Be(18_000m);
        r.Value.IsActive.Should().BeFalse();
    }

    // UTC-DPO-05 — Update: override khac chi nhanh khong coi la trung du cung khoang thoi gian
    [Fact]
    public async Task Update_KhacChiNhanh_KhongTrung()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        Seed(db, D(2026, 9, 1), D(2026, 12, 31), branchId: 1);
        var b = Seed(db, D(2027, 1, 1), D(2027, 3, 31), branchId: 2);

        // sua B (branch 2) sang cung khoang voi A (branch 1) -> khong trung vi khac chi nhanh
        var r = await UpdateHandler(db).Handle(new UpdateDrugPriceOverrideCommand(b.Id,
            new UpdateDrugPriceOverrideRequest(20_000m, true, D(2026, 9, 1), D(2026, 12, 31), null)),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
    }

    // UTC-DPO-06 — Delete (soft) thanh cong
    [Fact]
    public async Task Delete_ThanhCong_SoftDelete()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var e = Seed(db, D(2026, 9, 1), null);

        var r = await DeleteHandler(db).Handle(new DeleteDrugPriceOverrideCommand(e.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        db.DrugBranchPrices.Single(x => x.Id == e.Id).DeletedAt.Should().NotBeNull();
    }

    // UTC-DPO-07 — Delete khong ton tai -> PRICE_OVERRIDE_NOT_FOUND
    [Fact]
    public async Task Delete_KhongTonTai_TraVe_NOT_FOUND()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var r = await DeleteHandler(db).Handle(new DeleteDrugPriceOverrideCommand(Guid.NewGuid()), CancellationToken.None);
        r.ErrorCode.Should().Be("PRICE_OVERRIDE_NOT_FOUND");
    }
}
