using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Resolve phong kham (tenant) tu subdomain cho request portal AN DANH (login/activate).
// Kien truc 1 DB dung chung + tenant_id filter (khong dung catalog DB-per-clinic).
//   - subdomain lay tu Host header: "phongkham-a.diab.com.vn" -> "phongkham-a"
//     (dev: "phongkham-a.localhost:3000"). Cho phep override qua header/query khi dev.
//   - tra tenant_id tu diab_his_sys_tenants.subdomain (cache 5 phut). 0 = khong resolve duoc.
// ============================================================
public record ResolvePortalTenantQuery(string? Host, string? SubdomainOverride) : IRequest<int>;

public class ResolvePortalTenantHandler : IRequestHandler<ResolvePortalTenantQuery, int>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly IDapperConnectionFactory _db;
    private readonly IMemoryCache _cache;

    public ResolvePortalTenantHandler(IDapperConnectionFactory db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<int> Handle(ResolvePortalTenantQuery q, CancellationToken cancellationToken)
    {
        var sub = ExtractSubdomain(q.Host, q.SubdomainOverride);
        if (string.IsNullOrWhiteSpace(sub)) return 0;

        if (_cache.TryGetValue($"portal-tenant:{sub}", out int cached)) return cached;

        using var conn = _db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int?>(
            @"SELECT id FROM diab_his_sys_tenants
              WHERE subdomain = @Sub AND deleted_at IS NULL AND status = 'ACTIVE'
              LIMIT 1",
            new { Sub = sub });

        var tenantId = id ?? 0;
        _cache.Set($"portal-tenant:{sub}", tenantId, CacheTtl);
        return tenantId;
    }

    // Public de unit test parse logic
    public static string? ExtractSubdomain(string? host, string? overrideSub)
    {
        if (!string.IsNullOrWhiteSpace(overrideSub)) return overrideSub.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host)) return null;

        var h = host.Split(':')[0].Trim().ToLowerInvariant();
        if (h.Length == 0 || h == "localhost") return null;
        if (System.Net.IPAddress.TryParse(h, out _)) return null;

        var parts = h.Split('.');
        if (parts.Length < 2) return null;               // vd "localhost" don le
        var first = parts[0];
        if (first is "www" or "api" or "portal") return null; // khong phai subdomain phong kham
        return first;
    }
}
