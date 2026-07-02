using Dapper;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Gom items tu encounter: services da chi dinh + cls_orders + prescriptions + dispense records
/// </summary>
public class BillingCalculatorImpl : IBillingCalculator
{
    private readonly IDapperConnectionFactory _db;

    public BillingCalculatorImpl(IDapperConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<BillingItem>> BuildItemsFromEncounterAsync(
        Guid encounterId, int tenantId, bool includeDispensing, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var items = new List<BillingItem>();
        var eid = encounterId.ToString();

        // 1. Services from encounter_services (if table exists)
        var services = await conn.QueryAsync<dynamic>(
            @"SELECT es.service_id, s.name, s.price, s.vat_rate, s.bhyt_code, es.quantity
              FROM his_encounter_services es
              JOIN diab_his_bil_services s ON s.id = es.service_id
              WHERE es.encounter_id = @eid AND es.tenant_id = @tenantId",
            new { eid, tenantId });

        foreach (var s in services)
        {
            var lineTotal = (decimal)s.price * (int)s.quantity;
            items.Add(new BillingItem
            {
                TenantId = tenantId,
                ItemType = "SERVICE",
                RefId = Guid.Parse((string)s.service_id),
                Name = (string)s.name,
                Quantity = (int)s.quantity,
                UnitPrice = (decimal)s.price,
                VatRate = (int)s.vat_rate,
                LineTotal = lineTotal,
                BhytApplicable = !string.IsNullOrEmpty((string?)s.bhyt_code)
            });
        }

        // 2. CLS Orders (Lab + Rad)
        var clsOrders = await conn.QueryAsync<dynamic>(
            @"SELECT o.id, o.order_type, o.service_name, o.price, o.quantity
              FROM his_cls_orders o
              WHERE o.encounter_id = @eid AND o.tenant_id = @tenantId AND o.status != 'CANCELLED'",
            new { eid, tenantId });

        foreach (var o in clsOrders)
        {
            var qty = (decimal)(o.quantity ?? 1);
            var price = (decimal)(o.price ?? 0);
            items.Add(new BillingItem
            {
                TenantId = tenantId,
                ItemType = (string)o.order_type == "LAB" ? "LAB" : "RAD",
                RefId = Guid.Parse((string)o.id),
                Name = (string)o.service_name,
                Quantity = qty,
                UnitPrice = price,
                LineTotal = price * qty
            });
        }

        // 3. Prescriptions / Dispensing
        if (includeDispensing)
        {
            var dispenseItems = await conn.QueryAsync<dynamic>(
                @"SELECT di.drug_id, d.name, di.quantity, di.unit_price
                  FROM his_dispense_records dr
                  JOIN his_dispense_items di ON di.dispense_id = dr.id
                  JOIN his_drugs d ON d.id = di.drug_id
                  WHERE dr.encounter_id = @eid AND dr.tenant_id = @tenantId AND dr.status != 'CANCELLED'",
                new { eid, tenantId });

            foreach (var di in dispenseItems)
            {
                var qty = (decimal)di.quantity;
                var price = (decimal)di.unit_price;
                items.Add(new BillingItem
                {
                    TenantId = tenantId,
                    ItemType = "DRUG",
                    RefId = Guid.Parse((string)di.drug_id),
                    Name = (string)di.name,
                    Quantity = qty,
                    UnitPrice = price,
                    LineTotal = qty * price,
                    BhytApplicable = true
                });
            }
        }

        return items;
    }
}
