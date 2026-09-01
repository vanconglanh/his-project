using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Settings;

// ────────────────────────────────────────────────
// DTOs
// ────────────────────────────────────────────────
public record AdminSettingDto(
    string Key, string LabelVi, string? DescriptionVi, string DataType, string ValueGroup,
    bool IsPublic, string? DefaultValue, string? Value, bool IsOverridden);

// ────────────────────────────────────────────────
// Queries / Commands
// ────────────────────────────────────────────────
public record GetPublicSettingsQuery() : IRequest<Result<IReadOnlyDictionary<string, string?>>>;

public record GetAdminSettingsQuery() : IRequest<Result<IReadOnlyList<AdminSettingDto>>>;

public record UpdateSettingCommand(string Key, string Value) : IRequest<Result>;

public record DeleteSettingOverrideCommand(string Key) : IRequest<Result>;

// ────────────────────────────────────────────────
// GET /settings/public — chi key co is_public=1, moi user dang nhap doc duoc
// ────────────────────────────────────────────────
public class GetPublicSettingsQueryHandler : IRequestHandler<GetPublicSettingsQuery, Result<IReadOnlyDictionary<string, string?>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ISettingsProvider _settings;

    public GetPublicSettingsQueryHandler(IDapperConnectionFactory db, ISettingsProvider settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<Result<IReadOnlyDictionary<string, string?>>> Handle(GetPublicSettingsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var keys = (await conn.QueryAsync<string>(
            "SELECT setting_key FROM diab_his_sys_setting_meta WHERE is_public = 1")).ToList();

        var result = new Dictionary<string, string?>();
        foreach (var key in keys)
            result[key] = await _settings.GetRawAsync(key, ct);

        return Result<IReadOnlyDictionary<string, string?>>.Success(result);
    }
}

// ────────────────────────────────────────────────
// GET /admin/settings — full list + value resolve tenant>global>default
// ────────────────────────────────────────────────
public class GetAdminSettingsQueryHandler : IRequestHandler<GetAdminSettingsQuery, Result<IReadOnlyList<AdminSettingDto>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ISettingsProvider _settings;
    private readonly ITenantProvider _tenant;

    public GetAdminSettingsQueryHandler(IDapperConnectionFactory db, ISettingsProvider settings, ITenantProvider tenant)
    {
        _db = db;
        _settings = settings;
        _tenant = tenant;
    }

    public async Task<Result<IReadOnlyList<AdminSettingDto>>> Handle(GetAdminSettingsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var metas = (await conn.QueryAsync<dynamic>(@"
            SELECT setting_key, label_vi, description_vi, data_type, value_group, is_public, default_value
            FROM diab_his_sys_setting_meta
            ORDER BY value_group, sort_order, setting_key")).ToList();

        var overriddenKeys = (await conn.QueryAsync<string>(
            "SELECT setting_key FROM diab_his_sys_settings WHERE tenant_id = @TenantId",
            new { TenantId = _tenant.TenantId })).ToHashSet(StringComparer.Ordinal);

        var result = new List<AdminSettingDto>();
        foreach (var m in metas)
        {
            string key = (string)m.setting_key;
            string? defaultValue = m.default_value is null ? null : (string)m.default_value;
            var value = await _settings.GetRawAsync(key, ct) ?? defaultValue;

            result.Add(new AdminSettingDto(
                key,
                (string)m.label_vi,
                m.description_vi is null ? null : (string)m.description_vi,
                (string)m.data_type,
                (string)m.value_group,
                Convert.ToBoolean(m.is_public),
                defaultValue,
                value,
                overriddenKeys.Contains(key)
            ));
        }

        return Result<IReadOnlyList<AdminSettingDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// PUT /admin/settings/{key}
// ────────────────────────────────────────────────
public class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ISettingsProvider _settings;

    public UpdateSettingCommandHandler(IDapperConnectionFactory db, ISettingsProvider settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<Result> Handle(UpdateSettingCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var meta = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT data_type FROM diab_his_sys_setting_meta WHERE setting_key = @Key", new { cmd.Key });

        if (meta is null)
            return Result.Failure("SETTING_KEY_NOT_FOUND", "Không tìm thấy khoá cấu hình");

        string dataType = (string)meta.data_type;
        if (!IsValidValue(dataType, cmd.Value))
            return Result.Failure("SETTING_VALUE_INVALID", $"Giá trị không hợp lệ cho kiểu dữ liệu '{dataType}'");

        await _settings.SetAsync(cmd.Key, cmd.Value, ct);
        return Result.Success();
    }

    private static bool IsValidValue(string dataType, string value) => dataType switch
    {
        "int" => int.TryParse(value, out _),
        "decimal" => decimal.TryParse(value, out _),
        "bool" => bool.TryParse(value, out _),
        _ => true // string: khong rang buoc
    };
}

// ────────────────────────────────────────────────
// DELETE /admin/settings/{key} — xoa override tenant, revert ve global
// ────────────────────────────────────────────────
public class DeleteSettingOverrideCommandHandler : IRequestHandler<DeleteSettingOverrideCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public DeleteSettingOverrideCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(DeleteSettingOverrideCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var meta = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_setting_meta WHERE setting_key = @Key", new { cmd.Key });
        if (meta == 0)
            return Result.Failure("SETTING_KEY_NOT_FOUND", "Không tìm thấy khoá cấu hình");

        await conn.ExecuteAsync(
            "DELETE FROM diab_his_sys_settings WHERE setting_key = @Key AND tenant_id = @TenantId",
            new { cmd.Key, TenantId = _tenant.TenantId });

        return Result.Success();
    }
}
