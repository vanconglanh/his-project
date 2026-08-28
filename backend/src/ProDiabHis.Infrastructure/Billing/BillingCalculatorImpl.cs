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
/// GHI CHU D11/Task-3 (goi dinh muc tra truoc): thay vi goi lai IPackageEntitlementService.QuoteAsync
/// tai day (co the cho ket qua SAI vi dinh muc cho cac nguon LAB_ORDER/RAD_ORDER/PRESCRIPTION da
/// duoc TRU THAT (ConsumeAsync) ngay luc tao chi dinh/ke don - xem AppointmentHandlers/ClsHandlers/
/// PrescriptionHandlers), ham nay tra cuu TRUC TIEP cac dong diab_his_pkg_usage_logs (action=DEDUCT,
/// chua bi REVERSE) da duoc ghi cho tung nguon o buoc do. Neu tim thay: line_total chi tinh tren
/// phan VUOT dinh muc (excess_quantity), discount_percent = ty le da duoc dinh muc chi tra, va set
/// covered_by_subscription_id / covered_by_usage_log_id de doi soat (D11). Cach nay tranh double-charge
/// (khong tinh tien phan da duoc dinh muc chi tra) ma cung khong "an" mien phi 1 dong hoa don khong co
/// deduction that su dung sau. QuoteAsync (preview, khong side-effect) chi phu hop de hien thi UI
/// TRUOC khi luu (form ke don) - da dung o do, khong phai o buoc tinh hoa don nay.
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
                var item = new BillingItem
                {
                    TenantId = tenantId,
                    ItemType = "LAB",
                    RefId = Guid.TryParse((string)o.id, out var labId) ? labId : null,
                    Code = (string?)o.code,
                    Name = (string)o.name,
                    Quantity = 1,
                    UnitPrice = price,
                    LineTotal = price
                };
                if (labId != Guid.Empty) await ApplyPackageCoverageAsync(conn, tenantId, "LAB_ORDER", labId, null, item);
                items.Add(item);
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
                var item = new BillingItem
                {
                    TenantId = tenantId,
                    ItemType = "RAD",
                    RefId = Guid.TryParse((string)o.id, out var radId) ? radId : null,
                    Code = (string?)o.code,
                    Name = (string)o.name,
                    Quantity = 1,
                    UnitPrice = price,
                    LineTotal = price
                };
                if (radId != Guid.Empty) await ApplyPackageCoverageAsync(conn, tenantId, "RAD_ORDER", radId, null, item);
                items.Add(item);
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
                    @"SELECT di.id, di.prescription_item_id, di.drug_id, d.name, di.quantity, di.unit_cost
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
                    var item = new BillingItem
                    {
                        TenantId = tenantId,
                        ItemType = "DRUG",
                        RefId = Guid.TryParse((string)di.drug_id, out var drugId) ? drugId : null,
                        Name = (string)di.name,
                        Quantity = qty,
                        UnitPrice = price,
                        LineTotal = qty * price,
                        BhytApplicable = true
                    };
                    // Nguon deduct cua thuoc la PRESCRIPTION, khoa theo prescription_item_id (source_item_id
                    // trong pkg_usage_logs) - xem CreatePrescriptionHandler.
                    if (Guid.TryParse((string?)di.prescription_item_id, out var presItemId) && presItemId != Guid.Empty)
                        await ApplyPackageCoverageAsync(conn, tenantId, "PRESCRIPTION", null, presItemId, item);
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Khong the doc phieu phat thuoc cho encounter {EncounterId}, bo qua nguon nay.", encounterId);
            }
        }

        return items;
    }

    /// <summary>
    /// D11 - tra cuu dong diab_his_pkg_usage_logs (action=DEDUCT, chua bi REVERSE) da ghi luc
    /// tao chi dinh CLS / ke don thuoc de biet phan da duoc dinh muc goi chi tra, roi dieu chinh
    /// item hoa don: chi tinh tien phan VUOT dinh muc (excess), danh dau covered_by_subscription_id /
    /// covered_by_usage_log_id. Neu khong tim thay log nao (khong dung goi / chua tru) -> giu nguyen
    /// gia goc, khong doi gi (dam bao khong "mien phi" khi khong co deduction that su).
    /// </summary>
    private static async Task ApplyPackageCoverageAsync(
        System.Data.IDbConnection conn, int tenantId, string sourceType, Guid? sourceId, Guid? sourceItemId, BillingItem item)
    {
        var log = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT l.id, l.subscription_id, l.covered_quantity, l.excess_quantity
              FROM diab_his_pkg_usage_logs l
              WHERE l.tenant_id=@tenantId AND l.source_type=@sourceType
                AND (@sourceId IS NULL OR l.source_id=@sourceId)
                AND (@sourceItemId IS NULL OR l.source_item_id=@sourceItemId)
                AND l.action='DEDUCT'
                AND NOT EXISTS (SELECT 1 FROM diab_his_pkg_usage_logs r WHERE r.reversal_of_id=l.id AND r.action='REVERSE')
              ORDER BY l.used_at DESC LIMIT 1",
            new
            {
                tenantId,
                sourceType,
                sourceId = sourceId?.ToString(),
                sourceItemId = sourceItemId?.ToString()
            });

        if (log == null) return;

        var coveredQty = (decimal)log.covered_quantity;
        var excessQty = (decimal)log.excess_quantity;
        if (coveredQty <= 0) return;

        var totalQty = coveredQty + excessQty;
        item.LineTotal = totalQty > 0 ? Math.Round(item.UnitPrice * excessQty, 2) : 0;
        item.DiscountPercent = totalQty > 0 ? Math.Round(coveredQty / totalQty * 100, 2) : 100;
        item.CoveredBySubscriptionId = Guid.Parse((string)log.subscription_id);
        item.CoveredByUsageLogId = Guid.Parse((string)log.id);
    }
}
