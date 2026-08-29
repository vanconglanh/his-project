using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Doc + giai ma config kenh thong bao (SMS/Zalo ZNS) per-tenant/branch tu
/// <c>diab_his_int_notification_channels</c> (giong pattern <c>DtqgCredentialProvider</c>).
/// Uu tien dong khop branch hien tai, fallback branch_id NULL (dung chung tenant).
/// Moi lan goi deu doc lai DB -> doi/reset credential qua UI co hieu luc ngay, khong cache lau.
/// </summary>
public class NotificationChannelCredentialProvider : INotificationChannelCredentialProvider
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<NotificationChannelCredentialProvider> _logger;

    public NotificationChannelCredentialProvider(
        IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch,
        IEncryptionService encryption, ILogger<NotificationChannelCredentialProvider> logger)
    {
        _db = db; _currentUser = currentUser; _branch = branch; _encryption = encryption; _logger = logger;
    }

    public Task<NotificationChannelConfig?> GetForCurrentAsync(NotificationChannel channel, CancellationToken ct = default)
    {
        if (_currentUser.TenantId is null) return Task.FromResult<NotificationChannelConfig?>(null);
        return GetAsync(_currentUser.TenantId.Value, _branch.BranchId > 0 ? _branch.BranchId : (int?)null, channel, ct);
    }

    public async Task<NotificationChannelConfig?> GetAsync(int tenantId, int? branchId, NotificationChannel channel, CancellationToken ct = default)
    {
        var channelDb = channel == NotificationChannel.ZaloZns ? "ZALO_ZNS" : "SMS";
        using var conn = (IDbConnection)_db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync<Row>(
            @"SELECT provider AS Provider, config_encrypted AS ConfigEncrypted
                FROM diab_his_int_notification_channels
               WHERE tenant_id = @tenantId AND channel = @channel AND is_active = 1 AND deleted_at IS NULL
                 AND (branch_id = @branchId OR branch_id IS NULL)
               ORDER BY (branch_id = @branchId) DESC
               LIMIT 1",
            new { tenantId, channel = channelDb, branchId });

        if (row is null) return null;

        Dictionary<string, string> config;
        try
        {
            var json = string.IsNullOrWhiteSpace(row.ConfigEncrypted) ? "{}" : _encryption.Decrypt(row.ConfigEncrypted);
            config = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification: giai ma config that bai cho tenant {TenantId} channel {Channel}", tenantId, channelDb);
            return null;
        }

        return new NotificationChannelConfig(channel, row.Provider ?? "", config);
    }

    private sealed class Row
    {
        public string? Provider { get; set; }
        public string? ConfigEncrypted { get; set; }
    }
}
