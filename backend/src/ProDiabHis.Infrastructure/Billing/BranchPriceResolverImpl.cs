using Dapper;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Resolver GIA + AN/HIEN dung chung cho ca DICH VU va THUOC (1 tang logic, khong trung code).
/// 3 tang uu tien: override BRANCH con hieu luc -> override GROUP (nhom cua branch) con hieu luc
/// -> gia goc TENANT. Doc bang override bang Dapper (read-only). group_id cua branch doc truc tiep
/// tu diab_his_sys_branches (CHI DOC).
/// </summary>
public class BranchPriceResolverImpl : IBranchPriceResolver
{
    private readonly IDapperConnectionFactory _db;

    public BranchPriceResolverImpl(IDapperConnectionFactory db)
    {
        _db = db;
    }

    private readonly record struct ItemConfig(string OverrideTable, string ItemColumn, string BaseSql);

    private static ItemConfig Config(PriceItemType type) => type switch
    {
        PriceItemType.Service => new ItemConfig(
            "diab_his_bil_service_branch_prices", "service_id",
            "SELECT price FROM diab_his_bil_services WHERE id=@sid AND tenant_id=@tenantId AND deleted_at IS NULL"),
        PriceItemType.Drug => new ItemConfig(
            "diab_his_pha_drug_branch_prices", "drug_id",
            "SELECT price FROM diab_his_pha_drugs WHERE ID=@sid AND tenant_id=@tenantId AND deleted_at IS NULL AND status='ACTIVE'"),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public async Task<ResolvedItemPrice?> ResolveAsync(
        int tenantId, PriceItemType itemType, string itemId, int? branchId, DateOnly asOfDate,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var cfg = Config(itemType);
        var sid = itemId;
        var asOf = asOfDate.ToString("yyyy-MM-dd");

        // 1) Override theo BRANCH con hieu luc (uu tien cao nhat)
        if (branchId.HasValue)
        {
            var branchOverride = await conn.QueryFirstOrDefaultAsync<dynamic>(
                $@"SELECT id, price, is_active FROM {cfg.OverrideTable}
                   WHERE tenant_id=@tenantId AND {cfg.ItemColumn}=@sid AND scope='BRANCH' AND branch_id=@branchId
                     AND deleted_at IS NULL
                     AND effective_from <= @asOf AND (effective_to IS NULL OR effective_to >= @asOf)
                   ORDER BY effective_from DESC LIMIT 1",
                new { tenantId, sid, branchId, asOf });
            if (branchOverride != null)
            {
                return new ResolvedItemPrice(
                    (decimal)branchOverride.price, PriceSource.Branch,
                    ParseGuid((string)branchOverride.id.ToString()), Convert.ToBoolean(branchOverride.is_active));
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
                    $@"SELECT id, price, is_active FROM {cfg.OverrideTable}
                       WHERE tenant_id=@tenantId AND {cfg.ItemColumn}=@sid AND scope='GROUP' AND group_id=@groupId
                         AND deleted_at IS NULL
                         AND effective_from <= @asOf AND (effective_to IS NULL OR effective_to >= @asOf)
                       ORDER BY effective_from DESC LIMIT 1",
                    new { tenantId, sid, groupId, asOf });
                if (groupOverride != null)
                {
                    return new ResolvedItemPrice(
                        (decimal)groupOverride.price, PriceSource.Group,
                        ParseGuid((string)groupOverride.id.ToString()), Convert.ToBoolean(groupOverride.is_active));
                }
            }
        }

        // 3) Gia goc TENANT trong danh muc (dich vu/thuoc) - luon hien
        var basePrice = await conn.QueryFirstOrDefaultAsync<decimal?>(cfg.BaseSql, new { sid, tenantId });
        return basePrice.HasValue ? new ResolvedItemPrice(basePrice.Value, PriceSource.Tenant, null, true) : null;
    }

    private static Guid? ParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;
}
