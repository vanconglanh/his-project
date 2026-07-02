using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>Ghi audit log vao bang sec_audit_logs</summary>
public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task LogAsync(
        string action,
        string? resourceType,
        string? resourceId,
        object? details = null,
        CancellationToken cancellationToken = default)
        => LogAsync(action, resourceType, resourceId,
            AuditSeverity.INFO, false, null, details, cancellationToken);

    public async Task LogAsync(
        string action,
        string? resourceType,
        string? resourceId,
        AuditSeverity severity,
        bool crossTenantAttempt = false,
        string? requestId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ctx = _httpContextAccessor.HttpContext;
            var ipAddress = ctx?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = ctx?.Request.Headers.UserAgent.ToString();
            var rid = requestId ?? ctx?.TraceIdentifier;

            var auditLog = new AuditLog
            {
                TenantId = _tenantProvider.TenantId,
                UserId = _currentUser.UserId,
                UserEmail = _currentUser.Email,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DetailsJson = details != null
                    ? JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = false })
                    : null,
                Severity = severity.ToString(),
                CrossTenantAttempt = crossTenantAttempt,
                RequestId = rid,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(cancellationToken);

            // Log CRITICAL events to structured logging ngay lap tuc
            if (severity == AuditSeverity.CRITICAL)
            {
                _logger.LogCritical(
                    "AUDIT_CRITICAL | Action={Action} Resource={ResourceType}/{ResourceId} User={UserId} CrossTenant={CrossTenant}",
                    action, resourceType, resourceId, _currentUser.UserId, crossTenantAttempt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi ghi audit log: action={Action} resource={ResourceType}/{ResourceId}",
                action, resourceType, resourceId);
        }
    }
}
