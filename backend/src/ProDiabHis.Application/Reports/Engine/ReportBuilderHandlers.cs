using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reports.Engine;

// ---- GET /reports/datasets ---- //
public class GetReportDatasetsHandler : IRequestHandler<GetReportDatasetsQuery, IReadOnlyList<Dataset>>
{
    private readonly IDatasetRegistry _datasets;

    public GetReportDatasetsHandler(IDatasetRegistry datasets) => _datasets = datasets;

    public Task<IReadOnlyList<Dataset>> Handle(GetReportDatasetsQuery request, CancellationToken ct)
        => Task.FromResult(_datasets.GetAll());
}

// ---- GET /reports/definitions ---- //
public class GetReportDefinitionsHandler : IRequestHandler<GetReportDefinitionsQuery, IReadOnlyList<ReportDefinition>>
{
    private readonly IReportDefinitionStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public GetReportDefinitionsHandler(IReportDefinitionStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<ReportDefinition>> Handle(GetReportDefinitionsQuery request, CancellationToken ct)
        => _store.GetVisibleAsync(_tenant.TenantId, _currentUser.UserId?.ToString(), ct);
}

// ---- POST /reports/definitions ---- //
public class CreateReportDefinitionHandler : IRequestHandler<CreateReportDefinitionCommand, ReportDefinition>
{
    private readonly IReportDefinitionStore _store;
    private readonly IDatasetRegistry _datasets;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public CreateReportDefinitionHandler(
        IReportDefinitionStore store, IDatasetRegistry datasets, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _datasets = datasets;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDefinition> Handle(CreateReportDefinitionCommand request, CancellationToken ct)
    {
        var validated = ReportDefinitionValidation.ValidateAndResolveDataset(_datasets, request.Input);
        _ = validated;

        var createdBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.CreateAsync(_tenant.TenantId, createdBy, request.Input, ct);
    }
}

// ---- PUT /reports/definitions/{id} ---- //
public class UpdateReportDefinitionHandler : IRequestHandler<UpdateReportDefinitionCommand, ReportDefinition>
{
    private readonly IReportDefinitionStore _store;
    private readonly IDatasetRegistry _datasets;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public UpdateReportDefinitionHandler(
        IReportDefinitionStore store, IDatasetRegistry datasets, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _datasets = datasets;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDefinition> Handle(UpdateReportDefinitionCommand request, CancellationToken ct)
    {
        ReportDefinitionValidation.ValidateAndResolveDataset(_datasets, request.Input);

        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_NOT_FOUND", "Không tìm thấy báo cáo tự tạo");

        ReportDefinitionValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        var updatedBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.UpdateAsync(_tenant.TenantId, request.Id, updatedBy, request.Input, ct);
    }
}

// ---- DELETE /reports/definitions/{id} ---- //
public class DeleteReportDefinitionHandler : IRequestHandler<DeleteReportDefinitionCommand>
{
    private readonly IReportDefinitionStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public DeleteReportDefinitionHandler(IReportDefinitionStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReportDefinitionCommand request, CancellationToken ct)
    {
        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_NOT_FOUND", "Không tìm thấy báo cáo tự tạo");

        ReportDefinitionValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        await _store.DeleteAsync(_tenant.TenantId, request.Id, ct);
    }
}

// ---- POST /reports/preview ---- //
public class PreviewReportDefinitionHandler : IRequestHandler<PreviewReportDefinitionQuery, ReportDataResult>
{
    private readonly IDatasetRegistry _datasets;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public PreviewReportDefinitionHandler(IDatasetRegistry datasets, IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _datasets = datasets;
        _db = db;
        _tenant = tenant;
    }

    public async Task<ReportDataResult> Handle(PreviewReportDefinitionQuery request, CancellationToken ct)
    {
        var dataset = _datasets.GetByKey(request.DatasetKey)
            ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Dataset '{request.DatasetKey}' không tồn tại");

        var input = new ReportDefinitionInput(
            "Xem trước", request.DatasetKey, request.Columns, request.Filters, request.GroupBy, request.Sort,
            request.Kpis, Chart: null, ReportViewType.Table, ReportVisibility.Private);

        var previewDefinition = new ReportDefinition(
            Id: "preview", TenantId: _tenant.TenantId, Code: "preview", Title: "Xem trước",
            DatasetKey: request.DatasetKey, Columns: request.Columns, Filters: request.Filters,
            GroupBy: request.GroupBy, Sort: request.Sort, Kpis: request.Kpis, Chart: null,
            ViewType: ReportViewType.Table, Visibility: ReportVisibility.Private, IsActive: true,
            CreatedBy: null, CreatedAt: DateTime.UtcNow, UpdatedBy: null, UpdatedAt: DateTime.UtcNow);

        var descriptor = DynamicDescriptorFactory.Create(previewDefinition, dataset, DynamicDescriptorFactory.PreviewLimit);

        var ctx = new ReportQueryContext(_tenant.TenantId, request.From, request.To, new Dictionary<string, string?>());
        var (sql, parameters) = descriptor.BuildQuery(ctx);

        using var conn = (IDbConnection)_db.CreateConnection();
        var rawRows = await conn.QueryAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));

        var rows = rawRows
            .Select(r =>
            {
                var src = (IDictionary<string, object>)r;
                var dict = new Dictionary<string, object?>(src.Count);
                foreach (var kv in src) dict[kv.Key] = kv.Value;
                return (IDictionary<string, object?>)dict;
            })
            .ToList();

        var subtotalKeys = descriptor.Columns.Where(c => c.IsGroupSubtotal).Select(c => c.Key).ToList();

        decimal SumColumn(IEnumerable<IDictionary<string, object?>> src, string key)
            => src.Sum(row => ReportValueConverter.ToDecimal(row.TryGetValue(key, out var v) ? v : null));

        var grandTotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(rows, k));
        var kpiResults = descriptor.Kpis
            .Select(k => new ReportKpiResult(k.Label, k.Tint, k.Compute(rows), k.IsMoney))
            .ToList();

        return new ReportDataResult(descriptor.Columns, Groups: null, Rows: rows, Totals: grandTotals, Kpis: kpiResults, TotalRows: rows.Count);
    }
}

/// <summary>Validate chung dung boi Create/Update handler — dataset ton tai + SafeQueryBuilder chap nhan
/// definition (build thu 1 lan voi ngay hom nay de bat loi field/agg/filter/operator lang som, KHONG luu
/// ket qua) + kiem tra quyen so huu khi sua/xoa.</summary>
internal static class ReportDefinitionValidation
{
    public static Dataset ValidateAndResolveDataset(IDatasetRegistry datasets, ReportDefinitionInput input)
    {
        var dataset = datasets.GetByKey(input.DatasetKey)
            ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Dataset '{input.DatasetKey}' không tồn tại");

        if (string.IsNullOrWhiteSpace(input.Title))
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Tên báo cáo không được để trống");

        // Build thu voi khoang ngay hop le (hom nay) de bat sai field/agg/operator ngay khi luu, khong doi den luc chay.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SafeQueryBuilder.Build(dataset, input, new ReportQueryContext(1, today, today, new Dictionary<string, string?>()), DynamicDescriptorFactory.PreviewLimit);

        if (input.Kpis is not null)
        {
            foreach (var kpi in input.Kpis)
            {
                var matched = input.Columns.Any(c => c.Agg == kpi.Agg && string.Equals(c.Field, kpi.Field, StringComparison.OrdinalIgnoreCase));
                if (!matched)
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID",
                        $"KPI '{kpi.Label}' phải tham chiếu 1 cột số đo đã chọn trong báo cáo (field='{kpi.Field}', agg='{kpi.Agg}')");
            }
        }

        return dataset;
    }

    public static void EnsureOwnerOrAdmin(ReportDefinition existing, ICurrentUser currentUser)
    {
        var userId = currentUser.UserId?.ToString();
        var isOwner = userId is not null && string.Equals(existing.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);
        var isAdmin = currentUser.Roles.Any(r =>
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quan tri", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
            throw new CrossTenantAccessException("REPORT_DEFINITION_FORBIDDEN", "Chỉ chủ sở hữu hoặc quản trị viên mới được sửa/xoá báo cáo này");
    }
}
