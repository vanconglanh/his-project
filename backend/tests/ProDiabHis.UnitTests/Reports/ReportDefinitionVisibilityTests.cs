using ProDiabHis.Application.Reports.Engine;
using ProDiabHis.Infrastructure.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>Kiem tra ReportDefinitionStore.IsVisibleToUser — quy tac chia se bao cao tu tao theo
/// visibility PRIVATE/TENANT/ROLE (Report Builder P3.2).</summary>
public class ReportDefinitionVisibilityTests
{
    private static ReportDefinition MakeDefinition(ReportVisibility visibility, string? createdBy, IReadOnlyList<string>? sharedRoles = null)
        => new(
            Id: "id-1", TenantId: 1, Code: "ud-abc", Title: "Test",
            DatasetKey: "kho", Columns: Array.Empty<ReportDefinitionColumn>(), Filters: Array.Empty<ReportDefinitionFilter>(),
            GroupBy: Array.Empty<string>(), Sort: Array.Empty<ReportDefinitionSort>(), Kpis: Array.Empty<ReportDefinitionKpi>(),
            CalcFields: Array.Empty<ReportDefinitionCalcField>(), Chart: null, ViewType: ReportViewType.Table,
            Visibility: visibility, SharedRoles: sharedRoles ?? Array.Empty<string>(), IsActive: true,
            CreatedBy: createdBy, CreatedAt: DateTime.UtcNow, UpdatedBy: null, UpdatedAt: DateTime.UtcNow);

    [Fact]
    public void Tenant_VisibleToEveryoneInTenant()
    {
        var d = MakeDefinition(ReportVisibility.Tenant, createdBy: "owner-1");
        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "other-user", currentUserRoles: new List<string> { "ke_toan" }));
        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: null, currentUserRoles: Array.Empty<string>()));
    }

    [Fact]
    public void Private_OnlyVisibleToOwner()
    {
        var d = MakeDefinition(ReportVisibility.Private, createdBy: "owner-1");
        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "owner-1", currentUserRoles: Array.Empty<string>()));
        Assert.False(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "other-user", currentUserRoles: new List<string> { "admin" }));
        Assert.False(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: null, currentUserRoles: Array.Empty<string>()));
    }

    [Fact]
    public void Role_VisibleToOwnerAndSharedRole_NotToOthers()
    {
        var d = MakeDefinition(ReportVisibility.Role, createdBy: "owner-1", sharedRoles: new List<string> { "bac_si" });

        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "owner-1", currentUserRoles: Array.Empty<string>()), "chu so huu luon nhin thay");
        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "user-2", currentUserRoles: new List<string> { "bac_si" }), "role duoc chia se phai nhin thay");
        Assert.False(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "user-3", currentUserRoles: new List<string> { "ke_toan" }), "role khac khong duoc chia se");
        Assert.False(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: null, currentUserRoles: Array.Empty<string>()));
    }

    [Fact]
    public void Role_IsCaseInsensitive()
    {
        var d = MakeDefinition(ReportVisibility.Role, createdBy: "owner-1", sharedRoles: new List<string> { "BAC_SI" });
        Assert.True(ReportDefinitionStore.IsVisibleToUser(d, currentUserId: "user-2", currentUserRoles: new List<string> { "bac_si" }));
    }
}
