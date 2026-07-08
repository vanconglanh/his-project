using System.Data;
using System.Text.Json;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// CRUD tenant-scoped cho bao cao tu tao (diab_his_rep_definitions). Moi truy van BAT BUOC co
/// WHERE tenant_id = @tenantId (khong trust code goi tu ngoai). Doc/ghi definition_json/chart_json
/// qua System.Text.Json — cot JSON cua MySQL tra ve/nhan chuoi qua Dapper.
/// </summary>
public class ReportDefinitionStore : IReportDefinitionStore
{
    private readonly IDapperConnectionFactory _db;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ReportDefinitionStore(IDapperConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<ReportDefinition>> GetVisibleAsync(int tenantId, string? currentUserId, IReadOnlyList<string> currentUserRoles, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        // Lay ca TENANT + PRIVATE(cua chinh minh) + ROLE (loc chinh xac trong C# vi can so sanh JSON
        // shared_roles_json voi danh sach role cua user hien tai — khong the lam gon trong 1 cau SQL portable).
        var rows = await conn.QueryAsync<RepDefinitionRow>(new CommandDefinition(
            @"SELECT id, tenant_id AS TenantId, code, title, dataset_key AS DatasetKey,
                     definition_json AS DefinitionJson, chart_json AS ChartJson,
                     view_type AS ViewType, orientation, visibility, shared_roles_json AS SharedRolesJson, is_active AS IsActive,
                     created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt
              FROM diab_his_rep_definitions
              WHERE tenant_id = @tenantId AND is_active = 1 AND deleted_at IS NULL
                AND (visibility = 'TENANT' OR visibility = 'ROLE' OR (visibility = 'PRIVATE' AND created_by = @currentUserId))
              ORDER BY created_at DESC",
            new { tenantId, currentUserId }, cancellationToken: ct));

        return rows.Select(Map)
            .Where(d => IsVisibleToUser(d, currentUserId, currentUserRoles))
            .ToList();
    }

    /// <summary>Kiem tra 1 ReportDefinition da nap co "nhin thay duoc" boi user hien tai khong — dung chung
    /// cho GetVisibleAsync (loc danh sach) va CompositeReportRegistry (loc GetByCode).</summary>
    public static bool IsVisibleToUser(ReportDefinition d, string? currentUserId, IReadOnlyList<string>? currentUserRoles)
    {
        var isOwner = currentUserId is not null && string.Equals(d.CreatedBy, currentUserId, StringComparison.OrdinalIgnoreCase);

        return d.Visibility switch
        {
            ReportVisibility.Tenant => true,
            ReportVisibility.Private => isOwner,
            ReportVisibility.Role => isOwner || (currentUserRoles is not null &&
                d.SharedRoles.Any(sr => currentUserRoles.Any(r => string.Equals(r, sr, StringComparison.OrdinalIgnoreCase)))),
            _ => false
        };
    }

    public async Task<ReportDefinition?> GetByCodeAsync(int tenantId, string code, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RepDefinitionRow>(new CommandDefinition(
            @"SELECT id, tenant_id AS TenantId, code, title, dataset_key AS DatasetKey,
                     definition_json AS DefinitionJson, chart_json AS ChartJson,
                     view_type AS ViewType, orientation, visibility, shared_roles_json AS SharedRolesJson, is_active AS IsActive,
                     created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt
              FROM diab_his_rep_definitions
              WHERE tenant_id = @tenantId AND code = @code AND is_active = 1 AND deleted_at IS NULL
              LIMIT 1",
            new { tenantId, code }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<ReportDefinition?> GetByIdAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RepDefinitionRow>(new CommandDefinition(
            @"SELECT id, tenant_id AS TenantId, code, title, dataset_key AS DatasetKey,
                     definition_json AS DefinitionJson, chart_json AS ChartJson,
                     view_type AS ViewType, orientation, visibility, shared_roles_json AS SharedRolesJson, is_active AS IsActive,
                     created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt
              FROM diab_his_rep_definitions
              WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL
              LIMIT 1",
            new { tenantId, id }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<ReportDefinition> CreateAsync(int tenantId, string createdBy, ReportDefinitionInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var id = Guid.NewGuid().ToString();
        var code = $"ud-{Guid.NewGuid():N}"[..11]; // "ud-" + 8 hex

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO diab_his_rep_definitions
                (id, tenant_id, code, title, dataset_key, definition_json, chart_json, view_type, visibility, shared_roles_json, is_active, created_by, created_at, updated_by, updated_at)
              VALUES
                (@id, @tenantId, @code, @title, @datasetKey, CAST(@definitionJson AS JSON), CAST(@chartJson AS JSON), @viewType, @visibility, CAST(@sharedRolesJson AS JSON), 1, @createdBy, NOW(), @createdBy, NOW())",
            new
            {
                id,
                tenantId,
                code,
                title = input.Title,
                datasetKey = input.DatasetKey,
                definitionJson = SerializeDefinition(input),
                chartJson = SerializeChart(input.Chart),
                viewType = input.ViewType == ReportViewType.Chart ? "CHART" : "TABLE",
                visibility = VisibilityCode(input.Visibility),
                sharedRolesJson = SerializeSharedRoles(input.SharedRoles),
                createdBy
            }, cancellationToken: ct));

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task<ReportDefinition> UpdateAsync(int tenantId, string id, string updatedBy, ReportDefinitionInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE diab_his_rep_definitions
                 SET title = @title, dataset_key = @datasetKey, definition_json = CAST(@definitionJson AS JSON),
                     chart_json = CAST(@chartJson AS JSON), view_type = @viewType, visibility = @visibility,
                     shared_roles_json = CAST(@sharedRolesJson AS JSON),
                     updated_by = @updatedBy, updated_at = NOW()
               WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL",
            new
            {
                tenantId,
                id,
                title = input.Title,
                datasetKey = input.DatasetKey,
                definitionJson = SerializeDefinition(input),
                chartJson = SerializeChart(input.Chart),
                viewType = input.ViewType == ReportViewType.Chart ? "CHART" : "TABLE",
                visibility = VisibilityCode(input.Visibility),
                sharedRolesJson = SerializeSharedRoles(input.SharedRoles),
                updatedBy
            }, cancellationToken: ct));

        if (affected == 0)
            throw new ReportValidationException("REPORT_NOT_FOUND", "Không tìm thấy báo cáo tự tạo");

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task DeleteAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE diab_his_rep_definitions SET deleted_at = NOW(), is_active = 0 WHERE tenant_id = @tenantId AND id = @id",
            new { tenantId, id }, cancellationToken: ct));
    }

    // ---- Mapping ---- //

    private static string VisibilityCode(ReportVisibility v) => v switch
    {
        ReportVisibility.Private => "PRIVATE",
        ReportVisibility.Role => "ROLE",
        _ => "TENANT"
    };

    private static ReportVisibility VisibilityFromCode(string code) => code switch
    {
        "PRIVATE" => ReportVisibility.Private,
        "ROLE" => ReportVisibility.Role,
        _ => ReportVisibility.Tenant
    };

    private static ReportDefinition Map(RepDefinitionRow row)
    {
        var (columns, filters, groupBy, sort, kpis, calcFields) = DeserializeDefinition(row.DefinitionJson);
        var chart = DeserializeChart(row.ChartJson);
        var sharedRoles = DeserializeSharedRoles(row.SharedRolesJson);

        return new ReportDefinition(
            row.Id, row.TenantId, row.Code, row.Title, row.DatasetKey,
            columns, filters, groupBy, sort, kpis, calcFields, chart,
            row.ViewType == "CHART" ? ReportViewType.Chart : ReportViewType.Table,
            VisibilityFromCode(row.Visibility), sharedRoles,
            row.IsActive, row.CreatedBy, row.CreatedAt, row.UpdatedBy, row.UpdatedAt);
    }

    private static string SerializeDefinition(ReportDefinitionInput input)
    {
        var dto = new DefinitionJsonDto(
            input.Columns.Select(c => new ColumnJsonDto(c.Field, c.Label, c.Agg is null ? null : ReportAggregationCodes.ToCode(c.Agg.Value), c.IsSubtotal)).ToList(),
            (input.Filters ?? Array.Empty<ReportDefinitionFilter>()).Select(f => new FilterJsonDto(f.Field, f.Op, f.Value.ToList())).ToList(),
            (input.GroupBy ?? Array.Empty<string>()).ToList(),
            (input.Sort ?? Array.Empty<ReportDefinitionSort>()).Select(s => new SortJsonDto(s.Field, s.Desc)).ToList(),
            (input.Kpis ?? Array.Empty<ReportDefinitionKpi>()).Select(k => new KpiJsonDto(k.Label, k.Field, ReportAggregationCodes.ToCode(k.Agg))).ToList(),
            (input.CalcFields ?? Array.Empty<ReportDefinitionCalcField>()).Select(c => new CalcFieldJsonDto(c.Key, c.Label, c.Formula, c.DataType)).ToList());

        return JsonSerializer.Serialize(dto, JsonOpts);
    }

    private static string? SerializeChart(ReportDefinitionChart? chart)
        => chart is null ? null : JsonSerializer.Serialize(new ChartJsonDto(chart.Type, chart.Dims.ToList(), chart.Measure), JsonOpts);

    private static string SerializeSharedRoles(IReadOnlyList<string>? roles)
        => JsonSerializer.Serialize((roles ?? Array.Empty<string>()).ToList(), JsonOpts);

    private static (
        IReadOnlyList<ReportDefinitionColumn> Columns,
        IReadOnlyList<ReportDefinitionFilter> Filters,
        IReadOnlyList<string> GroupBy,
        IReadOnlyList<ReportDefinitionSort> Sort,
        IReadOnlyList<ReportDefinitionKpi> Kpis,
        IReadOnlyList<ReportDefinitionCalcField> CalcFields) DeserializeDefinition(string json)
    {
        var dto = JsonSerializer.Deserialize<DefinitionJsonDto>(json, JsonOpts)
            ?? new DefinitionJsonDto(new(), new(), new(), new(), new(), new());

        var columns = dto.Columns.Select(c => new ReportDefinitionColumn(
            c.Field, c.Label, ReportAggregationCodes.TryFromCode(c.Agg, out var agg) ? agg : null, c.IsSubtotal)).ToList();

        var filters = dto.Filters.Select(f => new ReportDefinitionFilter(f.Field, f.Op, f.Value)).ToList();
        var groupBy = dto.GroupBy.ToList();
        var sort = dto.Sort.Select(s => new ReportDefinitionSort(s.Field, s.Desc)).ToList();
        var kpis = dto.Kpis.Select(k => new ReportDefinitionKpi(k.Label, k.Field, ReportAggregationCodes.FromCode(k.Agg))).ToList();
        var calcFields = (dto.CalcFields ?? new()).Select(c => new ReportDefinitionCalcField(c.Key, c.Label, c.Formula, c.DataType)).ToList();

        return (columns, filters, groupBy, sort, kpis, calcFields);
    }

    private static ReportDefinitionChart? DeserializeChart(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var dto = JsonSerializer.Deserialize<ChartJsonDto>(json, JsonOpts);
        return dto is null ? null : new ReportDefinitionChart(dto.Type, dto.Dims, dto.Measure);
    }

    private static IReadOnlyList<string> DeserializeSharedRoles(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? new List<string>();
    }

    private record RepDefinitionRow(
        string Id, int TenantId, string Code, string Title, string DatasetKey,
        string DefinitionJson, string? ChartJson, string ViewType, string Orientation, string Visibility,
        string? SharedRolesJson,
        bool IsActive, string? CreatedBy, DateTime CreatedAt, string? UpdatedBy, DateTime UpdatedAt);

    private record DefinitionJsonDto(
        List<ColumnJsonDto> Columns, List<FilterJsonDto> Filters, List<string> GroupBy, List<SortJsonDto> Sort, List<KpiJsonDto> Kpis,
        List<CalcFieldJsonDto>? CalcFields = null);

    private record ColumnJsonDto(string Field, string Label, string? Agg, bool IsSubtotal);
    private record FilterJsonDto(string Field, string Op, List<string?> Value);
    private record SortJsonDto(string Field, bool Desc);
    private record KpiJsonDto(string Label, string Field, string Agg);
    private record ChartJsonDto(string Type, List<string> Dims, string Measure);
    private record CalcFieldJsonDto(string Key, string Label, string Formula, string DataType);
}
