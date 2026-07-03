using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// FEFO (First Expired, First Out) strategy implementation.
/// Queries pha_stocks ordered by expiry_date ASC, skips expired batches,
/// accumulates until quantity is fulfilled.
/// </summary>
public class FefoStrategyImpl : IFefoStrategy
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<FefoStrategyImpl> _logger;

    public FefoStrategyImpl(IDapperConnectionFactory db, ILogger<FefoStrategyImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BatchPick>> PickAsync(string warehouseId, int tenantId, string drugId, decimal quantityNeeded, CancellationToken ct = default)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        // Query available batches from diab_his_pha_stock (schema moi CHAR(36)),
        // FEFO order theo exp_date ASC, bo qua lo het han / het ton.
        // Ton kho khong scope theo kho (warehouse_id) o schema hien tai —
        // warehouseId chi de log/tra ve, khong dua vao WHERE.
        var batches = (await conn.QueryAsync<dynamic>(
            @"SELECT lot_number     AS batch_no,
                     exp_date       AS expiry_date,
                     quantity       AS quantity_available,
                     import_price   AS unit_cost
              FROM diab_his_pha_stock
              WHERE tenant_id = @tenantId
                AND drug_id   = @drugId
                AND quantity  > 0
                AND exp_date  > CURDATE()
              ORDER BY exp_date ASC",
            new { tenantId, drugId })).ToList();

        var picks = new List<BatchPick>();
        decimal remaining = quantityNeeded;

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            decimal available = (decimal)batch.quantity_available;
            decimal pickQty = Math.Min(available, remaining);

            picks.Add(new BatchPick(
                (string)batch.batch_no,
                DateOnly.FromDateTime((DateTime)batch.expiry_date),
                pickQty,
                (decimal)batch.unit_cost));

            remaining -= pickQty;
            _logger.LogDebug("FEFO pick: drug={DrugId} batch={Batch} qty={Qty}", drugId, (string)batch.batch_no, pickQty);
        }

        if (remaining > 0)
        {
            _logger.LogWarning("FEFO insufficient stock: drug={DrugId} wh={Wh} needed={Needed} shortfall={Short}",
                drugId, warehouseId, quantityNeeded, remaining);
            throw new InvalidOperationException($"PHARMACY_STOCK_INSUFFICIENT:Ton kho khong du (con thieu {remaining})");
        }

        return picks;
    }
}
