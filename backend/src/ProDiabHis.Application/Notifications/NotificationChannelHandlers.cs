using System.Data;
using System.Text.Json;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Notifications;

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record ListNotificationChannelsQuery : IRequest<Result<List<NotificationChannelResponse>>>;
public record GetNotificationChannelQuery(string Id) : IRequest<Result<NotificationChannelResponse>>;
public record CreateNotificationChannelCommand(NotificationChannelRequest Request) : IRequest<Result<NotificationChannelResponse>>;
public record UpdateNotificationChannelCommand(string Id, NotificationChannelRequest Request) : IRequest<Result<NotificationChannelResponse>>;
public record DeleteNotificationChannelCommand(string Id) : IRequest<Result<bool>>;
public record TestNotificationChannelCommand(string Id) : IRequest<Result<NotificationChannelTestResult>>;

/// <summary>Helpers dung chung: parse channel enum, che gia tri nhay cam, map row -> response.</summary>
internal static class NotificationChannelMapper
{
    // Cac key duoc coi la nhay cam -> che khi tra ve; con lai (endpoint, brand_name, oa_id,
    // template_id, sms_type...) tra plaintext de UI hien thi/sua lai duoc.
    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "api_key", "apikey", "secret", "secret_key", "secretkey", "access_token", "accesstoken",
        "token", "app_secret", "appsecret", "password"
    };

    public static bool TryParseChannel(string? raw, out NotificationChannel channel)
    {
        switch ((raw ?? "").Trim().ToUpperInvariant())
        {
            case "SMS": channel = NotificationChannel.Sms; return true;
            case "ZALO_ZNS": case "ZALOZNS": case "ZNS": channel = NotificationChannel.ZaloZns; return true;
            default: channel = NotificationChannel.Sms; return false;
        }
    }

    public static string ToDbChannel(NotificationChannel c) => c == NotificationChannel.ZaloZns ? "ZALO_ZNS" : "SMS";

    public static Dictionary<string, string> Mask(IReadOnlyDictionary<string, string> config)
    {
        var masked = new Dictionary<string, string>();
        foreach (var (k, v) in config)
        {
            if (SecretKeys.Contains(k) && !string.IsNullOrEmpty(v))
                masked[k] = v.Length > 4 ? "****" + v[^4..] : "****";
            else
                masked[k] = v;
        }
        return masked;
    }
}

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class ListNotificationChannelsHandler : IRequestHandler<ListNotificationChannelsQuery, Result<List<NotificationChannelResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;
    private readonly IEncryptionService _encryption;

    public ListNotificationChannelsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch, IEncryptionService encryption)
    { _db = db; _currentUser = currentUser; _branch = branch; _encryption = encryption; }

    public async Task<Result<List<NotificationChannelResponse>>> Handle(ListNotificationChannelsQuery q, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<List<NotificationChannelResponse>>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");
        var tenantId = _currentUser.TenantId.Value;

        using var conn = (IDbConnection)_db.CreateConnection();
        // Tra ve kenh cua branch hien tai + kenh dung chung (branch_id NULL).
        var rows = await conn.QueryAsync<ChannelRow>(
            @"SELECT id, tenant_id, branch_id, channel, provider, config_encrypted, is_active,
                     last_tested_at, last_test_ok, created_at, updated_at
                FROM diab_his_int_notification_channels
               WHERE tenant_id = @tenantId AND deleted_at IS NULL
                 AND (@branchId <= 0 OR branch_id = @branchId OR branch_id IS NULL)
               ORDER BY channel ASC, (branch_id IS NULL) ASC",
            new { tenantId, branchId = _branch.BranchId });

        var items = rows.Select(r => MapRow(r, _encryption)).ToList();
        return Result<List<NotificationChannelResponse>>.Success(items);
    }

    internal static NotificationChannelResponse MapRow(ChannelRow r, IEncryptionService enc)
    {
        Dictionary<string, string> config;
        try
        {
            var json = string.IsNullOrWhiteSpace(r.config_encrypted) ? "{}" : enc.Decrypt(r.config_encrypted);
            config = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch { config = new(); }

        return new NotificationChannelResponse(
            r.id, r.tenant_id, r.branch_id, r.channel, r.provider,
            NotificationChannelMapper.Mask(config),
            r.is_active == 1, r.last_tested_at, r.last_test_ok == 1, r.created_at, r.updated_at);
    }

    internal sealed class ChannelRow
    {
        public string id { get; set; } = "";
        public int tenant_id { get; set; }
        public int? branch_id { get; set; }
        public string channel { get; set; } = "";
        public string provider { get; set; } = "";
        public string? config_encrypted { get; set; }
        public int is_active { get; set; }
        public DateTime? last_tested_at { get; set; }
        public int last_test_ok { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}

public class GetNotificationChannelHandler : IRequestHandler<GetNotificationChannelQuery, Result<NotificationChannelResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;

    public GetNotificationChannelHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IEncryptionService encryption)
    { _db = db; _currentUser = currentUser; _encryption = encryption; }

    public async Task<Result<NotificationChannelResponse>> Handle(GetNotificationChannelQuery q, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<NotificationChannelResponse>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");

        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<ListNotificationChannelsHandler.ChannelRow>(
            @"SELECT id, tenant_id, branch_id, channel, provider, config_encrypted, is_active,
                     last_tested_at, last_test_ok, created_at, updated_at
                FROM diab_his_int_notification_channels
               WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.Id, tenantId = _currentUser.TenantId.Value });

        if (row is null)
            return Result<NotificationChannelResponse>.Failure("NOTIFICATION_CHANNEL_NOT_FOUND", "Khong tim thay kenh thong bao.");

        return Result<NotificationChannelResponse>.Success(
            ListNotificationChannelsHandler.MapRow(row, _encryption));
    }
}

public class CreateNotificationChannelHandler : IRequestHandler<CreateNotificationChannelCommand, Result<NotificationChannelResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;
    private readonly IEncryptionService _encryption;

    public CreateNotificationChannelHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch, IEncryptionService encryption)
    { _db = db; _currentUser = currentUser; _branch = branch; _encryption = encryption; }

    public async Task<Result<NotificationChannelResponse>> Handle(CreateNotificationChannelCommand cmd, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<NotificationChannelResponse>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");
        var tenantId = _currentUser.TenantId.Value;

        if (!NotificationChannelMapper.TryParseChannel(cmd.Request.Channel, out _))
            return Result<NotificationChannelResponse>.Failure("NOTIFICATION_CHANNEL_INVALID", "Loai kenh khong hop le (SMS hoac ZALO_ZNS).");
        if (string.IsNullOrWhiteSpace(cmd.Request.Provider))
            return Result<NotificationChannelResponse>.Failure("NOTIFICATION_PROVIDER_REQUIRED", "Vui long chon nha cung cap.");

        var channelDb = cmd.Request.Channel.Trim().ToUpperInvariant() == "SMS" ? "SMS" : "ZALO_ZNS";
        var branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null;

        using var conn = (IDbConnection)_db.CreateConnection();

        // 1 kenh/loai/scope: neu da co (chua xoa) -> bao trung, huong dan sua thay vi tao moi.
        var dup = await conn.ExecuteScalarAsync<string?>(
            @"SELECT id FROM diab_his_int_notification_channels
               WHERE tenant_id = @tenantId AND channel = @channel AND deleted_at IS NULL
                 AND ((@branchId IS NULL AND branch_id IS NULL) OR branch_id = @branchId)",
            new { tenantId, channel = channelDb, branchId });
        if (dup != null)
            return Result<NotificationChannelResponse>.Failure("NOTIFICATION_CHANNEL_DUPLICATE", "Da ton tai cau hinh cho kenh nay o chi nhanh hien tai. Vui long sua cau hinh dang co.");

        var id = Guid.NewGuid().ToString();
        var configJson = JsonSerializer.Serialize(cmd.Request.Config ?? new());
        var configEncrypted = _encryption.Encrypt(configJson);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_int_notification_channels
                (id, tenant_id, branch_id, channel, provider, config_encrypted, is_active, created_at, created_by, updated_at, updated_by)
              VALUES (@id, @tenantId, @branchId, @channel, @provider, @config, @isActive, NOW(), @userId, NOW(), @userId)",
            new
            {
                id, tenantId, branchId, channel = channelDb, provider = cmd.Request.Provider.Trim().ToUpperInvariant(),
                config = configEncrypted, isActive = cmd.Request.IsActive ? 1 : 0,
                userId = (int?)null
            });

        return await new GetNotificationChannelHandler(_db, _currentUser, _encryption)
            .Handle(new GetNotificationChannelQuery(id), ct);
    }
}

public class UpdateNotificationChannelHandler : IRequestHandler<UpdateNotificationChannelCommand, Result<NotificationChannelResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;

    public UpdateNotificationChannelHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IEncryptionService encryption)
    { _db = db; _currentUser = currentUser; _encryption = encryption; }

    public async Task<Result<NotificationChannelResponse>> Handle(UpdateNotificationChannelCommand cmd, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<NotificationChannelResponse>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");
        var tenantId = _currentUser.TenantId.Value;

        using var conn = (IDbConnection)_db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<ListNotificationChannelsHandler.ChannelRow>(
            @"SELECT id, tenant_id, branch_id, channel, provider, config_encrypted, is_active,
                     last_tested_at, last_test_ok, created_at, updated_at
                FROM diab_his_int_notification_channels
               WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (existing is null)
            return Result<NotificationChannelResponse>.Failure("NOTIFICATION_CHANNEL_NOT_FOUND", "Khong tim thay kenh thong bao.");

        // Merge config: giu nguyen gia tri cu cho nhung key nhay cam bi bo trong/masked
        // (UI khong gui lai secret cu -> khong ghi de bang chuoi rong).
        Dictionary<string, string> oldConfig;
        try
        {
            var oldJson = string.IsNullOrWhiteSpace(existing.config_encrypted) ? "{}" : _encryption.Decrypt(existing.config_encrypted);
            oldConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(oldJson) ?? new();
        }
        catch { oldConfig = new(); }

        var merged = new Dictionary<string, string>(oldConfig);
        foreach (var (k, v) in cmd.Request.Config ?? new())
        {
            if (string.IsNullOrEmpty(v) || v.StartsWith("****")) continue; // giu gia tri cu
            merged[k] = v;
        }

        var configEncrypted = _encryption.Encrypt(JsonSerializer.Serialize(merged));

        await conn.ExecuteAsync(
            @"UPDATE diab_his_int_notification_channels
                 SET provider = @provider, config_encrypted = @config, is_active = @isActive,
                     updated_at = NOW()
               WHERE id = @id AND tenant_id = @tenantId",
            new
            {
                id = cmd.Id, tenantId,
                provider = string.IsNullOrWhiteSpace(cmd.Request.Provider) ? existing.provider : cmd.Request.Provider.Trim().ToUpperInvariant(),
                config = configEncrypted, isActive = cmd.Request.IsActive ? 1 : 0
            });

        return await new GetNotificationChannelHandler(_db, _currentUser, _encryption)
            .Handle(new GetNotificationChannelQuery(cmd.Id), ct);
    }
}

public class DeleteNotificationChannelHandler : IRequestHandler<DeleteNotificationChannelCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeleteNotificationChannelHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<bool>> Handle(DeleteNotificationChannelCommand cmd, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<bool>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");

        using var conn = (IDbConnection)_db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_int_notification_channels
                 SET deleted_at = NOW(), is_active = 0, updated_at = NOW()
               WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId = _currentUser.TenantId.Value });

        if (affected == 0)
            return Result<bool>.Failure("NOTIFICATION_CHANNEL_NOT_FOUND", "Khong tim thay kenh thong bao.");
        return Result<bool>.Success(true);
    }
}

public class TestNotificationChannelHandler : IRequestHandler<TestNotificationChannelCommand, Result<NotificationChannelTestResult>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationSender _sender;

    public TestNotificationChannelHandler(IDapperConnectionFactory db, ICurrentUser currentUser, INotificationSender sender)
    { _db = db; _currentUser = currentUser; _sender = sender; }

    public async Task<Result<NotificationChannelTestResult>> Handle(TestNotificationChannelCommand cmd, CancellationToken ct)
    {
        if (_currentUser.TenantId is null)
            return Result<NotificationChannelTestResult>.Failure("TENANT_REQUIRED", "Khong xac dinh duoc tenant.");
        var tenantId = _currentUser.TenantId.Value;

        using var conn = (IDbConnection)_db.CreateConnection();
        var raw = await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT channel FROM diab_his_int_notification_channels WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id, tenantId });
        if (raw is null)
            return Result<NotificationChannelTestResult>.Failure("NOTIFICATION_CHANNEL_NOT_FOUND", "Khong tim thay kenh thong bao.");

        NotificationChannelMapper.TryParseChannel(raw, out var channel);
        var testResult = await _sender.TestConnectionAsync(channel, ct);

        var ok = testResult.IsSuccess && testResult.Value;
        await conn.ExecuteAsync(
            "UPDATE diab_his_int_notification_channels SET last_tested_at = NOW(), last_test_ok = @ok, updated_at = NOW() WHERE id = @id",
            new { ok = ok ? 1 : 0, id = cmd.Id });

        if (!ok)
            return Result<NotificationChannelTestResult>.Success(
                new NotificationChannelTestResult(false, testResult.ErrorMessage ?? "Ket noi that bai."));

        return Result<NotificationChannelTestResult>.Success(new NotificationChannelTestResult(true, "Ket noi thanh cong."));
    }
}
