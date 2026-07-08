using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reports.Engine;

// ---- GET /reports/dashboards ---- //
public class GetReportDashboardsHandler : IRequestHandler<GetReportDashboardsQuery, IReadOnlyList<ReportDashboard>>
{
    private readonly IReportDashboardStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public GetReportDashboardsHandler(IReportDashboardStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<ReportDashboard>> Handle(GetReportDashboardsQuery request, CancellationToken ct)
        => _store.GetVisibleAsync(_tenant.TenantId, _currentUser.UserId?.ToString(), ct);
}

// ---- GET /reports/dashboards/{id} ---- //
public class GetReportDashboardByIdHandler : IRequestHandler<GetReportDashboardByIdQuery, ReportDashboard>
{
    private readonly IReportDashboardStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public GetReportDashboardByIdHandler(IReportDashboardStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDashboard> Handle(GetReportDashboardByIdQuery request, CancellationToken ct)
    {
        var dashboard = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_DASHBOARD_NOT_FOUND", "Không tìm thấy dashboard");

        ReportDashboardValidation.EnsureVisible(dashboard, _currentUser);
        return dashboard;
    }
}

// ---- POST /reports/dashboards ---- //
public class CreateReportDashboardHandler : IRequestHandler<CreateReportDashboardCommand, ReportDashboard>
{
    private readonly IReportDashboardStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public CreateReportDashboardHandler(IReportDashboardStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDashboard> Handle(CreateReportDashboardCommand request, CancellationToken ct)
    {
        var createdBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.CreateAsync(_tenant.TenantId, createdBy, request.Input, ct);
    }
}

// ---- PUT /reports/dashboards/{id} ---- //
public class UpdateReportDashboardHandler : IRequestHandler<UpdateReportDashboardCommand, ReportDashboard>
{
    private readonly IReportDashboardStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public UpdateReportDashboardHandler(IReportDashboardStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDashboard> Handle(UpdateReportDashboardCommand request, CancellationToken ct)
    {
        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_DASHBOARD_NOT_FOUND", "Không tìm thấy dashboard");

        ReportDashboardValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        var updatedBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.UpdateAsync(_tenant.TenantId, request.Id, updatedBy, request.Input, ct);
    }
}

// ---- DELETE /reports/dashboards/{id} ---- //
public class DeleteReportDashboardHandler : IRequestHandler<DeleteReportDashboardCommand>
{
    private readonly IReportDashboardStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public DeleteReportDashboardHandler(IReportDashboardStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReportDashboardCommand request, CancellationToken ct)
    {
        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_DASHBOARD_NOT_FOUND", "Không tìm thấy dashboard");

        ReportDashboardValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        await _store.DeleteAsync(_tenant.TenantId, request.Id, ct);
    }
}

// ---- GET /reports/dashboards/{id}/data ---- //
public class GetReportDashboardDataHandler : IRequestHandler<GetReportDashboardDataQuery, ReportDashboardDataResult>
{
    /// <summary>LIMIT du lieu moi widget — giong pageSize mac dinh cua 1 bao cao thuong (GET /{code}/data).</summary>
    private const int WidgetPageSize = 100;

    private readonly IReportDashboardStore _store;
    private readonly IGenericReportDataService _dataSvc;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public GetReportDashboardDataHandler(
        IReportDashboardStore store, IGenericReportDataService dataSvc, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _dataSvc = dataSvc;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportDashboardDataResult> Handle(GetReportDashboardDataQuery request, CancellationToken ct)
    {
        var dashboard = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_DASHBOARD_NOT_FOUND", "Không tìm thấy dashboard");

        ReportDashboardValidation.EnsureVisible(dashboard, _currentUser);

        var widgetResults = new List<ReportDashboardWidgetData>();
        foreach (var widget in dashboard.Widgets)
        {
            // Tai su dung dung 1 pipeline du lieu voi bao cao thuong (GenericReportDataService) — bao gom
            // tenant isolation (descriptor resolve qua IReportRegistry da scope tenant) + validate khoang ngay.
            var payload = await _dataSvc.GetDataAsync(widget.ReportCode, request.From, request.To,
                new Dictionary<string, string?>(), 1, WidgetPageSize, ct);

            widgetResults.Add(new ReportDashboardWidgetData(widget.ReportCode, widget.Title, widget.WidgetType, payload));
        }

        return new ReportDashboardDataResult(dashboard.Title, widgetResults);
    }
}

/// <summary>Validate quyen truy cap/sua dashboard — giong pattern ReportDefinitionValidation (bao cao tu tao).</summary>
internal static class ReportDashboardValidation
{
    public static void EnsureVisible(ReportDashboard dashboard, ICurrentUser currentUser)
    {
        if (dashboard.Visibility == ReportVisibility.Tenant) return;

        var userId = currentUser.UserId?.ToString();
        var isOwner = userId is not null && string.Equals(dashboard.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);
        if (!isOwner)
            throw new CrossTenantAccessException("REPORT_DASHBOARD_FORBIDDEN", "Không có quyền xem dashboard này");
    }

    public static void EnsureOwnerOrAdmin(ReportDashboard existing, ICurrentUser currentUser)
    {
        var userId = currentUser.UserId?.ToString();
        var isOwner = userId is not null && string.Equals(existing.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);
        var isAdmin = currentUser.Roles.Any(r =>
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quan tri", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
            throw new CrossTenantAccessException("REPORT_DASHBOARD_FORBIDDEN", "Chỉ chủ sở hữu hoặc quản trị viên mới được sửa/xoá dashboard này");
    }
}
