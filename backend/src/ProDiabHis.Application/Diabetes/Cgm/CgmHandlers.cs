using System.Text;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Diabetes.Cgm;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════

/// <summary>FR-711: Portal — bệnh nhân tự liên kết tài khoản CGM.</summary>
public record LinkCgmAccountCommand(Guid PatientId, CgmLinkRequest Request) : IRequest<Result<CgmLinkResponse>>;

/// <summary>FR-711: Bác sĩ xem trạng thái liên kết CGM của bệnh nhân.</summary>
public record GetCgmStatusQuery(Guid PatientId) : IRequest<Result<CgmStatusResponse>>;

/// <summary>FR-711: Portal — thiết bị/app CGM chủ động đẩy batch dữ liệu đo về (push, bổ sung cho job pull định kỳ).</summary>
public record SyncCgmReadingsCommand(Guid PatientId, CgmSyncRequest Request) : IRequest<Result<CgmSyncResponse>>;

// ═══════════════════════════════════════════════
// Link tai khoan CGM (POST /api/v1/portal/cgm/link)
// ═══════════════════════════════════════════════
public class LinkCgmAccountCommandHandler : IRequestHandler<LinkCgmAccountCommand, Result<CgmLinkResponse>>
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase) { "Dexcom" };

    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IEncryptionService _enc;
    private readonly ICgmDeviceProvider _provider;
    private readonly IAuditService _audit;
    private readonly ILogger<LinkCgmAccountCommandHandler> _logger;

    public LinkCgmAccountCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IEncryptionService enc,
        ICgmDeviceProvider provider, IAuditService audit, ILogger<LinkCgmAccountCommandHandler> logger)
    { _db = db; _tenant = tenant; _enc = enc; _provider = provider; _audit = audit; _logger = logger; }

    public async Task<Result<CgmLinkResponse>> Handle(LinkCgmAccountCommand cmd, CancellationToken ct)
    {
        var providerCode = (cmd.Request.Provider ?? string.Empty).Trim();
        if (!SupportedProviders.Contains(providerCode))
        {
            return Result<CgmLinkResponse>.Failure("CGM_PROVIDER_NOT_SUPPORTED",
                $"Nhà cung cấp CGM '{cmd.Request.Provider}' chưa được hỗ trợ. Hiện chỉ hỗ trợ: {string.Join(", ", SupportedProviders)}");
        }

        if (string.IsNullOrWhiteSpace(cmd.Request.AuthCode))
            return Result<CgmLinkResponse>.Failure("CGM_AUTH_CODE_REQUIRED", "Thiếu mã xác thực (authCode) từ nhà cung cấp CGM");

        using var conn = _db.CreateConnection();

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.PatientId.ToString(), TId = _tenant.TenantId });
        if (patient is null)
            return Result<CgmLinkResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        CgmLinkResult linkResult;
        try
        {
            linkResult = await _provider.LinkPatientAccountAsync(string.Empty, cmd.Request.AuthCode, ct);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogWarning(ex, "CGM provider {Provider} chua duoc cau hinh day du", providerCode);
            return Result<CgmLinkResponse>.Failure("CGM_PROVIDER_NOT_CONFIGURED",
                "Nhà cung cấp CGM chưa được cấu hình đầy đủ, vui lòng liên hệ quản trị hệ thống");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi LinkPatientAccountAsync cho patient {PatientId}", cmd.PatientId);
            return Result<CgmLinkResponse>.Failure("CGM_PROVIDER_UNAVAILABLE", "Không kết nối được nhà cung cấp CGM, vui lòng thử lại");
        }

        if (!linkResult.Success)
        {
            return Result<CgmLinkResponse>.Failure(
                linkResult.ErrorCode ?? "CGM_LINK_FAILED",
                linkResult.ErrorMessage ?? "Liên kết tài khoản CGM thất bại");
        }

        var accessTokenEnc = string.IsNullOrEmpty(linkResult.AccessToken) ? null : _enc.Encrypt(linkResult.AccessToken);
        var refreshTokenEnc = string.IsNullOrEmpty(linkResult.RefreshToken) ? null : _enc.Encrypt(linkResult.RefreshToken);
        var now = DateTime.UtcNow;

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_dev_cgm_links WHERE tenant_id=@TId AND patient_id=@PId AND provider=@Provider",
            new { TId = _tenant.TenantId, PId = cmd.PatientId.ToString(), Provider = providerCode });

        if (existing is null)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_dev_cgm_links
                    (id, tenant_id, patient_id, provider, external_account_id, access_token_enc, refresh_token_enc,
                     token_expires_at, status, linked_at, created_at, updated_at)
                VALUES
                    (UUID(), @TId, @PId, @Provider, @ExtId, @AccessToken, @RefreshToken, @Exp, 'ACTIVE', @Now, @Now, @Now)",
                new
                {
                    TId = _tenant.TenantId, PId = cmd.PatientId.ToString(), Provider = providerCode,
                    ExtId = linkResult.ExternalAccountId, AccessToken = accessTokenEnc, RefreshToken = refreshTokenEnc,
                    Exp = linkResult.ExpiresAt, Now = now
                });
        }
        else
        {
            await conn.ExecuteAsync(@"
                UPDATE diab_his_dev_cgm_links
                SET external_account_id=@ExtId, access_token_enc=@AccessToken, refresh_token_enc=@RefreshToken,
                    token_expires_at=@Exp, status='ACTIVE', last_sync_error=NULL, linked_at=@Now, updated_at=@Now,
                    deleted_at=NULL
                WHERE id=@Id",
                new
                {
                    Id = (string)existing.id, ExtId = linkResult.ExternalAccountId, AccessToken = accessTokenEnc,
                    RefreshToken = refreshTokenEnc, Exp = linkResult.ExpiresAt, Now = now
                });
        }

        await _audit.LogAsync("LINK_CGM_ACCOUNT", "Patient", cmd.PatientId.ToString(),
            new { provider = providerCode, external_account_id = linkResult.ExternalAccountId }, ct);

        return Result<CgmLinkResponse>.Success(
            new CgmLinkResponse(true, providerCode, linkResult.ExternalAccountId, linkResult.ExpiresAt));
    }
}

// ═══════════════════════════════════════════════
// Trang thai lien ket CGM (GET /api/v1/patients/{id}/cgm-status)
// ═══════════════════════════════════════════════
public class GetCgmStatusQueryHandler : IRequestHandler<GetCgmStatusQuery, Result<CgmStatusResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetCgmStatusQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<CgmStatusResponse>> Handle(GetCgmStatusQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = q.PatientId.ToString(), TId = _tenant.TenantId });
        if (patient is null)
            return Result<CgmStatusResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var link = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT * FROM diab_his_dev_cgm_links
            WHERE tenant_id=@TId AND patient_id=@PId AND deleted_at IS NULL
            ORDER BY status = 'ACTIVE' DESC, updated_at DESC
            LIMIT 1",
            new { TId = _tenant.TenantId, PId = q.PatientId.ToString() });

        if (link is null)
            return Result<CgmStatusResponse>.Success(new CgmStatusResponse(false, null, null, null, null, null));

        var status = (string)link.status;
        return Result<CgmStatusResponse>.Success(new CgmStatusResponse(
            Linked: status == "ACTIVE",
            Provider: (string)link.provider,
            Status: status,
            LinkedAt: (DateTime?)link.linked_at,
            LastSyncedAt: (DateTime?)link.last_synced_at,
            LastSyncError: (string?)link.last_sync_error));
    }
}

// ═══════════════════════════════════════════════
// FR-711: Dong bo (push) batch du lieu do CGM (POST /api/v1/portal/cgm/sync)
// Bo sung cho CgmReadingsSyncJob (pull dinh ky) — dung khi thiet bi/app CGM cua benh nhan
// chu dong day du lieu ve thay vi cho HIS poll theo lich.
// Idempotency: INSERT IGNORE theo UNIQUE KEY (tenant_id, patient_id, provider, device_id, reading_at)
// cua bang diab_his_dev_cgm_readings — trung khoang thoi gian/gia tri se tu dong bo qua, KHONG loi.
// ═══════════════════════════════════════════════
public class SyncCgmReadingsCommandHandler : IRequestHandler<SyncCgmReadingsCommand, Result<CgmSyncResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;
    private readonly ILogger<SyncCgmReadingsCommandHandler> _logger;

    public SyncCgmReadingsCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        IAuditService audit, ILogger<SyncCgmReadingsCommandHandler> logger)
    { _db = db; _tenant = tenant; _audit = audit; _logger = logger; }

    public async Task<Result<CgmSyncResponse>> Handle(SyncCgmReadingsCommand cmd, CancellationToken ct)
    {
        var providerCode = (cmd.Request.Provider ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(providerCode))
            return Result<CgmSyncResponse>.Failure("CGM_PROVIDER_NOT_SUPPORTED", "Thiếu thông tin nhà cung cấp CGM (provider)");

        var readings = cmd.Request.Readings;
        if (readings is null || readings.Count == 0)
            return Result<CgmSyncResponse>.Failure("CGM_SYNC_EMPTY_BATCH", "Batch dữ liệu đo CGM rỗng, không có gì để đồng bộ");

        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var patient = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM diab_his_pat_patients WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.PatientId.ToString(), TId = tenantId });
        if (patient is null)
            return Result<CgmSyncResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        // Chi cho phep dong bo neu benh nhan da lien ket tai khoan CGM voi provider nay (bat ky trang thai
        // nao tru REVOKED) - tranh nhan du lieu "ma" tu thiet bi chua tung link.
        var link = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, status FROM diab_his_dev_cgm_links
            WHERE tenant_id=@TId AND patient_id=@PId AND provider=@Provider AND deleted_at IS NULL
            ORDER BY status='ACTIVE' DESC, updated_at DESC LIMIT 1",
            new { TId = tenantId, PId = cmd.PatientId.ToString(), Provider = providerCode });

        if (link is null || (string)link.status == "REVOKED")
            return Result<CgmSyncResponse>.Failure("CGM_ACCOUNT_NOT_LINKED",
                "Bệnh nhân chưa liên kết tài khoản CGM với nhà cung cấp này, vui lòng liên kết trước khi đồng bộ");

        string linkId = (string)link.id;
        var now = DateTime.UtcNow;
        int inserted = 0, skipped = 0;

        foreach (var r in readings)
        {
            if (r.GlucoseValueMgDl <= 0)
            {
                skipped++;
                continue;
            }

            var rows = await conn.ExecuteAsync(@"
                INSERT IGNORE INTO diab_his_dev_cgm_readings
                    (id, tenant_id, patient_id, cgm_link_id, provider, device_id, reading_at,
                     glucose_value_mg_dl, trend_direction, created_at, updated_at)
                VALUES
                    (UUID(), @TenantId, @PatientId, @LinkId, @Provider, @DeviceId, @ReadingAt,
                     @Value, @Trend, @Now, @Now)",
                new
                {
                    TenantId = tenantId, PatientId = cmd.PatientId.ToString(), LinkId = linkId,
                    Provider = providerCode, DeviceId = r.DeviceId, ReadingAt = r.Timestamp,
                    Value = r.GlucoseValueMgDl, Trend = r.TrendDirection, Now = now
                });

            if (rows > 0) inserted++; else skipped++;
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_dev_cgm_links SET status='ACTIVE', last_sync_error=NULL, last_synced_at=@Now, updated_at=@Now WHERE id=@Id",
            new { Now = now, Id = linkId });

        _logger.LogInformation(
            "SyncCgmReadingsCommand: patient {PatientId} provider {Provider} - nhan {Received}, moi {Inserted}, bo qua {Skipped}",
            cmd.PatientId, providerCode, readings.Count, inserted, skipped);

        await _audit.LogAsync("SYNC_CGM_READINGS", "Patient", cmd.PatientId.ToString(),
            new { provider = providerCode, received = readings.Count, inserted, skipped }, ct);

        return Result<CgmSyncResponse>.Success(new CgmSyncResponse(readings.Count, inserted, skipped, now));
    }
}
