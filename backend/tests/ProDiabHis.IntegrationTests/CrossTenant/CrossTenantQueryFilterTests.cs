using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.CrossTenant;

/// <summary>
/// ITC-XTENANT-QF — Xac minh EF Core Global Query Filter (HasQueryFilter theo TenantId) hoat dong
/// dung cho tung entity chinh: DbContext scoped tenant A KHONG thay row cua tenant B, va nguoc lai.
/// IgnoreQueryFilters() cho thay CA HAI (chung to du lieu that co ton tai -> filter moi la thu chan).
/// </summary>
[Collection("Api")]
public class CrossTenantQueryFilterTests
{
    private readonly ApiTestFixture _fx;

    public CrossTenantQueryFilterTests(ApiTestFixture fx)
    {
        _fx = fx;
        CrossTenantSeeder.EnsureSeeded(_fx.NewDbContext);
    }

    [ApiFact]
    public async Task Patients_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);

        (await ctxA.Patients.AnyAsync(p => p.Id == CrossTenantIds.PatientA)).Should().BeTrue();
        (await ctxA.Patients.AnyAsync(p => p.Id == CrossTenantIds.PatientB))
            .Should().BeFalse("query filter tenant A khong duoc thay benh nhan tenant B");

        // IgnoreQueryFilters -> thay ca 2 (du lieu that ton tai)
        (await ctxA.Patients.IgnoreQueryFilters().AnyAsync(p => p.Id == CrossTenantIds.PatientB)).Should().BeTrue();
    }

    [ApiFact]
    public async Task Encounters_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.Encounters.AnyAsync(e => e.Id == CrossTenantIds.EncounterA)).Should().BeTrue();
        (await ctxA.Encounters.AnyAsync(e => e.Id == CrossTenantIds.EncounterB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task Billings_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.Billings.AnyAsync(b => b.Id == CrossTenantIds.BillingA)).Should().BeTrue();
        (await ctxA.Billings.AnyAsync(b => b.Id == CrossTenantIds.BillingB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task Prescriptions_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.Prescriptions.AnyAsync(p => p.Id == CrossTenantIds.PrescA)).Should().BeTrue();
        (await ctxA.Prescriptions.AnyAsync(p => p.Id == CrossTenantIds.PrescB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task LabResults_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.LabResults.AnyAsync(l => l.Id == CrossTenantIds.LabA)).Should().BeTrue();
        (await ctxA.LabResults.AnyAsync(l => l.Id == CrossTenantIds.LabB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task Drugs_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.Drugs.AnyAsync(d => d.Id == CrossTenantIds.DrugA)).Should().BeTrue();
        (await ctxA.Drugs.AnyAsync(d => d.Id == CrossTenantIds.DrugB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task Branches_QueryFilter_TenantA_ChiThayCuaMinh()
    {
        await using var ctxA = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantA);
        (await ctxA.Branches.AnyAsync(b => b.Id == CrossTenantIds.BranchA)).Should().BeTrue();
        (await ctxA.Branches.AnyAsync(b => b.Id == CrossTenantIds.BranchB)).Should().BeFalse();
    }

    [ApiFact]
    public async Task Users_QueryFilter_TenantB_ChiThayCuaMinh()
    {
        // Chieu nguoc: DbContext scoped tenant B khong thay user tenant A.
        await using var ctxB = CrossTenantSeeder.ContextForTenant(_fx.ConnectionString, CrossTenantIds.TenantB);
        (await ctxB.Users.AnyAsync(u => u.Id == CrossTenantIds.UserB)).Should().BeTrue();
        (await ctxB.Users.AnyAsync(u => u.Id == CrossTenantIds.UserA)).Should().BeFalse();
    }
}
