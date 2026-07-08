using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reports.Engine;

// ---- GET /reports/schedules ---- //
public class GetReportSchedulesHandler : IRequestHandler<GetReportSchedulesQuery, IReadOnlyList<ReportSchedule>>
{
    private readonly IReportScheduleStore _store;
    private readonly ITenantProvider _tenant;

    public GetReportSchedulesHandler(IReportScheduleStore store, ITenantProvider tenant)
    {
        _store = store;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<ReportSchedule>> Handle(GetReportSchedulesQuery request, CancellationToken ct)
        => _store.GetAllAsync(_tenant.TenantId, ct);
}

// ---- POST /reports/schedules ---- //
public class CreateReportScheduleHandler : IRequestHandler<CreateReportScheduleCommand, ReportSchedule>
{
    private readonly IReportScheduleStore _store;
    private readonly IReportRegistry _registry;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public CreateReportScheduleHandler(IReportScheduleStore store, IReportRegistry registry, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _registry = registry;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportSchedule> Handle(CreateReportScheduleCommand request, CancellationToken ct)
    {
        ReportScheduleValidation.EnsureReportCodeExists(_registry, request.Input.ReportCode);

        var createdBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.CreateAsync(_tenant.TenantId, createdBy, request.Input, ct);
    }
}

// ---- PUT /reports/schedules/{id} ---- //
public class UpdateReportScheduleHandler : IRequestHandler<UpdateReportScheduleCommand, ReportSchedule>
{
    private readonly IReportScheduleStore _store;
    private readonly IReportRegistry _registry;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public UpdateReportScheduleHandler(IReportScheduleStore store, IReportRegistry registry, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _registry = registry;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ReportSchedule> Handle(UpdateReportScheduleCommand request, CancellationToken ct)
    {
        ReportScheduleValidation.EnsureReportCodeExists(_registry, request.Input.ReportCode);

        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_SCHEDULE_NOT_FOUND", "Không tìm thấy lịch gửi báo cáo");

        ReportScheduleValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        var updatedBy = _currentUser.UserId?.ToString()
            ?? throw new ReportValidationException("REPORT_UNAUTHENTICATED", "Không xác định được người dùng hiện tại");

        return await _store.UpdateAsync(_tenant.TenantId, request.Id, updatedBy, request.Input, ct);
    }
}

// ---- DELETE /reports/schedules/{id} ---- //
public class DeleteReportScheduleHandler : IRequestHandler<DeleteReportScheduleCommand>
{
    private readonly IReportScheduleStore _store;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public DeleteReportScheduleHandler(IReportScheduleStore store, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _store = store;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReportScheduleCommand request, CancellationToken ct)
    {
        var existing = await _store.GetByIdAsync(_tenant.TenantId, request.Id, ct)
            ?? throw new ReportValidationException("REPORT_SCHEDULE_NOT_FOUND", "Không tìm thấy lịch gửi báo cáo");

        ReportScheduleValidation.EnsureOwnerOrAdmin(existing, _currentUser);

        await _store.DeleteAsync(_tenant.TenantId, request.Id, ct);
    }
}

internal static class ReportScheduleValidation
{
    /// <summary>Bat buoc report_code phai la 1 bao cao nguoi dung hien tai nhin thay duoc (code-defined
    /// hoac tu tao/da duoc chia se) tai thoi diem tao lich — tranh lich "treo" tro toi bao cao khong ton tai
    /// hoac khong duoc phep xem.</summary>
    public static void EnsureReportCodeExists(IReportRegistry registry, string reportCode)
    {
        if (registry.GetByCode(reportCode) is null)
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Không tìm thấy báo cáo '{reportCode}' hoặc bạn không có quyền xem báo cáo này");
    }

    public static void EnsureOwnerOrAdmin(ReportSchedule existing, ICurrentUser currentUser)
    {
        var userId = currentUser.UserId?.ToString();
        var isOwner = userId is not null && string.Equals(existing.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);
        var isAdmin = currentUser.Roles.Any(r =>
            r.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quản trị", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Quan tri", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
            throw new CrossTenantAccessException("REPORT_SCHEDULE_FORBIDDEN", "Chỉ chủ sở hữu hoặc quản trị viên mới được sửa/xoá lịch gửi này");
    }
}
