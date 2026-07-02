using System.Text;
using Dapper;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.AuditLogs;

public record AuditLogFilter(
    int? TenantId,
    Guid? UserId,
    string? Action,
    string? ResourceType,
    string? Severity,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50
);

/// <summary>Query audit logs bang Dapper (read-side) voi filter phong phu</summary>
public class AuditQueryService
{
    private readonly IDapperConnectionFactory _dapper;

    public AuditQueryService(IDapperConnectionFactory dapper) => _dapper = dapper;

    public async Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        using var conn = _dapper.CreateConnection();

        var where = new List<string>();
        var p = new DynamicParameters();

        if (filter.TenantId.HasValue)
        {
            where.Add("tenant_id = @tenantId");
            p.Add("tenantId", filter.TenantId.Value);
        }
        if (filter.UserId.HasValue)
        {
            where.Add("user_id = @userId");
            p.Add("userId", filter.UserId.Value.ToString());
        }
        if (!string.IsNullOrEmpty(filter.Action))
        {
            where.Add("action = @action");
            p.Add("action", filter.Action);
        }
        if (!string.IsNullOrEmpty(filter.ResourceType))
        {
            where.Add("resource_type = @resourceType");
            p.Add("resourceType", filter.ResourceType);
        }
        if (!string.IsNullOrEmpty(filter.Severity))
        {
            where.Add("severity = @severity");
            p.Add("severity", filter.Severity);
        }
        if (filter.From.HasValue)
        {
            where.Add("created_at >= @from");
            p.Add("from", filter.From.Value);
        }
        if (filter.To.HasValue)
        {
            where.Add("created_at <= @to");
            p.Add("to", filter.To.Value);
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var countSql = $"SELECT COUNT(*) FROM sec_audit_logs {whereClause}";
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);

        var offset = (filter.Page - 1) * filter.PageSize;
        p.Add("limit", filter.PageSize);
        p.Add("offset", offset);

        var dataSql = $@"
            SELECT id, tenant_id, user_id, user_email, action, resource_type, resource_id,
                   ip_address, user_agent, details_json, severity, cross_tenant_attempt,
                   request_id, created_at
            FROM sec_audit_logs
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @limit OFFSET @offset";

        var rows = await conn.QueryAsync<dynamic>(dataSql, p);

        var items = rows.Select(r => new AuditLogResponse(
            Id: Guid.Parse((string)r.id),
            TenantId: (int)r.tenant_id,
            UserId: r.user_id != null ? Guid.Parse((string)r.user_id) : (Guid?)null,
            UserEmail: (string?)r.user_email,
            Action: (string)r.action,
            ResourceType: (string?)r.resource_type,
            ResourceId: (string?)r.resource_id,
            IpAddress: (string?)r.ip_address,
            UserAgent: (string?)r.user_agent,
            Details: null, // omit in list view
            CreatedAt: (DateTime)r.created_at
        )).ToList();

        return new PagedResult<AuditLogResponse>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<string> ExportToCsvAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        // Export toi da 10000 ban ghi
        var bigFilter = filter with { Page = 1, PageSize = 10000 };
        var result = await GetAuditLogsAsync(bigFilter, ct);

        var sb = new StringBuilder();
        sb.AppendLine("id,tenant_id,user_id,user_email,action,resource_type,resource_id,ip_address,severity,cross_tenant_attempt,created_at");

        foreach (var item in result.Items)
        {
            sb.AppendLine(
                $"{item.Id},{item.TenantId},{item.UserId},{EscapeCsv(item.UserEmail)}," +
                $"{EscapeCsv(item.Action)},{EscapeCsv(item.ResourceType)},{EscapeCsv(item.ResourceId)}," +
                $"{EscapeCsv(item.IpAddress)},,," +
                $"{item.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (value == null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
