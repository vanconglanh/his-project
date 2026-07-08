using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Gom items tu encounter: dich vu chi dinh (hien chua co bang luu tru — bo qua) +
/// CLS orders (Lab/Rad) + phieu phat thuoc (dispense).
///
/// GHI CHU BUG FIX (Thao, xem yeu cau QC): ban goc tham chieu cac bang LEGACY khong
/// ton tai tren schema hien tai (his_encounter_services, his_cls_orders,
/// his_dispense_records, his_dispense_items, his_drugs) khien tao hoa don luon 500.
/// Da doi lai dung ten bang/cot thuc te (doi chieu db/migrations):
///   - his_cls_orders            -> diab_his_cli_lab_orders + diab_his_cli_rad_orders
///                                  (gia lay tu catalog diab_his_dict_lab_tests /
///                                   diab_his_dict_rad_procedures theo ma test/thu thuat,
///                                   vi 2 bang order khong tu luu gia)
///   - his_dispense_records/items -> diab_his_pha_dispense_records + diab_his_pha_dispense_items
///                                  (gia = unit_cost, khong co unit_price)
///   - his_drugs                 -> diab_his_pha_drugs
///   - his_encounter_services    -> KHONG TON TAI trong schema (chua co tinh nang chi dinh
///                                  dich vu roi luu vao bang rieng theo encounter). Bo qua
///                                  nguon nay, tra danh sach rong thay vi query bang khong
///                                  ton tai (tranh 500).
/// </summary>
public class BillingCalculatorImpl : IBillingCalculator
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BillingCalculatorImpl> _logger;

    public BillingCalculatorImpl(IDapperConnectionFactory db, ILogger<BillingCalculatorImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<BillingItem>> BuildItemsFromEncounterAsync(
        Guid encounterId, int tenantId, bool includeDispensing, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var items = new List<BillingItem>();
        var eid = encounterId.ToString();

        // 1. Dich vu da chi dinh theo lượt kham (encounter services)
        // KHONG co bang luu tru chi dinh dich vu theo encounter trong schema hien tai
        // (khong ton tai his_encounter_services / diab_his_enc_services). Bo qua nguon nay
        // (khong query) de tranh loi bang khong ton tai; tra danh sach rong cho nguon nay.

        // 2. CLS Orders (Lab + Rad) — dung dung ten bang: diab_his_cli_lab_orders / diab_his_cli_rad_orders
        try
        {
            var labOrders = await conn.QueryAsync<dynamic>(
                @"SELECT o.id, o.test_code AS code, o.test_name AS name,
                         COALESCE(t.default_price, 0) AS price
                  FROM diab_his_cli_lab_orders o
                  LEFT JOIN diab_his_dict_lab_tests t ON t.code = o.test_code
                  WHERE o.encounter_id = @eid AND o.tenant_id = @tenantId
                    AND o.status <> 'cancelled' AND o.deleted_at IS NULL",
                new { eid, tenantId });

            foreach (var o in labOrders)
            {
                var price = (decimal)o.price;
                items.Add(new BillingItem
                {
                    TenantId = tenantId,
                    ItemType = "LAB",
                    RefId = Guid.TryParse((string)o.id, out var labId) ? labId : null,
                    Code = (string?)o.code,
                    Name = (string)o.name,
                    Quantity = 1,
                    UnitPrice = price,
                    LineTotal = price
                });
            }

            var radOrders = await conn.QueryAsync<dynamic>(
                @"SELECT o.id, o.procedure_code AS code, o.procedure_name AS name,
                         COALESCE(r.default_price, 0) AS price
                  FROM diab_his_cli_rad_orders o
                  LEFT JOIN diab_his_dict_rad_procedures r ON r.code = o.procedure_code
                  WHERE o.encounter_id = @eid AND o.tenant_id = @tenantId
                    AND o.status <> 'cancelled' AND o.deleted_at IS NULL",
                new { eid, tenantId });

            foreach (var o in radOrders)
            {
                var price = (decimal)o.price;
                items.Add(new BillingItem
                {
                    TenantId = tenantId,
                    ItemType = "RAD",
                    RefId = Guid.TryParse((string)o.id, out var radId) ? radId : null,
                    Code = (string?)o.code,
                    Name = (string)o.name,
                    Quantity = 1,
                    UnitPrice = price,
                    LineTotal = price
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong the doc chi dinh CLS cho encounter {EncounterId}, bo qua nguon nay.", encounterId);
        }

        // 3. Phieu phat thuoc (dispense) — dung dung ten bang: diab_his_pha_dispense_records/items
        if (includeDispensing)
        {
            try
            {
                var dispenseItems = await conn.QueryAsync<dynamic>(
                    @"SELECT di.id, di.drug_id, d.name, di.quantity, di.unit_cost
                      FROM diab_his_pha_dispense_records dr
                      JOIN diab_his_pha_dispense_items di ON di.dispense_record_id = dr.id
                      JOIN diab_his_pha_drugs d ON d.id = di.drug_id
                      WHERE dr.prescription_id IN (
                                SELECT id FROM diab_his_pha_prescriptions
                                WHERE encounter_id = @eid AND tenant_id = @tenantId)
                        AND dr.tenant_id = @tenantId
                        AND dr.status IN ('DISPENSED', 'PARTIAL')
                        AND di.deleted_at IS NULL",
                    new { eid, tenantId });

                foreach (var di in dispenseItems)
                {
                    var qty = (decimal)di.quantity;
                    var price = (decimal)di.unit_cost;
                    items.Add(new BillingItem
                    {
                        TenantId = tenantId,
                        ItemType = "DRUG",
                        RefId = Guid.TryParse((string)di.drug_id, out var drugId) ? drugId : null,
                        Name = (string)di.name,
                        Quantity = qty,
                        UnitPrice = price,
                        LineTotal = qty * price,
                        BhytApplicable = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Khong the doc phieu phat thuoc cho encounter {EncounterId}, bo qua nguon nay.", encounterId);
            }
        }

        return items;
    }
}
