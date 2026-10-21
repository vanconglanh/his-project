using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>BM-95: validator voucher, logic tinh giam theo nhom, het han/het luot, handler tao/ap voucher.</summary>
public class VoucherTests
{
    private static CreateVoucherRequest Req(string code = "KM10", decimal pct = 10, string scope = "ALL",
        DateOnly? from = null, DateOnly? to = null, int? max = null) =>
        new(code, "mo ta", pct, scope, from, to, max);

    private static ICurrentUser User()
    {
        var u = Substitute.For<ICurrentUser>();
        u.UserId.Returns(Guid.NewGuid());
        return u;
    }

    // ---------- Validator ----------

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void Validator_DiscountOutOfRange_Fails(double pct)
    {
        var r = new CreateVoucherCommandValidator().Validate(new CreateVoucherCommand(Req(pct: (decimal)pct)));
        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Validator_DiscountBoundary_Passes(double pct)
    {
        new CreateVoucherCommandValidator().Validate(new CreateVoucherCommand(Req(pct: (decimal)pct))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_InvalidAppliesTo_Fails()
    {
        new CreateVoucherCommandValidator().Validate(new CreateVoucherCommand(Req(scope: "FOO"))).IsValid.Should().BeFalse();
        foreach (var s in new[] { "EXAM", "LAB", "IMAGING", "DRUG", "ALL" })
            new CreateVoucherCommandValidator().Validate(new CreateVoucherCommand(Req(scope: s))).IsValid.Should().BeTrue(s);
    }

    [Fact]
    public void Validator_ValidFromAfterValidTo_Fails()
    {
        var r = Req(from: new DateOnly(2026, 12, 1), to: new DateOnly(2026, 11, 1));
        new CreateVoucherCommandValidator().Validate(new CreateVoucherCommand(r)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateCode_SameTenant_Fails_ButOtherTenantOk()
    {
        var name = Guid.NewGuid().ToString();
        using (var db = TestDbContextFactory.Create(name, 1))
        {
            var h = new CreateVoucherHandler(db, new FakeTenantProvider(1), User());
            (await h.Handle(new CreateVoucherCommand(Req("km10")), default)).IsSuccess.Should().BeTrue();
            var dup = await h.Handle(new CreateVoucherCommand(Req("KM10")), default);
            dup.IsSuccess.Should().BeFalse();
            dup.ErrorCode.Should().Be("VOUCHER_CODE_DUPLICATE");
        }
        using (var db2 = TestDbContextFactory.Create(name, 2))
        {
            var h2 = new CreateVoucherHandler(db2, new FakeTenantProvider(2), User());
            (await h2.Handle(new CreateVoucherCommand(Req("KM10")), default)).IsSuccess.Should().BeTrue();
        }
    }

    // ---------- Logic: nhom + tinh giam ----------

    [Theory]
    [InlineData("LAB", null, "LAB")]
    [InlineData("RAD", null, "IMAGING")]
    [InlineData("DRUG", null, "DRUG")]
    [InlineData("SERVICE", "EXAM", "EXAM")]
    [InlineData("SERVICE", "LAB", "LAB")]
    [InlineData("SERVICE", "OTHER", null)]
    [InlineData("SERVICE", null, null)]
    public void GroupOf_MapsCorrectly(string type, string? cat, string? expected) =>
        VoucherLogic.GroupOf(type, cat).Should().Be(expected);

    private static List<BillingItem> Items() => new()
    {
        new() { ItemType = "SERVICE", Name = "Kham", LineTotal = 350000 },
        new() { ItemType = "LAB", Name = "XN", LineTotal = 100000 },
        new() { ItemType = "RAD", Name = "SA", LineTotal = 200000 },
        new() { ItemType = "DRUG", Name = "Thuoc", LineTotal = 50000 },
    };

    [Theory]
    [InlineData("EXAM", 10, 35000)]
    [InlineData("LAB", 10, 10000)]
    [InlineData("IMAGING", 50, 100000)]
    [InlineData("DRUG", 20, 10000)]
    [InlineData("ALL", 10, 70000)]
    public void CalculateDiscount_ByGroup(string scope, double pct, double expected)
    {
        var v = new Voucher { AppliesTo = scope, DiscountPercent = (decimal)pct };
        var d = VoucherLogic.CalculateDiscount(v, Items(), i => i.ItemType == "SERVICE" ? "EXAM" : null);
        d.Should().Be((decimal)expected);
    }

    // ---------- Het han / het luot / inactive ----------

    [Fact]
    public void CheckUsable_Expired_And_NotYetValid()
    {
        var today = new DateOnly(2026, 10, 21);
        VoucherLogic.CheckUsable(new Voucher { ValidTo = new DateOnly(2026, 10, 20) }, today)!.Value.Code.Should().Be("VOUCHER_EXPIRED");
        VoucherLogic.CheckUsable(new Voucher { ValidFrom = new DateOnly(2026, 10, 22) }, today)!.Value.Code.Should().Be("VOUCHER_EXPIRED");
        VoucherLogic.CheckUsable(new Voucher { ValidFrom = today, ValidTo = today }, today).Should().BeNull();
    }

    [Fact]
    public void CheckUsable_UsageExceeded_And_Inactive()
    {
        var today = new DateOnly(2026, 10, 21);
        VoucherLogic.CheckUsable(new Voucher { MaxUses = 5, UsedCount = 5 }, today)!.Value.Code.Should().Be("VOUCHER_USAGE_EXCEEDED");
        VoucherLogic.CheckUsable(new Voucher { MaxUses = 5, UsedCount = 4 }, today).Should().BeNull();
        VoucherLogic.CheckUsable(new Voucher { IsActive = false }, today)!.Value.Code.Should().Be("VOUCHER_INACTIVE");
    }

    // ---------- Handler ap voucher ----------

    private static async Task<(ProDiabHis.Infrastructure.Persistence.AppDbContext db, Domain.Entities.Billing b)> Seed(
        Voucher v, string status = BillingStatus.Draft)
    {
        var db = TestDbContextFactory.Create(tenantId: 1);
        var svc = new BillingService { TenantId = 1, Code = "KBTH", Name = "Kham", Category = "EXAM", Price = 350000 };
        db.BillingServices.Add(svc);
        var b = new Domain.Entities.Billing { TenantId = 1, Status = status, BillNo = "HD-1" };
        b.Items.Add(new BillingItem { BillingId = b.Id, TenantId = 1, ItemType = "SERVICE", RefId = svc.Id, Name = "Kham", Quantity = 1, UnitPrice = 350000, LineTotal = 350000 });
        b.Items.Add(new BillingItem { BillingId = b.Id, TenantId = 1, ItemType = "LAB", Name = "XN", Quantity = 1, UnitPrice = 100000, LineTotal = 100000 });
        BillingCalc(b);
        v.TenantId = 1;
        db.Vouchers.Add(v);
        db.Billings.Add(b);
        foreach (var i in b.Items) db.BillingItems.Add(i);
        await db.SaveChangesAsync();
        return (db, b);
    }

    private static void BillingCalc(Domain.Entities.Billing b)
    {
        b.Subtotal = b.Items.Sum(i => i.LineTotal);
        b.PatientPayable = b.Subtotal;
        b.Balance = b.PatientPayable;
    }

    [Fact]
    public async Task Apply_ExamVoucher_DiscountsOnlyExamLines_RecordsUsage()
    {
        var (db, b) = await Seed(new Voucher { Code = "KM10", DiscountPercent = 10, AppliesTo = "EXAM", MaxUses = 3 });
        var h = new ApplyVoucherHandler(db, new FakeTenantProvider(1), User());

        var r = await h.Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("km10")), default);

        r.IsSuccess.Should().BeTrue(r.ErrorMessage);
        r.Value!.DiscountAmount.Should().Be(35000);
        r.Value.PatientPayable.Should().Be(450000 - 35000);
        db.VoucherUsages.Count().Should().Be(1);
        db.Vouchers.Single().UsedCount.Should().Be(1);

        // ap lan 2 cung voucher len cung hoa don -> chan
        var again = await h.Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("KM10")), default);
        again.ErrorCode.Should().Be("VOUCHER_ALREADY_APPLIED");
    }

    [Fact]
    public async Task Apply_ExpiredVoucher_Fails()
    {
        var (db, b) = await Seed(new Voucher { Code = "OLD", DiscountPercent = 10, ValidTo = new DateOnly(2000, 1, 1) });
        var r = await new ApplyVoucherHandler(db, new FakeTenantProvider(1), User())
            .Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("OLD")), default);
        r.ErrorCode.Should().Be("VOUCHER_EXPIRED");
        db.VoucherUsages.Count().Should().Be(0);
    }

    [Fact]
    public async Task Apply_UsageExceeded_Fails()
    {
        var (db, b) = await Seed(new Voucher { Code = "FULL", DiscountPercent = 10, MaxUses = 2, UsedCount = 2 });
        var r = await new ApplyVoucherHandler(db, new FakeTenantProvider(1), User())
            .Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("FULL")), default);
        r.ErrorCode.Should().Be("VOUCHER_USAGE_EXCEEDED");
    }

    [Fact]
    public async Task Apply_UnknownCode_And_FinalizedBilling_Fail()
    {
        var (db, b) = await Seed(new Voucher { Code = "OK", DiscountPercent = 10 });
        var h = new ApplyVoucherHandler(db, new FakeTenantProvider(1), User());
        (await h.Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("NOPE")), default))
            .ErrorCode.Should().Be("VOUCHER_NOT_FOUND");

        b.Status = BillingStatus.Finalized;
        await db.SaveChangesAsync();
        (await h.Handle(new ApplyVoucherCommand(b.Id, new ApplyVoucherRequest("OK")), default))
            .ErrorCode.Should().Be("BILLING_ALREADY_FINALIZED");
    }
}
