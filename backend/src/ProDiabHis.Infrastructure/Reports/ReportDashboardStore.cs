using System.Data;
using System.Text.Json;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// CRUD tenant-scoped cho dashboard tuy bien (diab_his_rep_dashboards). Moi truy van BAT BUOC co
/// WHERE tenant_id = @tenantId. Doc/ghi widgets_json qua System.Text.Json — cot JSON cua MySQL tra ve/nhan
/// chuoi qua Dapper (giong pattern ReportDefinitionStore).
/// </summary>
public class ReportDashboardStore : IReportDashboardStore
{
    private readonly IDapperConnectionFactory _db;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ReportDashboardStore(IDapperConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<ReportDashboard>> GetVisibleAsync(int tenantId, string? currentUserId, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var rows = await conn.QueryAsync<RepDashboardRow>(new CommandDefinition(
            @"SELECT id, tenant_id AS TenantId, code, title, widgets_json AS WidgetsJson,
                     visibility, is_active AS IsActive,
                     created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt
              FROM diab_his_rep_dashboards
              WHERE tenant_id = @tenantId AND is_active = 1 AND deleted_at IS NULL
                AND (visibility = 'TENANT' OR (visibility = 'PRIVATE' AND created_by = @currentUserId))
              ORDER BY created_at DESC",
            new { tenantId, currentUserId }, cancellationToken: ct));

        return rows.Select(Map).ToList();
    }

    public async Task<ReportDashboard?> GetByIdAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RepDashboardRow>(new CommandDefinition(
            @"SELECT id, tenant_id AS TenantId, code, title, widgets_json AS WidgetsJson,
                     visibility, is_active AS IsActive,
                     created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt
              FROM diab_his_rep_dashboards
              WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL
              LIMIT 1",
            new { tenantId, id }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<ReportDashboard> CreateAsync(int tenantId, string createdBy, ReportDashboardInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var id = Guid.NewGuid().ToString();
        var code = $"db-{Guid.NewGuid():N}"[..11]; // "db-" + 8 hex

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO diab_his_rep_dashboards
                (id, tenant_id, code, title, widgets_json, visibility, is_active, created_by, created_at, updated_by, updated_at)
              VALUES
                (@id, @tenantId, @code, @title, CAST(@widgetsJson AS JSON), @visibility, 1, @createdBy, NOW(), @createdBy, NOW())",
            new
            {
                id,
                tenantId,
                code,
                title = input.Title,
                widgetsJson = SerializeWidgets(input.Widgets),
                visibility = input.Visibility == ReportVisibility.Private ? "PRIVATE" : "TENANT",
                createdBy
            }, cancellationToken: ct));

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task<ReportDashboard> UpdateAsync(int tenantId, string id, string updatedBy, ReportDashboardInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE diab_his_rep_dashboards
                 SET title = @title, widgets_json = CAST(@widgetsJson AS JSON), visibility = @visibility,
                     updated_by = @updatedBy, updated_at = NOW()
               WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL",
            new
            {
                tenantId,
                id,
                title = input.Title,
                widgetsJson = SerializeWidgets(input.Widgets),
                visibility = input.Visibility == ReportVisibility.Private ? "PRIVATE" : "TENANT",
                updatedBy
            }, cancellationToken: ct));

        if (affected == 0)
            throw new ReportValidationException("REPORT_DASHBOARD_NOT_FOUND", "Không tìm thấy dashboard");

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task DeleteAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE diab_his_rep_dashboards SET deleted_at = NOW(), is_active = 0 WHERE tenant_id = @tenantId AND id = @id",
            new { tenantId, id }, cancellationToken: ct));
    }

    // ---- Mapping ---- //

    private static ReportDashboard Map(RepDashboardRow row)
    {
        var widgets = DeserializeWidgets(row.WidgetsJson);

        return new ReportDashboard(
            row.Id, row.TenantId, row.Code, row.Title, widgets,
            row.Visibility == "PRIVATE" ? ReportVisibility.Private : ReportVisibility.Tenant,
            row.IsActive, row.CreatedBy, row.CreatedAt, row.UpdatedBy, row.UpdatedAt);
    }

    private static string SerializeWidgets(IReadOnlyList<ReportDashboardWidget> widgets)
        => JsonSerializer.Serialize(
            widgets.Select(w => new WidgetJsonDto(w.ReportCode, w.Title, w.WidgetType.ToString().ToUpperInvariant(), w.W, w.H, w.X, w.Y)).ToList(),
            JsonOpts);

    private static IReadOnlyList<ReportDashboardWidget> DeserializeWidgets(string json)
    {
        var dtos = JsonSerializer.Deserialize<List<WidgetJsonDto>>(json, JsonOpts) ?? new();
        return dtos.Select(w => new ReportDashboardWidget(
            w.ReportCode, w.Title,
            w.WidgetType.ToUpperInvariant() switch
            {
                "CHART" => ReportWidgetType.Chart,
                "KPI" => ReportWidgetType.Kpi,
                _ => ReportWidgetType.Table
            },
            w.W, w.H, w.X, w.Y)).ToList();
    }

    private record RepDashboardRow(
        string Id, int TenantId, string Code, string Title, string WidgetsJson, string Visibility,
        bool IsActive, string? CreatedBy, DateTime CreatedAt, string? UpdatedBy, DateTime UpdatedAt);

    private record WidgetJsonDto(string ReportCode, string Title, string WidgetType, int W, int H, int X, int Y);
}
