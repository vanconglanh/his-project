using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;
using ProDiabHis.Application.Documents;

namespace ProDiabHis.Infrastructure.Documents;

/// <summary>
/// Impl <see cref="IPendingRadOrdersProvider"/> cho DocumentClassifierService — Dapper query
/// diab_his_cli_rad_orders dang cho ket qua theo encounterId, co filter tenant_id (multi-tenant
/// application-layer). Cung dieu kien voi cach RadResultHandlers xac dinh RadOrder chua co ket qua.
/// </summary>
public class PendingRadOrdersProvider : IPendingRadOrdersProvider
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public PendingRadOrdersProvider(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<bool> HasPendingAsync(Guid encounterId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
            FROM diab_his_cli_rad_orders ro
            WHERE ro.tenant_id = @TId
              AND ro.encounter_id = @EncId
              AND ro.deleted_at IS NULL
              AND ro.status <> 'cancelled'
              AND NOT EXISTS (
                    SELECT 1 FROM diab_his_rad_results rr
                    WHERE rr.order_id = ro.id
                      AND rr.tenant_id = ro.tenant_id
                      AND rr.deleted_at IS NULL)
            LIMIT 1";

        using var conn = _db.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(sql,
            new { TId = _tenant.TenantId, EncId = encounterId.ToString() }, cancellationToken: ct));
        return result.HasValue;
    }
}
