using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-E3P-xx — E/Dot3 gia override 3 tang theo chi nhanh (BR-70..BR-76).
/// Trong tam: BR-72 khong cho 2 override cung scope/service co khoang hieu luc GIAO NHAU,
/// va guard quyen service.price_override.
/// </summary>
public class ServicePriceOverrideTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _perm = Substitute.For<IPermissionChecker>();

    public ServicePriceOverrideTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _perm.HasPermission("service.price_override").Returns(true);
    }

    private static Guid SeedService(ProDiabHis.Infrastructure.Persistence.AppDbContext db)
    {
        var svc = new BillingService
        {
            TenantId = 1, Code = "KHAM-NOI", Name = "Khám nội tổng quát",
            Category = "KHAM", Price = 200_000m, IsActive = true
        };
        db.BillingServices.Add(svc);
        db.SaveChanges();
        return svc.Id;
    }

    private CreateServicePriceOverrideHandler Handler(ProDiabHis.Infrastructure.Persistence.AppDbContext db)
        => new(db, _tenant, _user, _perm);

    private static CreateServicePriceOverrideRequest Req(Guid serviceId, DateOnly from, DateOnly? to,
        decimal price = 250_000m, int branchId = 1)
        => new(serviceId, PriceOverrideScope.Branch, branchId, null, price, true, from, to, "Giá áp dụng chi nhánh 1");

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // UTC-E3P-01 — khong co quyen -> FORBIDDEN, khong ghi DB
    [Fact]
    public async Task Create_KhongCoQuyen_TraVe_FORBIDDEN()
    {
        _perm.HasPermission("service.price_override").Returns(false);
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31))),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("FORBIDDEN");
        db.ServiceBranchPrices.Should().BeEmpty();
    }

    // UTC-E3P-02 — dich vu khong ton tai -> SERVICE_NOT_FOUND
    [Fact]
    public async Task Create_DichVuKhongTonTai_TraVe_SERVICE_NOT_FOUND()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(Guid.NewGuid(), D(2026, 9, 1), null)),
            CancellationToken.None);

        r.ErrorCode.Should().Be("SERVICE_NOT_FOUND");
    }

    // UTC-E3P-03 — HAPPY: tao override dau tien thanh cong
    [Fact]
    public async Task Create_LanDau_ThanhCong()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31))),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Price.Should().Be(250_000m);
        r.Value.Scope.Should().Be(PriceOverrideScope.Branch);
        r.Value.BranchId.Should().Be(1);
        r.Value.GroupId.Should().BeNull();
    }

    // UTC-E3P-04 — BR-72: khoang thoi gian giao nhau -> PRICE_OVERLAP
    [Fact]
    public async Task Create_KhoangHieuLucGiaoNhau_TraVe_PRICE_OVERLAP()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);
        await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31))), CancellationToken.None);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 12, 1), D(2027, 3, 31), 300_000m)),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("PRICE_OVERLAP");
    }

    // UTC-E3P-05 — BIEN: khoang ke tiep, khong giao (bat dau dung ngay sau ngay ket thuc) -> cho phep
    [Fact]
    public async Task Create_KhoangKeTiepKhongGiao_ThanhCong()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);
        await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31))), CancellationToken.None);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2027, 1, 1), D(2027, 3, 31), 300_000m)),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
    }

    // UTC-E3P-06 — BIEN: giao dung 1 ngay (from moi == to cu) -> PHAI bi chan
    [Fact]
    public async Task Create_GiaoDungMotNgay_VanBiChan()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);
        await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31))), CancellationToken.None);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 12, 31), D(2027, 3, 31), 300_000m)),
            CancellationToken.None);

        r.ErrorCode.Should().Be("PRICE_OVERLAP");
    }

    // UTC-E3P-07 — override khong co ngay ket thuc (vo han) chan moi khoang sau do
    [Fact]
    public async Task Create_OverrideVoHan_ChanMoiKhoangSauDo()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);
        await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 1, 1), null)), CancellationToken.None);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2030, 1, 1), null, 999_000m)),
            CancellationToken.None);

        r.ErrorCode.Should().Be("PRICE_OVERLAP");
    }

    // UTC-E3P-08 — khac chi nhanh thi khong coi la trung, du cung khoang thoi gian
    [Fact]
    public async Task Create_KhacChiNhanh_CungKhoangThoiGian_ThanhCong()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);
        await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31), 250_000m, branchId: 1)),
            CancellationToken.None);

        var r = await Handler(db).Handle(
            new CreateServicePriceOverrideCommand(Req(svcId, D(2026, 9, 1), D(2026, 12, 31), 280_000m, branchId: 2)),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.BranchId.Should().Be(2);
    }

    // UTC-E3P-09 — scope GROUP thi BranchId phai duoc bo qua (khong luu lan)
    [Fact]
    public async Task Create_ScopeGroup_KhongLuuBranchId()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var svcId = SeedService(db);

        var r = await Handler(db).Handle(new CreateServicePriceOverrideCommand(
                new CreateServicePriceOverrideRequest(svcId, PriceOverrideScope.Group, 1, 5, 270_000m, true,
                    D(2026, 9, 1), null, "Giá theo cụm chi nhánh")),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.BranchId.Should().BeNull();
        r.Value.GroupId.Should().Be(5);
    }
}
