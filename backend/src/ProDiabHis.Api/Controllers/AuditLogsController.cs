using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.AuditLogs;

namespace ProDiabHis.Api.Controllers;

/// <summary>Nhat ky thao tac he thong</summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AuditQueryService _auditQueryService;

    public AuditLogsController(IMediator mediator, AuditQueryService auditQueryService)
    {
        _mediator = mediator;
        _auditQueryService = auditQueryService;
    }

    /// <summary>Xem nhat ky thao tac</summary>
    [HttpGet]
    [RequirePermission("audit.review")]
    public async Task<IActionResult> ListAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] Guid? user_id = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resource_type = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListAuditLogsQuery(page, page_size, user_id, action, resource_type, from, to, severity), ct);

        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    /// <summary>Xuat audit log ra CSV</summary>
    [HttpGet("export")]
    [RequirePermission("audit.export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] Guid? user_id = null,
        [FromQuery] string? action = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var tenantId = int.TryParse(User.FindFirst("tenant_id")?.Value, out var tid) ? tid : (int?)null;

        var filter = new AuditLogFilter(
            TenantId: tenantId,
            UserId: user_id,
            Action: action,
            ResourceType: null,
            Severity: severity,
            From: from,
            To: to,
            Page: 1,
            PageSize: 10000);

        var csv = await _auditQueryService.ExportToCsvAsync(filter, ct);

        var filename = $"audit_log_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", filename);
    }
}
