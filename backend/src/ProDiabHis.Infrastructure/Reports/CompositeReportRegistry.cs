using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Gop 2 nguon descriptor: code-defined (<see cref="ReportRegistry"/>, singleton, 43 bao cao he thong) +
/// dong (dung tu <c>diab_his_rep_definitions</c> cua tenant hien tai — Report Builder P1). Dang ky Scoped
/// vi phai biet tenant/user hien tai (khac voi ReportRegistry code-defined khong doi theo request).
/// GetByCode tra code-defined truoc; khong thay moi tra store (nen luon khop tenant vi store da tu WHERE
/// tenant_id=@tenantId — khong the lay duoc dinh nghia cua tenant khac du biet code).
/// </summary>
public class CompositeReportRegistry : IReportRegistry
{
    private readonly ReportRegistry _codeDefined;
    private readonly IDatasetRegistry _datasets;
    private readonly IReportDefinitionStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public CompositeReportRegistry(
        ReportRegistry codeDefined,
        IDatasetRegistry datasets,
        IReportDefinitionStore store,
        ITenantProvider tenant,
        ICurrentUser currentUser)
    {
        _codeDefined = codeDefined;
        _datasets = datasets;
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public IReadOnlyList<ReportDescriptor> GetAll()
        => _codeDefined.GetAll().Concat(GetDynamicDescriptors()).ToList();

    public ReportDescriptor? GetByCode(string code)
    {
        var fromCode = _codeDefined.GetByCode(code);
        if (fromCode is not null) return fromCode;

        var definition = _store.GetByCodeAsync(_tenant.TenantId, code, CancellationToken.None)
            .GetAwaiter().GetResult();
        if (definition is null) return null;

        // Enforce visibility PRIVATE/ROLE ngay tai /data (khong chi o danh sach) — user khong duoc chia se
        // khong the "doan" ma bao cao roi chay duoc du biet code. Tenant isolation da dam bao boi store
        // (WHERE tenant_id=@tenantId trong ReportDefinitionStore) — khong the lay dinh nghia tenant khac.
        if (!ReportDefinitionStore.IsVisibleToUser(definition, _currentUser.UserId?.ToString(), _currentUser.RoleCodes))
            return null;

        var dataset = _datasets.GetByKey(definition.DatasetKey);
        return dataset is null ? null : DynamicDescriptorFactory.Create(definition, dataset);
    }

    private List<ReportDescriptor> GetDynamicDescriptors()
    {
        var definitions = _store.GetVisibleAsync(_tenant.TenantId, _currentUser.UserId?.ToString(), _currentUser.RoleCodes, CancellationToken.None)
            .GetAwaiter().GetResult();

        var list = new List<ReportDescriptor>();
        foreach (var def in definitions)
        {
            var dataset = _datasets.GetByKey(def.DatasetKey);
            if (dataset is null) continue; // dataset bi go bo sau khi da co bao cao dung no — bo qua, khong lam sap catalog

            try { list.Add(DynamicDescriptorFactory.Create(def, dataset)); }
            catch (ReportValidationException) { /* dinh nghia hong (field/agg khong con hop le) — bo qua */ }
        }
        return list;
    }
}
