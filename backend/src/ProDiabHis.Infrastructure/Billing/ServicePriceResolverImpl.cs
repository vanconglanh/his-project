using Dapper;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Resolver gia dich vu 3 tang (BR-70..BR-76). Doc bang override bang Dapper (read-only, khong
/// dung EF de tranh phu thuoc tracking) va gia goc tu diab_his_bil_services.
/// Doc group_id cua branch truc tiep tu diab_his_sys_branches (CHI DOC, khong sua bang branch
/// theo rang buoc pham vi cong viec).
/// </summary>
public class ServicePriceResolverImpl : IServicePriceResolver
{
    private readonly IDapperConnectionFactory _db;

    public ServicePriceResolverImpl(IDapperConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ResolvedServicePrice?> ResolveAsync(
        int tenantId, Guid serviceId, int? branchId, DateOnly asOfDate, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var sid = serviceId.ToString();
        var asOf = asOfDate.ToString("yyyy-MM-dd");

        // 1) Override theo BRANCH con hieu luc (uu tien cao nhat)
        if (branchId.HasValue)
        {
            var branchOverride = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, price FROM diab_his_bil_service_branch_prices
                  WHERE tenant_id=@tenantId AND service_id=@sid AND scope='BRANCH' AND branch_id=@branchId
                    AND deleted_at IS NULL
                    AND effective_from <= @asOf AND (effective_to IS NULL OR effective_to >= @asOf)
                  ORDER BY effective_from DESC LIMIT 1",
                new { tenantId, sid, branchId, asOf });
            if (branchOverride != null)
            {
                return new ResolvedServicePrice(
                    (decimal)branchOverride.price, PriceSource.Branch, ParseGuid((string)branchOverride.id));
            }
        }

        // 2) Override theo GROUP cua branch (neu branch co group_id), con hieu luc
        if (branchId.HasValue)
        {
            var groupId = await conn.ExecuteScalarAsync<int?>(
                "SELECT group_id FROM diab_his_sys_branches WHERE id=@branchId AND tenant_id=@tenantId AND deleted_at IS NULL",
                new { branchId, tenantId });

            if (groupId.HasValue)
            {
                var groupOverride = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT id, price FROM diab_his_bil_service_branch_prices
                      WHERE tenant_id=@tenantId AND service_id=@sid AND scope='GROUP' AND group_id=@groupId
                        AND deleted_at IS NULL
                        AND effective_from <= @asOf AND (effective_to IS NULL OR effective_to >= @asOf)
                      ORDER BY effective_from DESC LIMIT 1",
                    new { tenantId, sid, groupId, asOf });
                if (groupOverride != null)
                {
                    return new ResolvedServicePrice(
                        (decimal)groupOverride.price, PriceSource.Group, ParseGuid((string)groupOverride.id));
                }
            }
        }

        // 3) Gia goc TENANT trong danh muc dich vu
        var basePrice = await conn.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT price FROM diab_his_bil_services WHERE id=@sid AND tenant_id=@tenantId AND deleted_at IS NULL",
            new { sid, tenantId });

        return basePrice.HasValue ? new ResolvedServicePrice(basePrice.Value, PriceSource.Tenant, null) : null;
    }

    private static Guid? ParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;
}
