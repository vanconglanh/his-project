using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy.Warehouse;

namespace ProDiabHis.Application.Pharmacy.Dispensing;

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record GetDispenseQueueQuery(int? WarehouseId, string? Q, int Page, int PageSize)
    : IRequest<Result<PagedResult<DispenseQueueItem>>>;
public record DispenseCommand(int PrescriptionId, DispenseRequest Request) : IRequest<Result<DispenseRecordResponse>>;
public record RejectDispenseCommand(string DispenseRecordId, string Reason) : IRequest<Result<DispenseRecordResponse>>;
public record ReturnDispenseCommand(string DispenseRecordId, ReturnDispenseRequest Request) : IRequest<Result<DispenseRecordResponse>>;
public record GetDispenseHistoryQuery(int? PatientId, DateOnly? FromDate, DateOnly? ToDate, string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<DispenseRecordResponse>>>;
public record GetDispenseReceiptPdfQuery(string DispenseRecordId) : IRequest<Result<byte[]>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class GetDispenseQueueHandler : IRequestHandler<GetDispenseQueueQuery, Result<PagedResult<DispenseQueueItem>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDispenseQueueHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<DispenseQueueItem>>> Handle(GetDispenseQueueQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string>
        {
            "p.tenant_id = @tenantId",
            "p.status IN ('SIGNED','SUBMITTED_DTQG')",
            "p.deleted_at IS NULL",
            "NOT EXISTS (SELECT 1 FROM diab_his_pha_dispense_records dr WHERE dr.prescription_id = p.ID AND dr.status = 'DISPENSED' AND dr.tenant_id = @tenantId)"
        };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

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
            0, (string?)r.pres_id, null, (string?)r.patient_name, null,
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

    public async Task<Result<DispenseRecordResponse>> Handle(DispenseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pres = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status, tenant_id FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.PrescriptionId, tenantId });

        if (pres == null)
            return Result<DispenseRecordResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        string presStatus = pres.status;
        if (presStatus != "SIGNED" && presStatus != "SUBMITTED_DTQG")
            return Result<DispenseRecordResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Don thuoc chua duoc ky so.");

        // Check duplicate
        var dupCheck = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_pha_dispense_records WHERE prescription_id = @presId AND tenant_id = @tenantId AND status = 'DISPENSED'",
            new { presId = cmd.PrescriptionId, tenantId });
        if (dupCheck > 0)
            return Result<DispenseRecordResponse>.Failure("PHARMACY_DISPENSE_DUPLICATE", "Don nay da duoc phat thuoc.");

        var dispenseId = Guid.NewGuid().ToString();
        var dispenseItems = new List<DispenseItemResponse>();
        decimal totalAmount = 0;

        foreach (var reqItem in cmd.Request.Items)
        {
            var presItem = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, drug_id, quantity FROM diab_his_pha_prescription_items WHERE id = @id AND tenant_id = @tenantId",
                new { id = reqItem.PrescriptionItemId, tenantId });

            if (presItem == null) continue;

            int drugId = (int)presItem.drug_id;
            decimal neededQty = (decimal)presItem.quantity;

            // Use provided batch_picks or auto FEFO
            IReadOnlyList<BatchPick> picks;
            if (reqItem.BatchPicks?.Count > 0)
            {
                picks = reqItem.BatchPicks.Select(bp => new BatchPick(bp.BatchNo, DateOnly.MinValue, bp.Quantity, 0)).ToList();
            }
            else
            {
                picks = await _fefo.PickAsync(cmd.Request.WarehouseId, tenantId, drugId, neededQty, ct);
            }

            foreach (var pick in picks)
            {
                // Get actual stock for this batch
                var stock = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, exp_date AS expiry_date, quantity AS quantity_available, import_price AS unit_cost FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND drug_id = @drug AND lot_number = @batch",
                    new { tenantId, drug = drugId, batch = pick.BatchNo });

                if (stock == null)
                    return Result<DispenseRecordResponse>.Failure("PHARMACY_BATCH_NOT_FOUND", $"Khong tim thay lo {pick.BatchNo}.");

                if ((decimal)stock.quantity_available < pick.Quantity)
                    return Result<DispenseRecordResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT", "Ton kho khong du phat thuoc.");

                decimal unitCost = (decimal)stock.unit_cost;
                decimal lineAmount = pick.Quantity * unitCost;

                // Deduct stock
                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_stock SET quantity = quantity - @qty, updated_at = NOW() WHERE id = @id",
                    new { qty = pick.Quantity, id = (string)stock.id });

                // Movement
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                      VALUES (@tenantId, @stockId, @wh, 'EXPORT', @qty, @cost, 'PRESCRIPTION', @presId, NOW(), @userId, NOW(), NOW())",
                    new { tenantId, stockId = (string)stock.id, wh = cmd.Request.WarehouseId, qty = pick.Quantity, cost = unitCost, presId = cmd.PrescriptionId, userId = 0 });

                // Dispense item
                var dispItemId = Guid.NewGuid().ToString();
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_dispense_items (id, tenant_id, dispense_record_id, prescription_item_id, drug_id, batch_no, expiry_date, quantity, unit_cost, created_at, updated_at)
                      VALUES (@id, @tenantId, @dispenseId, @presItemId, @drugId, @batchNo, @expiry, @qty, @cost, NOW(), NOW())",
                    new { id = dispItemId, tenantId, dispenseId, presItemId = reqItem.PrescriptionItemId, drugId, batchNo = pick.BatchNo, expiry = (DateTime)stock.expiry_date, qty = pick.Quantity, cost = unitCost });

                var drugName = await conn.ExecuteScalarAsync<string>("SELECT name FROM diab_his_pha_drugs WHERE id = @id", new { id = drugId });
                dispenseItems.Add(new DispenseItemResponse(dispItemId, reqItem.PrescriptionItemId, drugId, drugName,
                    pick.BatchNo, DateOnly.FromDateTime((DateTime)stock.expiry_date), pick.Quantity, unitCost, lineAmount));
                totalAmount += lineAmount;
            }
        }

        // Insert dispense record
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_dispense_records (id, tenant_id, prescription_id, warehouse_id, dispensed_at, dispensed_by, status, note, total_amount, created_at, updated_at)
              VALUES (@id, @tenantId, @presId, @wh, NOW(), @dispensedBy, 'DISPENSED', @note, @totalAmount, NOW(), NOW())",
            new { id = dispenseId, tenantId, presId = cmd.PrescriptionId, wh = cmd.Request.WarehouseId, dispensedBy = 0, note = cmd.Request.Note, totalAmount });

        // Update prescription status
        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_prescriptions SET status = 'DISPENSED', updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.PrescriptionId, tenantId });

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
        var presId = await conn.ExecuteScalarAsync<int?>(
            "SELECT prescription_id FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.DispenseRecordId, tenantId });

        // Create a rejection record
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_dispense_records (id, tenant_id, prescription_id, warehouse_id, dispensed_at, status, note, total_amount, created_at, updated_at)
              SELECT @newId, tenant_id, prescription_id, warehouse_id, NOW(), 'REJECTED', @reason, 0, NOW(), NOW()
              FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { newId = rejId, reason = cmd.Reason, id = cmd.DispenseRecordId, tenantId });

        return Result<DispenseRecordResponse>.Success(new DispenseRecordResponse(
            rejId, tenantId, presId ?? 0, 0, DateTime.UtcNow, null, null, "REJECTED", cmd.Reason, [], 0));
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
                new { tenantId, qty = retItem.Quantity, presId = (string)record.prescription_id, wh = (int)record.warehouse_id, drug = (string)di.drug_id, batch = (string)di.batch_no });

            await conn.ExecuteAsync(
                "UPDATE diab_his_pha_dispense_items SET is_returned = 1, returned_quantity = @qty, updated_at = NOW() WHERE id = @id",
                new { qty = retItem.Quantity, id = retItem.DispenseItemId });
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_dispense_records SET status = 'RETURNED', updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.DispenseRecordId, tenantId });

        return Result<DispenseRecordResponse>.Success(new DispenseRecordResponse(
            cmd.DispenseRecordId, tenantId, (int)record.prescription_id, (int)record.warehouse_id,
            DateTime.UtcNow, null, null, "RETURNED", cmd.Request.Reason, [], 0));
    }
}

public class GetDispenseHistoryHandler : IRequestHandler<GetDispenseHistoryQuery, Result<PagedResult<DispenseRecordResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDispenseHistoryHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<DispenseRecordResponse>>> Handle(GetDispenseHistoryQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "dr.tenant_id = @tenantId", "dr.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

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
            (string)r.id, (int)r.tenant_id, (int)r.prescription_id, (int)r.warehouse_id,
            (DateTime)r.dispensed_at, (int?)r.dispensed_by, null, (string)r.status, (string?)r.note, [], (decimal)r.total_amount)).ToList();

        return Result<PagedResult<DispenseRecordResponse>>.Success(new PagedResult<DispenseRecordResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetDispenseReceiptPdfHandler : IRequestHandler<GetDispenseReceiptPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDispenseReceiptPdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<byte[]>> Handle(GetDispenseReceiptPdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var record = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, prescription_id, total_amount, dispensed_at FROM diab_his_pha_dispense_records WHERE id = @id AND tenant_id = @tenantId",
            new { id = q.DispenseRecordId, tenantId });

        if (record == null)
            return Result<byte[]>.Failure("PHARMACY_BATCH_NOT_FOUND", "Khong tim thay phieu phat.");

        // Minimal PDF stub
        var content = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/MediaBox[0 0 420 595]/Parent 2 0 R/Resources<<>>/Contents 4 0 R>>endobj\n4 0 obj<</Length 40>>stream\nBT /F1 12 Tf 50 500 Td (PHIEU PHAT THUOC) Tj ET\nendstream\nendobj\nxref\n0 5\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\n0000000115 00000 n\n0000000266 00000 n\ntrailer<</Size 5/Root 1 0 R>>\nstartxref\n360\n%%EOF";
        return Result<byte[]>.Success(System.Text.Encoding.ASCII.GetBytes(content));
    }
}
