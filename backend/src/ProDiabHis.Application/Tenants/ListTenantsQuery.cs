using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Tenants;

public record ListTenantsQuery(
    int Page,
    int PageSize,
    string? Status,
    string? Search
) : IRequest<PagedResult<TenantResponse>>;

public class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, PagedResult<TenantResponse>>
{
    private readonly IDapperConnectionFactory _db;

    public ListTenantsQueryHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PagedResult<TenantResponse>> Handle(ListTenantsQuery req, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var where = "WHERE deleted_at IS NULL";
        var p = new DynamicParameters();

        if (!string.IsNullOrEmpty(req.Status))
        {
            where += " AND status = @status";
            p.Add("status", req.Status);
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            where += " AND (name LIKE @search OR code LIKE @search)";
            p.Add("search", $"%{req.Search}%");
        }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_sys_tenants {where}", p);

        var offset = (req.Page - 1) * req.PageSize;
        p.Add("limit", req.PageSize);
        p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, code, name, cskcb_code, status, tax_code, address, phone, email,
                      subdomain, storage_quota_gb, expires_at, created_at, updated_at
               FROM diab_his_sys_tenants {where}
               ORDER BY created_at DESC
               LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r => new TenantResponse(
            (int)r.id,
            (string)r.code,
            (string)r.name,
            (string?)r.cskcb_code,
            (string)r.status,
            (string?)r.tax_code,
            (string?)r.address,
            (string?)r.phone,
            (string?)r.email,
            (string?)r.subdomain ?? string.Empty,
            (int)r.storage_quota_gb,
            r.expires_at == null ? null : (DateTime?)r.expires_at,
            (DateTime)r.created_at,
            (DateTime)r.updated_at
        )).ToList();

        return new PagedResult<TenantResponse>(items, req.Page, req.PageSize, total);
    }
}
