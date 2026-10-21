using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reception;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Reception;

public class ServiceOnlyOrderHandlerTests
{
    private static (CreateServiceOnlyOrderHandler h, ProDiabHis.Infrastructure.Persistence.AppDbContext db, Patient p, BillingService s1, BillingService s2)
        Setup()
    {
        var db = TestDbContextFactory.Create();
        var p = new Patient { TenantId = 1, Code = "BN1", FullName = "Nguyen Van A" };
        var s1 = new BillingService { TenantId = 1, Code = "ECG", Name = "Dien tim", Category = "EXAM", Price = 100000 };
        var s2 = new BillingService { TenantId = 1, Code = "XR", Name = "X-quang", Category = "RAD", Price = 200000 };
        db.Patients.Add(p); db.BillingServices.AddRange(s1, s2);
        db.SaveChanges();
        var resolver = Substitute.For<IServicePriceResolver>();
        resolver.ResolveAsync(default, default, default, default, default).ReturnsForAnyArgs((ResolvedServicePrice?)null);
        var user = Substitute.For<ICurrentUser>();
        return (new CreateServiceOnlyOrderHandler(db, new FakeTenantProvider(1), user, resolver), db, p, s1, s2);
    }

    [Fact]
    public async Task Tao_hoa_don_draft_khong_gan_encounter_dung_item()
    {
        var (h, db, p, s1, s2) = Setup();
        var r = await h.Handle(new CreateServiceOnlyOrderCommand(
            new ServiceOnlyOrderRequest(p.Id, new() { s1.Id, s2.Id }, null, "ghi chu", null)), default);

        r.IsSuccess.Should().BeTrue();
        var b = await db.Billings.Include(x => x.Items).SingleAsync();
        b.EncounterId.Should().BeNull();
        b.Status.Should().Be(BillingStatus.Draft);
        b.Items.Should().HaveCount(2);
        b.Subtotal.Should().Be(300000);
        r.Value!.BillingId.Should().Be(b.Id);
    }

    [Fact]
    public async Task Khong_chon_dich_vu_tra_loi()
    {
        var (h, _, p, _, _) = Setup();
        var r = await h.Handle(new CreateServiceOnlyOrderCommand(new ServiceOnlyOrderRequest(p.Id, null, null, null, null)), default);
        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("SERVICE_ONLY_EMPTY");
    }

    [Fact]
    public async Task Benh_nhan_khong_ton_tai()
    {
        var (h, _, _, s1, _) = Setup();
        var r = await h.Handle(new CreateServiceOnlyOrderCommand(new ServiceOnlyOrderRequest(Guid.NewGuid(), new() { s1.Id }, null, null, null)), default);
        r.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }
}
