using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire daily job: detect audit anomalies.
/// Triggers:
///   - failed_login > 10/h per user
///   - cross_tenant_attempt > 0
///   - after-hours admin access (22:00-06:00)
/// Tao CRITICAL alert qua IAuditService.
/// </summary>
public class AuditAnomalyDetectionJob
{
    private readonly IDapperConnectionFactory _dapper;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditAnomalyDetectionJob> _logger;

    public AuditAnomalyDetectionJob(
        IDapperConnectionFactory dapper,
        IAuditService auditService,
        ILogger<AuditAnomalyDetectionJob> logger)
    {
        _dapper = dapper;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[AuditAnomalyDetectionJob] Bat dau phan tich anomaly audit...");

        await DetectFailedLoginBurstAsync();
        await DetectCrossTenantAttemptsAsync();
        await DetectAfterHoursAdminAccessAsync();

        _logger.LogInformation("[AuditAnomalyDetectionJob] Hoan thanh.");
    }

    private async Task DetectFailedLoginBurstAsync()
    {
        using var conn = _dapper.CreateConnection();

        var bursts = await conn.QueryAsync<dynamic>(
            @"SELECT tenant_id, user_email, COUNT(*) as cnt
              FROM sec_audit_logs
              WHERE action = 'FAILED_LOGIN'
                AND created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)
              GROUP BY tenant_id, user_email
              HAVING cnt > 10");

        foreach (var b in bursts)
        {
            // Cast ve typed truoc khi truyen vao logger (tranh CS1973 khi dung dynamic)
            var tenantId = (int?)b.tenant_id;
            var email = (string?)b.user_email;
            var cnt = (int)b.cnt;

            _logger.LogWarning("[ANOMALY] Failed login burst: tenant={TenantId} email={Email} count={Count}",
                tenantId, email, cnt);

            await _auditService.LogAsync(
                action: "ANOMALY_FAILED_LOGIN_BURST",
                resourceType: "USER",
                resourceId: email,
                severity: AuditSeverity.CRITICAL,
                crossTenantAttempt: false,
                requestId: null,
                details: new { count = cnt, window = "1h" });
        }
    }

    private async Task DetectCrossTenantAttemptsAsync()
    {
        using var conn = _dapper.CreateConnection();

        var attempts = await conn.QueryAsync<dynamic>(
            @"SELECT tenant_id, user_id, user_email, COUNT(*) as cnt
              FROM sec_audit_logs
              WHERE cross_tenant_attempt = 1
                AND created_at >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
              GROUP BY tenant_id, user_id, user_email
              HAVING cnt > 0");

        foreach (var a in attempts)
        {
            var tenantId = (int?)a.tenant_id;
            var userId = (string?)a.user_id?.ToString();
            var cnt = (int)a.cnt;

            _logger.LogError("[ANOMALY] Cross-tenant attempt: tenant={TenantId} user={UserId} count={Count}",
                tenantId, userId, cnt);

            await _auditService.LogAsync(
                action: "ANOMALY_CROSS_TENANT",
                resourceType: "TENANT",
                resourceId: tenantId?.ToString(),
                severity: AuditSeverity.CRITICAL,
                crossTenantAttempt: true,
                requestId: null,
                details: new { userId, count = cnt });
        }
    }

    private async Task DetectAfterHoursAdminAccessAsync()
    {
        using var conn = _dapper.CreateConnection();

        var afterHours = await conn.QueryAsync<dynamic>(
            @"SELECT tenant_id, user_id, user_email, COUNT(*) as cnt
              FROM sec_audit_logs
              WHERE created_at >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
                AND (HOUR(created_at) >= 22 OR HOUR(created_at) < 6)
                AND action IN ('CREATE','UPDATE','DELETE','ENCRYPTION_ROTATE','system.config')
              GROUP BY tenant_id, user_id, user_email
              HAVING cnt > 0");

        foreach (var a in afterHours)
        {
            var tenantId = (int?)a.tenant_id;
            var userId = (string?)a.user_id?.ToString();
            var cnt = (int)a.cnt;

            _logger.LogWarning("[ANOMALY] After-hours admin access: tenant={TenantId} user={UserId} count={Count}",
                tenantId, userId, cnt);

            await _auditService.LogAsync(
                action: "ANOMALY_AFTER_HOURS_ACCESS",
                resourceType: "SYSTEM",
                resourceId: userId,
                severity: AuditSeverity.WARN,
                crossTenantAttempt: false,
                requestId: null,
                details: new { count = cnt, window = "22:00-06:00" });
        }
    }
}
