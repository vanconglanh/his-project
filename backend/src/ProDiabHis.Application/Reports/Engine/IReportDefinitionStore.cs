namespace ProDiabHis.Application.Reports.Engine;

/// <summary>1 cot trong definition_json.columns[] — field whitelist cua Dataset + phep gop (neu la Measure).</summary>
public record ReportDefinitionColumn(string Field, string Label, ReportAggregation? Agg, bool IsSubtotal = false);

/// <summary>1 dieu kien loc trong definition_json.filters[] — toan tu tu tap co dinh (khong tu do).</summary>
public record ReportDefinitionFilter(string Field, string Op, IReadOnlyList<string?> Value);

/// <summary>1 tieu chi sap xep trong definition_json.sort[].</summary>
public record ReportDefinitionSort(string Field, bool Desc);

/// <summary>1 KPI card trong definition_json.kpis[] — Field phai la Measure cua Dataset.</summary>
public record ReportDefinitionKpi(string Label, string Field, ReportAggregation Agg);

/// <summary>Cau hinh bieu do (chart_json) — chi khi ViewType = Chart.</summary>
public record ReportDefinitionChart(string Type, IReadOnlyList<string> Dims, string Measure);

/// <summary>
/// 1 cot tinh toan (calculated field) trong definition_json.calc_fields[] — do nguoi dung tu dinh nghia
/// bang cong thuc CHI tham chieu cac measure field cua Dataset + toan tu so hoc (+ - * / ()).
/// <see cref="Formula"/> KHONG BAO GIO duoc noi suy truc tiep vao SQL — luon di qua
/// <see cref="CalcFormulaParser"/> (whitelist token: field measure / so / + - * / ( )).
/// Column trong definition.columns[] tro toi calc field bang Field == Key, Agg == null.
/// </summary>
public record ReportDefinitionCalcField(string Key, string Label, string Formula, string DataType);

public enum ReportViewType { Table, Chart }
public enum ReportVisibility { Private, Tenant, Role }

/// <summary>
/// DTO 1 bao cao do nguoi dung tu tao (map truc tiep den bang diab_his_rep_definitions, tenant-scoped).
/// </summary>
public record ReportDefinition(
    string Id,
    int TenantId,
    string Code,
    string Title,
    string DatasetKey,
    IReadOnlyList<ReportDefinitionColumn> Columns,
    IReadOnlyList<ReportDefinitionFilter> Filters,
    IReadOnlyList<string> GroupBy,
    IReadOnlyList<ReportDefinitionSort> Sort,
    IReadOnlyList<ReportDefinitionKpi> Kpis,
    IReadOnlyList<ReportDefinitionCalcField> CalcFields,
    ReportDefinitionChart? Chart,
    ReportViewType ViewType,
    ReportVisibility Visibility,
    IReadOnlyList<string> SharedRoles,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime UpdatedAt);

/// <summary>Input tao/sua 1 ReportDefinition (chua tenant/code/audit — do handler/store tu gan).</summary>
public record ReportDefinitionInput(
    string Title,
    string DatasetKey,
    IReadOnlyList<ReportDefinitionColumn> Columns,
    IReadOnlyList<ReportDefinitionFilter> Filters,
    IReadOnlyList<string> GroupBy,
    IReadOnlyList<ReportDefinitionSort> Sort,
    IReadOnlyList<ReportDefinitionKpi> Kpis,
    ReportDefinitionChart? Chart,
    ReportViewType ViewType,
    ReportVisibility Visibility,
    IReadOnlyList<ReportDefinitionCalcField>? CalcFields = null,
    IReadOnlyList<string>? SharedRoles = null);

/// <summary>CRUD tenant-scoped cho bao cao tu tao (diab_his_rep_definitions).</summary>
public interface IReportDefinitionStore
{
    /// <summary>Danh sach bao cao "nhin thay duoc" boi current user trong tenant hien tai
    /// (TENANT: ca phong kham; PRIVATE: chi chu so huu; ROLE: chu so huu + role trong shared_roles).</summary>
    Task<IReadOnlyList<ReportDefinition>> GetVisibleAsync(int tenantId, string? currentUserId, IReadOnlyList<string> currentUserRoles, CancellationToken ct);

    Task<ReportDefinition?> GetByCodeAsync(int tenantId, string code, CancellationToken ct);

    Task<ReportDefinition?> GetByIdAsync(int tenantId, string id, CancellationToken ct);

    Task<ReportDefinition> CreateAsync(int tenantId, string createdBy, ReportDefinitionInput input, CancellationToken ct);

    Task<ReportDefinition> UpdateAsync(int tenantId, string id, string updatedBy, ReportDefinitionInput input, CancellationToken ct);

    Task DeleteAsync(int tenantId, string id, CancellationToken ct);
}
