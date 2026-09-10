using System.Text.Json;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.CLS;

/// <summary>
/// Handlers cho bo chi dinh mau CLS (BM-12). Bang diab_his_cls_order_templates.
/// MOI query/insert/delete deu filter tenant_id + doctor_id = ICurrentUser.UserId hien tai -
/// bac si A khong bao gio doc/xoa duoc bo mau cua bac si B (PO: "rieng tung bac si").
/// </summary>
internal static class ClsOrderTemplateSql
{
    public const string Table = "diab_his_cls_order_templates";

    public static object? TryParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<object>(json); } catch { return null; }
    }
}

public class CreateClsOrderTemplateCommandHandler
    : IRequestHandler<CreateClsOrderTemplateCommand, Result<ClsOrderTemplateResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateClsOrderTemplateCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<ClsOrderTemplateResponse>> Handle(CreateClsOrderTemplateCommand cmd, CancellationToken ct)
    {
        var name = cmd.Request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return Result<ClsOrderTemplateResponse>.Failure("VALIDATION_ERROR", "Tên bộ chỉ định mẫu không được để trống");

        string itemsJson;
        try { itemsJson = JsonSerializer.Serialize(cmd.Request.Items); }
        catch { return Result<ClsOrderTemplateResponse>.Failure("VALIDATION_ERROR", "Danh sách dịch vụ không hợp lệ"); }

        if (itemsJson == "null" || itemsJson == "[]")
            return Result<ClsOrderTemplateResponse>.Failure("CLS_TEMPLATE_EMPTY", "Chưa chọn dịch vụ nào để lưu làm bộ mẫu");

        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var doctorId = _user.UserId?.ToString();
        if (string.IsNullOrEmpty(doctorId))
            return Result<ClsOrderTemplateResponse>.Failure("UNAUTHORIZED", "Không xác định được người dùng hiện tại");

        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync($@"
            INSERT INTO {ClsOrderTemplateSql.Table}
                (id, tenant_id, doctor_id, name, items_json, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @TId, @DoctorId, @Name, @Items, @Now, @Uid, @Now, @Uid)",
            new { Id = id, TId = tid, DoctorId = doctorId, Name = name, Items = itemsJson, Now = now, Uid = doctorId });

        await _audit.LogAsync("CREATE", "ClsOrderTemplate", id, new { name }, ct);

        return Result<ClsOrderTemplateResponse>.Success(new ClsOrderTemplateResponse(
            Guid.Parse(id), name, cmd.Request.Items, now));
    }
}

public class ListClsOrderTemplatesQueryHandler
    : IRequestHandler<ListClsOrderTemplatesQuery, Result<ClsOrderTemplateListResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public ListClsOrderTemplatesQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<ClsOrderTemplateListResponse>> Handle(ListClsOrderTemplatesQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var doctorId = _user.UserId?.ToString();
        if (string.IsNullOrEmpty(doctorId))
            return Result<ClsOrderTemplateListResponse>.Failure("UNAUTHORIZED", "Không xác định được người dùng hiện tại");

        var rows = await conn.QueryAsync<dynamic>($@"
            SELECT id, name, items_json, created_at
            FROM {ClsOrderTemplateSql.Table}
            WHERE tenant_id=@TId AND doctor_id=@DoctorId AND deleted_at IS NULL
            ORDER BY created_at DESC",
            new { TId = tid, DoctorId = doctorId });

        var list = rows.Select(r => new ClsOrderTemplateResponse(
                Guid.Parse((string)r.id), (string)r.name,
                ClsOrderTemplateSql.TryParseJson((string)r.items_json), (DateTime)r.created_at))
            .ToList();

        return Result<ClsOrderTemplateListResponse>.Success(new ClsOrderTemplateListResponse(list));
    }
}

public class DeleteClsOrderTemplateCommandHandler : IRequestHandler<DeleteClsOrderTemplateCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public DeleteClsOrderTemplateCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result> Handle(DeleteClsOrderTemplateCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;
        var doctorId = _user.UserId?.ToString();
        var id = cmd.Id.ToString();

        // Chi xoa duoc bo mau CUA CHINH MINH (doctor_id=@DoctorId) - khong cho bac si khac
        // hoac vai tro nao xoa "ho", dung theo yeu cau PO "rieng tung bac si".
        var affected = await conn.ExecuteAsync($@"
            UPDATE {ClsOrderTemplateSql.Table}
            SET deleted_at=@Now, updated_at=@Now, updated_by=@Uid
            WHERE id=@Id AND tenant_id=@TId AND doctor_id=@DoctorId AND deleted_at IS NULL",
            new { Id = id, TId = tid, DoctorId = doctorId, Now = DateTime.UtcNow, Uid = doctorId });

        if (affected == 0)
            return Result.Failure("CLS_TEMPLATE_NOT_FOUND", "Không tìm thấy bộ chỉ định mẫu");

        await _audit.LogAsync("DELETE", "ClsOrderTemplate", id, null, ct);
        return Result.Success();
    }
}
