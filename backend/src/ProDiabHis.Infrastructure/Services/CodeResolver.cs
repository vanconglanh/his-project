using Dapper;
using Microsoft.Extensions.Caching.Memory;
using ProDiabHis.Application.Codes;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Services;

/// <summary>
/// Trien khai ICodeResolver - doc diab_his_sys_code_detail, uu tien ban ghi rieng
/// cua tenant hien tai (override) khi trung code voi ban ghi chuan (tenant_id IS NULL).
/// Cache in-memory TTL ngan (5 phut) theo key (tenantId, groupId) de giam tai DB;
/// invalidate thu cong khi admin ghi qua AdminCodesController (xem InvalidateCache).
/// </summary>
public class CodeResolver : ICodeResolver
{
    private readonly IDapperConnectionFactory _dbFactory;
    private readonly ITenantProvider _tenant;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public CodeResolver(IDapperConnectionFactory dbFactory, ITenantProvider tenant, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
        _cache = cache;
    }

    private string CacheKey(string groupId) => $"code_resolver:{_tenant.TenantId}:{groupId}";

    public static string BuildCacheKey(int tenantId, string groupId) => $"code_resolver:{tenantId}:{groupId}";

    public async Task<IReadOnlyList<CodeItem>> GetAsync(string groupId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue<IReadOnlyList<CodeItem>>(CacheKey(groupId), out var cached) && cached is not null)
            return cached;

        using var conn = _dbFactory.CreateConnection();
        // Lay CA cac dong is_hidden=1 cua tenant (dong danh dau AN ma chuan) de con suppress
        // ma global tuong ung. KHONG loc is_hidden o SQL — xu ly o tang nay.
        var rows = (await conn.QueryAsync<dynamic>(@"
            SELECT code, name, extra, tenant_id, is_hidden
            FROM diab_his_sys_code_detail
            WHERE code_master_id = @GroupId
              AND is_active = 1
              AND (tenant_id IS NULL OR tenant_id = @TenantId)
            ORDER BY sort_order, code",
            new { GroupId = groupId, TenantId = _tenant.TenantId })).ToList();

        // Tap code bi tenant hien tai danh dau AN (is_hidden=1) -> loai khoi ket qua,
        // ke ca ban global cung code.
        var hiddenCodes = rows
            .Where(r => r.tenant_id is not null && Convert.ToBoolean(r.is_hidden))
            .Select(r => (string)r.code)
            .ToHashSet(StringComparer.Ordinal);

        // Gop: neu cung code xuat hien o ca 2 nguon (global + tenant) thi ban cua tenant thang.
        var map = new Dictionary<string, CodeItem>(StringComparer.Ordinal);
        foreach (var r in rows)
        {
            string code = (string)r.code;
            bool isHidden = Convert.ToBoolean(r.is_hidden);
            int? tenantId = r.tenant_id is null ? null : (int)r.tenant_id;

            if (isHidden) continue;              // bo qua chinh dong danh dau an
            if (hiddenCodes.Contains(code)) continue; // tenant da an code nay -> loai luon ban global

            string name = (string)r.name;
            string? extra = r.extra is null ? null : (string)r.extra;

            if (map.TryGetValue(code, out var exist) && tenantId is null)
                continue; // da co ban tenant override, bo qua ban global

            if (!map.ContainsKey(code) || tenantId is not null)
                map[code] = new CodeItem(code, name, extra);
        }

        var result = map.Values.ToList().AsReadOnly() as IReadOnlyList<CodeItem>;
        _cache.Set(CacheKey(groupId), result, CacheTtl);
        return result;
    }

    public async Task<bool> IsValidAsync(string groupId, string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var items = await GetAsync(groupId, ct);
        return items.Any(i => string.Equals(i.Code, code, StringComparison.Ordinal));
    }

    public async Task<string> LabelAsync(string groupId, string code, CancellationToken ct = default)
    {
        var items = await GetAsync(groupId, ct);
        return items.FirstOrDefault(i => string.Equals(i.Code, code, StringComparison.Ordinal))?.Name ?? code;
    }
}
