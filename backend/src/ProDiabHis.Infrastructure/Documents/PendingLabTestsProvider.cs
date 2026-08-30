using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;
using ProDiabHis.Application.Documents;

namespace ProDiabHis.Infrastructure.Documents;

/// <summary>
/// Impl <see cref="IPendingLabTestsProvider"/> cho DocumentClassifierService — Dapper query
/// diab_his_cli_lab_orders pending theo encounterId, co filter tenant_id (multi-tenant
/// application-layer). Cung dieu kien voi ExtractLabResultOcrCommandHandler.LoadPendingTestsAsync.
/// </summary>
public class PendingLabTestsProvider : IPendingLabTestsProvider
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public PendingLabTestsProvider(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<(Guid LabOrderItemId, string TestCode, string TestName)>> GetPendingAsync(
        Guid encounterId, CancellationToken ct)
    {
        const string sql = @"
            SELECT  o.id         AS Id,
                    o.test_code  AS TestCode,
                    o.test_name  AS TestName
            FROM diab_his_cli_lab_orders o
            WHERE o.tenant_id = @TId
              AND o.encounter_id = @EncId
              AND o.deleted_at IS NULL
              AND o.status <> 'cancelled'
              AND NOT EXISTS (
                    SELECT 1 FROM diab_his_lab_results r
                    WHERE r.lab_order_item_id = o.id
                      AND r.tenant_id = o.tenant_id
                      AND r.deleted_at IS NULL)
            ORDER BY o.ordered_at";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(new CommandDefinition(sql,
            new { TId = _tenant.TenantId, EncId = encounterId.ToString() }, cancellationToken: ct));

        var list = new List<(Guid, string, string)>();
        foreach (var r in rows)
        {
            if (!Guid.TryParse((string?)r.Id, out var itemId)) continue;
            list.Add((itemId, (string?)r.TestCode ?? string.Empty, (string?)r.TestName ?? string.Empty));
        }
        return list;
    }
}
