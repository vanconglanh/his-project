using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.AuditLogs;

public record ListAuditLogsQuery(
    int Page,
    int PageSize,
    Guid? UserId,
    string? Action,
    string? ResourceType,
    DateTime? From,
    DateTime? To,
    string? Severity = null
) : IRequest<PagedResult<AuditLogResponse>>;

public class ListAuditLogsQueryHandler : IRequestHandler<ListAuditLogsQuery, PagedResult<AuditLogResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListAuditLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogResponse>> Handle(ListAuditLogsQuery req, CancellationToken ct)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (req.UserId.HasValue)
            query = query.Where(a => a.UserId == req.UserId.Value);
        if (!string.IsNullOrEmpty(req.Action))
            query = query.Where(a => a.Action == req.Action);
        if (!string.IsNullOrEmpty(req.ResourceType))
            query = query.Where(a => a.ResourceType == req.ResourceType);
        if (req.From.HasValue)
            query = query.Where(a => a.CreatedAt >= req.From.Value);
        if (req.To.HasValue)
            query = query.Where(a => a.CreatedAt <= req.To.Value);
        if (!string.IsNullOrEmpty(req.Severity))
            query = query.Where(a => a.Severity == req.Severity);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var responses = items.Select(a => new AuditLogResponse(
            Id: a.Id,
            TenantId: a.TenantId,
            UserId: a.UserId,
            UserEmail: a.UserEmail,
            Action: a.Action,
            ResourceType: a.ResourceType,
            ResourceId: a.ResourceId,
            IpAddress: a.IpAddress,
            UserAgent: a.UserAgent,
            Details: string.IsNullOrEmpty(a.DetailsJson)
                ? null
                : JsonSerializer.Deserialize<object>(a.DetailsJson),
            CreatedAt: a.CreatedAt,
            Severity: a.Severity,
            CrossTenantAttempt: a.CrossTenantAttempt,
            RequestId: a.RequestId
        )).ToList();

        return new PagedResult<AuditLogResponse>(responses, req.Page, req.PageSize, total);
    }
}
