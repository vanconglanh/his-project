using System.Data;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Diabetes.Cgm;
using ProDiabHis.Infrastructure.Integrations.Cgm;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// FR-711 [P2]: Hangfire recurring job — poll dữ liệu đo đường huyết (CGM) định kỳ cho các bệnh nhân đã
/// liên kết tài khoản (diab_his_dev_cgm_links, status=ACTIVE), ghi vào diab_his_dev_cgm_readings.
///
/// Idempotency: UNIQUE KEY (tenant_id, patient_id, provider, device_id, reading_at) ở bảng readings —
/// insert trùng (lần sync sau lặp lại khoảng thời gian cũ) sẽ bị DB từ chối, job dùng INSERT IGNORE.
///
/// Ghi chú kiến trúc: ICgmDeviceProvider hiện chỉ có adapter Dexcom (xem DexcomCgmProvider — CHƯA có
/// sandbox thật, mọi lời gọi thực tế sẽ throw NotImplementedException nếu CgmProvider:Type != Dexcom
/// hoặc chưa cấu hình ClientId/ClientSecret). Job set Authorization header theo access_token RIÊNG của
/// từng bệnh nhân (giải mã từ access_token_enc) trước khi gọi FetchReadingsAsync — vì vậy tạo 1
/// HttpClient/provider instance MỚI cho mỗi bệnh nhân thay vì dùng instance dùng chung từ DI (tránh race
/// condition khi nhiều bệnh nhân xử lý tuần tự trong cùng 1 lần chạy job).
/// </summary>
public class CgmReadingsSyncJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IEncryptionService _enc;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DexcomCgmOptions _dexcomOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CgmReadingsSyncJob> _logger;
    private readonly string _cgmProviderType;

    public CgmReadingsSyncJob(
        IDapperConnectionFactory db,
        IEncryptionService enc,
        IHttpClientFactory httpClientFactory,
        DexcomCgmOptions dexcomOptions,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILoggerFactory loggerFactory,
        ILogger<CgmReadingsSyncJob> logger)
    {
        _db = db;
        _enc = enc;
        _httpClientFactory = httpClientFactory;
        _dexcomOptions = dexcomOptions;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _cgmProviderType = configuration["CgmProvider:Type"] ?? "None";
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (string.Equals(_cgmProviderType, "None", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("CgmReadingsSyncJob: CgmProvider:Type=None, bo qua (chua cau hinh nha cung cap CGM)");
            return;
        }

        _logger.LogInformation("CgmReadingsSyncJob started at {Time}", DateTime.UtcNow);
        using var conn = (IDbConnection)_db.CreateConnection();

        var links = (await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_dev_cgm_links
            WHERE deleted_at IS NULL AND status = 'ACTIVE' AND provider = @Provider",
            new { Provider = _cgmProviderType })).ToList();

        _logger.LogInformation("CgmReadingsSyncJob: {Count} lien ket CGM can dong bo", links.Count);

        foreach (var link in links)
        {
            try { await SyncOneAsync(conn, link, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CgmReadingsSyncJob: loi dong bo link {Id}", (string)link.id);
            }
        }

        _logger.LogInformation("CgmReadingsSyncJob finished");
    }

    private async Task SyncOneAsync(IDbConnection conn, dynamic link, CancellationToken ct)
    {
        string linkId = (string)link.id;
        int tenantId = (int)link.tenant_id;
        string patientId = (string)link.patient_id;
        string provider = (string)link.provider;
        var now = DateTime.UtcNow;

        if (link.access_token_enc is null)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_dev_cgm_links SET status='ERROR', last_sync_error=@Err, updated_at=@Now WHERE id=@Id",
                new { Err = "Chua co access token, benh nhan can lien ket lai", Now = now, Id = linkId });
            return;
        }

        string accessToken;
        try { accessToken = _enc.Decrypt(Encoding.UTF8.GetString((byte[])link.access_token_enc)); }
        catch
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_dev_cgm_links SET status='ERROR', last_sync_error='Loi giai ma token', updated_at=@Now WHERE id=@Id",
                new { Now = now, Id = linkId });
            return;
        }

        // TODO: khi co refresh flow that (Dexcom sandbox), kiem tra token_expires_at o day va tu refresh
        // bang refresh_token_enc truoc khi goi FetchReadingsAsync; hien tai chi danh dau EXPIRED de nhac
        // benh nhan lien ket lai qua Portal.
        if (link.token_expires_at is not null && (DateTime)link.token_expires_at < now)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_dev_cgm_links SET status='EXPIRED', last_sync_error='Access token het han', updated_at=@Now WHERE id=@Id",
                new { Now = now, Id = linkId });
            return;
        }

        var provider2 = CreateProviderWithAuthorizedClient(provider, accessToken);
        if (provider2 is null)
        {
            _logger.LogWarning("CgmReadingsSyncJob: provider {Provider} chua duoc ho tro, bo qua link {Id}", provider, linkId);
            return;
        }

        var lastSyncedAt = (DateTime?)link.last_synced_at;
        var fromUtc = lastSyncedAt ?? now.AddDays(-1);
        var toUtc = now;

        IReadOnlyList<CgmReading> readings;
        try
        {
            readings = await provider2.FetchReadingsAsync((string)link.external_account_id, fromUtc, toUtc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CgmReadingsSyncJob: loi goi FetchReadingsAsync cho link {Id}", linkId);
            await conn.ExecuteAsync(
                "UPDATE diab_his_dev_cgm_links SET last_sync_error=@Err, updated_at=@Now WHERE id=@Id",
                new { Err = "Loi ket noi nha cung cap CGM", Now = now, Id = linkId });
            return;
        }

        var inserted = 0;
        foreach (var r in readings)
        {
            var rows = await conn.ExecuteAsync(@"
                INSERT IGNORE INTO diab_his_dev_cgm_readings
                    (id, tenant_id, patient_id, cgm_link_id, provider, device_id, reading_at,
                     glucose_value_mg_dl, trend_direction, created_at, updated_at)
                VALUES
                    (UUID(), @TenantId, @PatientId, @LinkId, @Provider, @DeviceId, @ReadingAt,
                     @Value, @Trend, @Now, @Now)",
                new
                {
                    TenantId = tenantId, PatientId = patientId, LinkId = linkId, Provider = provider,
                    DeviceId = r.DeviceId, ReadingAt = r.Timestamp, Value = r.GlucoseValueMgDl,
                    Trend = r.TrendDirection, Now = now
                });
            inserted += rows;
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_dev_cgm_links SET status='ACTIVE', last_sync_error=NULL, last_synced_at=@Now, updated_at=@Now WHERE id=@Id",
            new { Now = now, Id = linkId });

        _logger.LogInformation(
            "CgmReadingsSyncJob: link {Id} - {Fetched} ban ghi tu provider, {Inserted} ban ghi moi (sau idempotency)",
            linkId, readings.Count, inserted);
    }

    /// <summary>Tạo provider adapter với HttpClient RIÊNG đã gắn Authorization Bearer của bệnh nhân hiện tại.</summary>
    private ICgmDeviceProvider? CreateProviderWithAuthorizedClient(string provider, string accessToken)
    {
        if (!string.Equals(provider, "Dexcom", StringComparison.OrdinalIgnoreCase))
            return null;

        var httpClient = _httpClientFactory.CreateClient(DexcomCgmProvider.HttpClientName);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var logger = _loggerFactory.CreateLogger<DexcomCgmProvider>();
        return new DexcomCgmProvider(httpClient, _dexcomOptions, logger);
    }
}
