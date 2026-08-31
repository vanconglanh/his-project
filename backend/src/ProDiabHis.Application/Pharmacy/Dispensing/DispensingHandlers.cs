using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy.Warehouse;

namespace ProDiabHis.Application.Pharmacy.Dispensing;

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record GetDispenseQueueQuery(string? WarehouseId, string? Q, int Page, int PageSize)
    : IRequest<Result<PagedResult<DispenseQueueItem>>>;
public record DispenseCommand(string PrescriptionId, DispenseRequest Request) : IRequest<Result<DispenseRecordResponse>>;
public record RejectDispenseCommand(string DispenseRecordId, string Reason) : IRequest<Result<DispenseRecordResponse>>;
public record ReturnDispenseCommand(string DispenseRecordId, ReturnDispenseRequest Request) : IRequest<Result<DispenseRecordResponse>>;
public record GetDispenseHistoryQuery(string? PatientId, DateOnly? FromDate, DateOnly? ToDate, string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<DispenseRecordResponse>>>;
public record GetDispenseReceiptPdfQuery(string DispenseRecordId) : IRequest<Result<byte[]>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class GetDispenseQueueHandler : IRequestHandler<GetDispenseQueueQuery, Result<PagedResult<DispenseQueueItem>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public GetDispenseQueueHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<PagedResult<DispenseQueueItem>>> Handle(GetDispenseQueueQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;
        var (branchId, ignoreBranch) = BranchSql.Params(_branch);

        var where = new List<string>
        {
            "p.tenant_id = @tenantId",
            "p.status IN ('SIGNED','SUBMITTED_DTQG')",
            "p.deleted_at IS NULL",
            BranchSql.Condition("p"),
            "NOT EXISTS (SELECT 1 FROM diab_his_pha_dispense_records dr WHERE dr.prescription_id = p.ID AND dr.status = 'DISPENSED' AND dr.tenant_id = @tenantId)"
        };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);
        prm.Add("branchId", branchId); prm.Add("ignoreBranch", ignoreBranch);

        if (!string.IsNullOrWhiteSpace(q.Q)) { where.Add("(pat.full_name LIKE @q OR p.ID LIKE @q)"); prm.Add("q", $"%{q.Q}%"); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_prescriptions p LEFT JOIN diab_his_pat_patients pat ON pat.id = p.patient_id WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT p.id as pres_id, pat.full_name as patient_name, p.signed_at, 0 as total_amount,
                      (SELECT COUNT(*) FROM diab_his_pha_prescription_items pi WHERE pi.prescription_id = p.id) as items_count
               FROM diab_his_pha_prescriptions p
               LEFT JOIN diab_his_pat_patients pat ON pat.id = p.patient_id
               WHERE {wc} ORDER BY p.signed_at ASC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new DispenseQueueItem(
            (string)r.pres_id, null, null, (string?)r.patient_name, null,
            (DateTime?)r.signed_at, (int)(r.items_count ?? 0L), (decimal)(r.total_amount ?? 0m), false)).ToList();

        return Result<PagedResult<DispenseQueueItem>>.Success(new PagedResult<DispenseQueueItem>(items, q.Page, q.PageSize, total));
    }
}

public class DispenseHandler : IRequestHandler<DispenseCommand, Result<DispenseRecordResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFefoStrategy _fefo;
    private readonly IAuditService _audit;
    private readonly ICucQldLienThong _cucQld;

    public DispenseHandler(IDapperConnectionFactory db, ICurrentUser currentUser,
        IFefoStrategy fefo, IAuditService audit, ICucQldLienThong cucQld)
    {
        _db = db;
        _currentUser = currentUser;
        _fefo = fefo;
        _audit = audit;
        _cucQld = cucQld;
    }

    // Ban ke hoach cap phat mot dong lo — tinh truoc TOAN BO khi chua ghi gi vao DB.
    private sealed record DispensePlanLine(
        string PrescriptionItemId, string DrugId, string DrugName, string StockId,
        string BatchNo, DateTime Expiry, decimal Quantity, decimal UnitCost);

    public async Task<Result<DispenseRecordResponse>> Handle(DispenseCommand cmd, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;

        // ── GIAI DOAN 1: LEN KE HOACH (chi doc, KHONG ghi) ─────────────────────
        // Tinh truoc tat ca cac dong lo can tru + kiem du ton cho MOI dong. Neu bat ky
        // dong nao khong du ton / thieu lo -> tra loi ro rang NGAY, chua dong vao kho.
        // (BUG-01: truoc day tru kho ngay trong vong lap, dong sau loi thi dong truoc da mat.)
        var plan = new List<DispensePlanLine>();
        decimal totalAmount = 0;
        var plannedDeductByStock = new Dictionary<string, decimal>();

        using (var readConn = (IDbConnection)_db.CreateConnection())
        {
            var pres = await readConn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, status, tenant_id FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { id = cmd.PrescriptionId, tenantId });

            if (pres == null)
                return Result<DispenseRecordResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Không tìm thấy đơn thuốc.");

            string presStatus = pres.status;
            if (presStatus != "SIGNED" && presStatus != "SUBMITTED_DTQG")
                return Result<DispenseRecordResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Đơn thuốc chưa được ký số.");

            var dupCheck = await readConn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_pha_dispense_records WHERE prescription_id = @presId AND tenant_id = @tenantId AND status = 'DISPENSED'",
                new { presId = cmd.PrescriptionId, tenantId });
            if (dupCheck > 0)
                return Result<DispenseRecordResponse>.Failure("PHARMACY_DISPENSE_DUPLICATE", "Đơn này đã được phát thuốc.");

            foreach (var reqItem in cmd.Request.Items)
            {
                var presItem = await readConn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, drug_id, quantity FROM diab_his_pha_prescription_items WHERE id = @id AND tenant_id = @tenantId",
                    new { id = reqItem.PrescriptionItemId, tenantId });

                if (presItem == null) continue;

                string drugId = (string)presItem.drug_id;
                decimal neededQty = (decimal)presItem.quantity;

                var drugName = await readConn.ExecuteScalarAsync<string>(
                    "SELECT name FROM diab_his_pha_drugs WHERE id = @id", new { id = drugId }) ?? "";

                // Chon lo: theo batch_picks nguoi dung chi dinh, hoac tu dong FEFO.
                IReadOnlyList<BatchPick> picks;
                if (reqItem.BatchPicks?.Count > 0)
                {
                    picks = reqItem.BatchPicks.Select(bp => new BatchPick(bp.BatchNo, DateOnly.MinValue, bp.Quantity, 0)).ToList();
                }
                else
                {
                    try
                    {
                        picks = await _fefo.PickAsync(cmd.Request.WarehouseId, tenantId, drugId, neededQty, ct);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.StartsWith("PHARMACY_STOCK_INSUFFICIENT"))
                    {
                        // BUG-07/BUG-01: bien loi nghiep vu "het ton" thanh thong bao ro rang (422),
                        // KHONG de exception noi ra middleware -> tranh 500 "vui long thu lai sau"
                        // (khien duoc si bam lai nhieu lan -> BUG-01 tru kho lap lai).
                        var detail = ex.Message.Contains(':') ? ex.Message.Split(':', 2)[1] : "Tồn kho không đủ để phát thuốc.";
                        return Result<DispenseRecordResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT",
                            $"Không đủ tồn kho để phát \"{drugName}\": {detail}");
                    }
                }

                foreach (var pick in picks)
                {
                    var stock = await readConn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT id, exp_date AS expiry_date, quantity AS quantity_available, import_price AS unit_cost FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND drug_id = @drug AND lot_number = @batch",
                        new { tenantId, drug = drugId, batch = pick.BatchNo });

                    if (stock == null)
                        return Result<DispenseRecordResponse>.Failure("PHARMACY_BATCH_NOT_FOUND",
                            $"Không tìm thấy lô {pick.BatchNo} của thuốc \"{drugName}\".");

                    string stockId = (string)stock.id;
                    decimal available = (decimal)stock.quantity_available;
                    // Tru di phan da du kien lay o cac dong truoc cua CHINH lo nay (tranh dem trung).
                    decimal alreadyPlanned = plannedDeductByStock.GetValueOrDefault(stockId, 0m);
                    if (available - alreadyPlanned < pick.Quantity)
                        return Result<DispenseRecordResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT",
                            $"Không đủ tồn kho để phát \"{drugName}\" (lô {pick.BatchNo}): còn {available - alreadyPlanned}, cần {pick.Quantity}.");

                    decimal unitCost = (decimal)stock.unit_cost;
                    plannedDeductByStock[stockId] = alreadyPlanned + pick.Quantity;
                    plan.Add(new DispensePlanLine(
                        reqItem.PrescriptionItemId, drugId, drugName, stockId,
                        pick.BatchNo, (DateTime)stock.expiry_date, pick.Quantity, unitCost));
                    totalAmount += pick.Quantity * unitCost;
                }
            }
        }

        if (plan.Count == 0)
            return Result<DispenseRecordResponse>.Failure("PHARMACY_NOTHING_TO_DISPENSE",
                "Không có dòng thuốc hợp lệ để phát.");

        var dispenseId = Guid.NewGuid().ToString();
        var dispenseItems = new List<DispenseItemResponse>();

        // ── GIAI DOAN 2: GHI (trong 1 TRANSACTION duy nhat) ────────────────────
        // Toan bo tru kho + tao phieu phat + phieu xuat + cap nhat trang thai don nam
        // trong CUNG 1 transaction: bat ky loi nao -> rollback SACH, khong that thoat ton kho.
        using (var conn = (IDbConnection)_db.CreateConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var line in plan)
                {
                    // Khoa dong ton kho + tai kiem tra trong transaction (chong race dong thoi).
                    var current = await conn.ExecuteScalarAsync<decimal?>(
                        "SELECT quantity FROM diab_his_pha_stock WHERE id = @id FOR UPDATE",
                        new { id = line.StockId }, tx);
                    if (current == null || current.Value < line.Quantity)
                    {
                        tx.Rollback();
                        return Result<DispenseRecordResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT",
                            $"Không đủ tồn kho để phát \"{line.DrugName}\" (lô {line.BatchNo}).");
                    }

                    await conn.ExecuteAsync(
                        "UPDATE diab_his_pha_stock SET quantity = quantity - @qty, updated_at = NOW() WHERE id = @id",
                        new { qty = line.Quantity, id = line.StockId }, tx);

                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                          VALUES (@tenantId, @stockId, @wh, 'EXPORT', @qty, @cost, 'PRESCRIPTION', @presId, NOW(), @userId, NOW(), NOW())",
                        new { tenantId, stockId = line.StockId, wh = cmd.Request.WarehouseId, qty = line.Quantity, cost = line.UnitCost, presId = cmd.PrescriptionId, userId = 0 }, tx);

                    var dispItemId = Guid.NewGuid().ToString();
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pha_dispense_items (id, tenant_id, dispense_record_id, prescription_item_id, drug_id, batch_no, expiry_date, quantity, unit_cost, created_at, updated_at)
                          VALUES (@id, @tenantId, @dispenseId, @presItemId, @drugId, @batchNo, @expiry, @qty, @cost, NOW(), NOW())",
                        new { id = dispItemId, tenantId, dispenseId, presItemId = line.PrescriptionItemId, drugId = line.DrugId, batchNo = line.BatchNo, expiry = line.Expiry, qty = line.Quantity, cost = line.UnitCost }, tx);

                    dispenseItems.Add(new DispenseItemResponse(dispItemId, line.PrescriptionItemId, line.DrugId, line.DrugName,
                        line.BatchNo, DateOnly.FromDateTime(line.Expiry), line.Quantity, line.UnitCost, line.Quantity * line.UnitCost));
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_dispense_records (id, tenant_id, prescription_id, warehouse_id, dispensed_at, dispensed_by, status, note, total_amount, created_at, updated_at)
                      VALUES (@id, @tenantId, @presId, @wh, NOW(), @dispensedBy, 'DISPENSED', @note, @totalAmount, NOW(), NOW())",
                    new { id = dispenseId, tenantId, presId = cmd.PrescriptionId, wh = cmd.Request.WarehouseId, dispensedBy = 0, note = cmd.Request.Note, totalAmount }, tx);

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_prescriptions SET status = 'DISPENSED', updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
                    new { id = cmd.PrescriptionId, tenantId }, tx);

                tx.Commit();
            }
            catch (Exception)
            {
                try { tx.Rollback(); } catch { /* connection da hong — khong con gi de rollback */ }
                // Khong that thoat ton kho vi tat ca nam trong transaction da rollback.
                return Result<DispenseRecordResponse>.Failure("PHARMACY_DISPENSE_FAILED",
                    "Không thể tạo phiếu phát thuốc do lỗi hệ thống. Tồn kho được giữ nguyên, vui lòng thử lại.");
            }
        }

        // ── Sau khi commit: cac tac dong phu (audit, lien thong Cuc QLD) ───────
        await _audit.LogAsync("DISPENSE", "pha_prescriptions", cmd.PrescriptionId.ToString(), new { dispenseId, status = "DISPENSED" }, ct);
        await _cucQld.ReportExportAsync(Guid.Parse(dispenseId), ct);

        return Result<DispenseRecordResponse>.Success(new DispenseRecordResponse(
            dispenseId, tenantId, cmd.PrescriptionId, cmd.Request.WarehouseId,
            DateTime.UtcNow, null, null, "DISPENSED", cmd.Request.Note, dispenseItems, totalAmount));
    }
}

public class RejectDispenseHandler : IRequestHandler<RejectDispenseCommand, Result<DispenseRecordResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public RejectDispenseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DispenseRecordResponse>> Handle(RejectDispenseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rejId = Guid.NewGuid().ToString();
        var presId = await conn.ExecuteScalarAsync<string?>(
            "SELECT prescription_id FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.DispenseRecordId, tenantId });

        // Create a rejection record
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_dispense_records (id, tenant_id, prescription_id, warehouse_id, dispensed_at, status, note, total_amount, created_at, updated_at)
              SELECT @newId, tenant_id, prescription_id, warehouse_id, NOW(), 'REJECTED', @reason, 0, NOW(), NOW()
              FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { newId = rejId, reason = cmd.Reason, id = cmd.DispenseRecordId, tenantId });

        return Result<DispenseRecordResponse>.Success(new DispenseRecordResponse(
            rejId, tenantId, presId ?? "", "", DateTime.UtcNow, null, null, "REJECTED", cmd.Reason, [], 0));
    }
}

public class ReturnDispenseHandler : IRequestHandler<ReturnDispenseCommand, Result<DispenseRecordResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ReturnDispenseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<DispenseRecordResponse>> Handle(ReturnDispenseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var record = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, prescription_id, warehouse_id, status FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.DispenseRecordId, tenantId });

        if (record == null)
            return Result<DispenseRecordResponse>.Failure("PHARMACY_BATCH_NOT_FOUND", "Khong tim thay phieu phat.");

        foreach (var retItem in cmd.Request.Items)
        {
            var di = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, drug_id, batch_no, unit_cost, dispense_record_id FROM diab_his_pha_dispense_items WHERE id = @id AND tenant_id = @tenantId",
                new { id = retItem.DispenseItemId, tenantId });

            if (di == null) continue;

            // Return stock
            await conn.ExecuteAsync(
                @"UPDATE diab_his_pha_stock SET quantity = quantity + @qty, updated_at = NOW()
                  WHERE tenant_id = @tenantId AND drug_id = @drug AND lot_number = @batch",
                new { qty = retItem.Quantity, tenantId, drug = (string)di.drug_id, batch = (string)di.batch_no });

            // Movement RETURN
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                  SELECT @tenantId, id, @wh, 'RETURN', @qty, 'PRESCRIPTION', @presId, NOW(), 0, NOW(), NOW()
                  FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND drug_id = @drug AND lot_number = @batch LIMIT 1",
                new { tenantId, qty = retItem.Quantity, presId = (string)record.prescription_id, wh = (string)record.warehouse_id, drug = (string)di.drug_id, batch = (string)di.batch_no });

            await conn.ExecuteAsync(
                "UPDATE diab_his_pha_dispense_items SET is_returned = 1, returned_quantity = @qty, updated_at = NOW() WHERE id = @id",
                new { qty = retItem.Quantity, id = retItem.DispenseItemId });
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_dispense_records SET status = 'RETURNED', updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.DispenseRecordId, tenantId });

        return Result<DispenseRecordResponse>.Success(new DispenseRecordResponse(
            cmd.DispenseRecordId, tenantId, (string)record.prescription_id, (string)record.warehouse_id,
            DateTime.UtcNow, null, null, "RETURNED", cmd.Request.Reason, [], 0));
    }
}

public class GetDispenseHistoryHandler : IRequestHandler<GetDispenseHistoryQuery, Result<PagedResult<DispenseRecordResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public GetDispenseHistoryHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<PagedResult<DispenseRecordResponse>>> Handle(GetDispenseHistoryQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;
        var (branchId, ignoreBranch) = BranchSql.Params(_branch);

        var where = new List<string> { "dr.tenant_id = @tenantId", "dr.deleted_at IS NULL", BranchSql.Condition("dr") };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);
        prm.Add("branchId", branchId); prm.Add("ignoreBranch", ignoreBranch);

        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("dr.status = @status"); prm.Add("status", q.Status); }
        if (q.FromDate.HasValue) { where.Add("DATE(dr.dispensed_at) >= @fromDate"); prm.Add("fromDate", q.FromDate.Value); }
        if (q.ToDate.HasValue) { where.Add("DATE(dr.dispensed_at) <= @toDate"); prm.Add("toDate", q.ToDate.Value); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_dispense_records dr WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT dr.id, dr.tenant_id, dr.prescription_id, dr.warehouse_id,
                      dr.dispensed_at, dr.dispensed_by, dr.status, dr.note, dr.total_amount
               FROM diab_his_pha_dispense_records dr
               WHERE {wc} ORDER BY dr.dispensed_at DESC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new DispenseRecordResponse(
            (string)r.id, (int)r.tenant_id, (string)r.prescription_id, (string)r.warehouse_id,
            (DateTime)r.dispensed_at, (int?)r.dispensed_by, null, (string)r.status, (string?)r.note, [], (decimal)r.total_amount)).ToList();

        return Result<PagedResult<DispenseRecordResponse>>.Success(new PagedResult<DispenseRecordResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetDispenseReceiptPdfHandler : IRequestHandler<GetDispenseReceiptPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;
    private readonly IPharmacyDispenseReceiptPdfBuilder _builder;

    public GetDispenseReceiptPdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch, IPharmacyDispenseReceiptPdfBuilder builder)
    { _db = db; _currentUser = currentUser; _branch = branch; _builder = builder; }

    public async Task<Result<byte[]>> Handle(GetDispenseReceiptPdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var (branchId, ignoreBranch) = BranchSql.Params(_branch);

        var record = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $@"SELECT dr.id, dr.prescription_id, dr.total_amount, dr.dispensed_at, dr.note,
                     COALESCE(pat.full_name, '') AS patient_name, pat.code AS patient_code
              FROM diab_his_pha_dispense_records dr
              LEFT JOIN diab_his_pha_prescriptions p ON p.id = dr.prescription_id AND p.tenant_id = dr.tenant_id
              LEFT JOIN diab_his_pat_patients pat ON pat.id = p.patient_id AND pat.tenant_id = dr.tenant_id
              WHERE dr.id = @id AND dr.tenant_id = @tenantId AND {BranchSql.Condition("dr")}",
            new { id = q.DispenseRecordId, tenantId, branchId, ignoreBranch });

        if (record == null)
            return Result<byte[]>.Failure("PHARMACY_BATCH_NOT_FOUND", "Khong tim thay phieu phat.");

        var itemRows = await conn.QueryAsync<dynamic>(
            @"SELECT di.batch_no, di.expiry_date, di.quantity,
                     COALESCE(d.name, d.name_vi) AS drug_name, d.unit AS unit
              FROM diab_his_pha_dispense_items di
              LEFT JOIN diab_his_pha_drugs d ON d.id = di.drug_id AND d.tenant_id = di.tenant_id
              WHERE di.dispense_record_id = @id AND di.tenant_id = @tenantId
              ORDER BY di.created_at ASC",
            new { id = q.DispenseRecordId, tenantId });

        var items = itemRows.Select((r, idx) => new DispenseReceiptItem(
            idx + 1,
            (string?)r.drug_name ?? "—",
            (string?)r.unit,
            (decimal)r.quantity,
            (string?)r.batch_no,
            r.expiry_date == null ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)r.expiry_date))).ToList();

        var lh = await conn.QueryFirstOrDefaultAsync<Reports.LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });
        lh ??= new Reports.LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var data = new DispenseReceiptData(
            lh, (string)record.id, (string)record.prescription_id,
            string.IsNullOrWhiteSpace((string)record.patient_name) ? null : (string)record.patient_name,
            (string?)record.patient_code,
            (DateTime)record.dispensed_at, (string?)record.note, items, (decimal)record.total_amount);

        var pdf = _builder.Build(data);
        return Result<byte[]>.Success(pdf);
    }
}
