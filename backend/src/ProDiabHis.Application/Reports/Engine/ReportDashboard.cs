namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Kieu widget tren 1 dashboard tuy bien (P2.2).</summary>
public enum ReportWidgetType { Table, Chart, Kpi }

/// <summary>1 widget ghim 1 bao cao (code-defined hoac tu tao) len dashboard, kem vi tri/kich thuoc luoi.</summary>
public record ReportDashboardWidget(
    string ReportCode,
    string Title,
    ReportWidgetType WidgetType,
    int W,
    int H,
    int X,
    int Y);

/// <summary>DTO 1 dashboard tuy bien (map truc tiep den bang diab_his_rep_dashboards, tenant-scoped).</summary>
public record ReportDashboard(
    string Id,
    int TenantId,
    string Code,
    string Title,
    IReadOnlyList<ReportDashboardWidget> Widgets,
    ReportVisibility Visibility,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime UpdatedAt);

/// <summary>Input tao/sua 1 ReportDashboard (chua tenant/code/audit — do handler/store tu gan).</summary>
public record ReportDashboardInput(
    string Title,
    IReadOnlyList<ReportDashboardWidget> Widgets,
    ReportVisibility Visibility);

/// <summary>CRUD tenant-scoped cho dashboard tuy bien (diab_his_rep_dashboards).</summary>
public interface IReportDashboardStore
{
    /// <summary>Danh sach dashboard "nhin thay duoc" boi current user trong tenant hien tai
    /// (TENANT: ca phong kham; PRIVATE: chi chu so huu).</summary>
    Task<IReadOnlyList<ReportDashboard>> GetVisibleAsync(int tenantId, string? currentUserId, CancellationToken ct);

    Task<ReportDashboard?> GetByIdAsync(int tenantId, string id, CancellationToken ct);

    Task<ReportDashboard> CreateAsync(int tenantId, string createdBy, ReportDashboardInput input, CancellationToken ct);

    Task<ReportDashboard> UpdateAsync(int tenantId, string id, string updatedBy, ReportDashboardInput input, CancellationToken ct);

    Task DeleteAsync(int tenantId, string id, CancellationToken ct);
}
