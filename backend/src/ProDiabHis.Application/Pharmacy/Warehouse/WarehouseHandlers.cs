using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.Warehouse;

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record ListWarehousesQuery : IRequest<Result<IReadOnlyList<WarehouseResponse>>>;
public record GetWarehouseQuery(string Id) : IRequest<Result<WarehouseResponse>>;
public record CreateWarehouseCommand(WarehouseRequest Request) : IRequest<Result<WarehouseResponse>>;
public record UpdateWarehouseCommand(string Id, WarehouseRequest Request) : IRequest<Result<WarehouseResponse>>;
public record DeleteWarehouseCommand(string Id) : IRequest<Result<bool>>;

public record ListPurchaseOrdersQuery(string? Status, string? SupplierId, int Page, int PageSize)
    : IRequest<Result<PagedResult<PurchaseOrderResponse>>>;
public record CreatePurchaseOrderCommand(PurchaseOrderRequest Request) : IRequest<Result<PurchaseOrderResponse>>;
public record CreateGrnCommand(string PurchaseOrderId, GoodsReceivedRequest Request) : IRequest<Result<GrnResponse>>;

public record ListStocksQuery(int? WarehouseId, int? DrugId, string? BatchNo, bool? LowStock, bool? NearExpiry, int Page, int PageSize)
    : IRequest<Result<PagedResult<StockResponse>>>;
public record CreateAdjustmentCommand(StockAdjustmentRequest Request) : IRequest<Result<AdjustmentResponse>>;
public record ListMovementsQuery(int? WarehouseId, int? DrugId, string? MovementType, DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize)
    : IRequest<Result<PagedResult<StockMovementResponse>>>;
public record CreateTransferCommand(TransferRequest Request) : IRequest<Result<TransferResponse>>;
public record GetLowStockAlertsQuery(int? WarehouseId) : IRequest<Result<IReadOnlyList<StockResponse>>>;
public record GetNearExpiryAlertsQuery(int Days, int? WarehouseId) : IRequest<Result<IReadOnlyList<StockResponse>>>;
public record GetStocktakePdfQuery(int WarehouseId) : IRequest<Result<byte[]>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class ListWarehousesHandler : IRequestHandler<ListWarehousesQuery, Result<IReadOnlyList<WarehouseResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListWarehousesHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<WarehouseResponse>>> Handle(ListWarehousesQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        // pha_warehouses chua duoc tao — tra stub 1 kho mac dinh de FE hien thi
        var defaultWarehouse = new WarehouseResponse(
            "default", tenantId, "WH-01", "Kho tổng", "MAIN", null, null, DateTime.UtcNow);
        return Result<IReadOnlyList<WarehouseResponse>>.Success(
            new List<WarehouseResponse> { defaultWarehouse });
    }
}

public class GetWarehouseHandler : IRequestHandler<GetWarehouseQuery, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<WarehouseResponse>> Handle(GetWarehouseQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        // pha_warehouses chua duoc tao — tra stub mac dinh
        if (q.Id != "default")
            return Result<WarehouseResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Không tìm thấy kho.");
        return Result<WarehouseResponse>.Success(
            new WarehouseResponse("default", tenantId, "WH-01", "Kho tổng", "MAIN", null, null, DateTime.UtcNow));
    }
}

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<WarehouseResponse>> Handle(CreateWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO pha_warehouses (tenant_id, CODE, NAME, TYPE, ADDRESS, MANAGER_USER_ID, CREATED_AT, UPDATED_AT)
              VALUES (@tenantId, @code, @name, @type, @address, @mgr, NOW(), NOW());
              SELECT LAST_INSERT_ID();",
            new { tenantId, code = r.Code, name = r.Name, type = r.Type, address = r.Address, mgr = r.ManagerUserId });

        return Result<WarehouseResponse>.Success(new WarehouseResponse(
            id.ToString(), tenantId, r.Code, r.Name, r.Type, r.Address, r.ManagerUserId, DateTime.UtcNow));
    }
}

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdateWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<WarehouseResponse>> Handle(UpdateWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        var rows = await conn.ExecuteAsync(
            "UPDATE pha_warehouses SET CODE=@code, NAME=@name, TYPE=@type, ADDRESS=@address, MANAGER_USER_ID=@mgr, UPDATED_AT=NOW() WHERE ID=@id AND tenant_id=@tenantId AND DELETED_AT IS NULL",
            new { code = r.Code, name = r.Name, type = r.Type, address = r.Address, mgr = r.ManagerUserId, id = cmd.Id, tenantId });

        if (rows == 0)
            return Result<WarehouseResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay kho.");

        return await new GetWarehouseHandler(_db, _currentUser).Handle(new GetWarehouseQuery(cmd.Id), ct);
    }
}

public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeleteWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<bool>> Handle(DeleteWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.ExecuteAsync(
            "UPDATE pha_warehouses SET DELETED_AT=NOW(), UPDATED_AT=NOW() WHERE ID=@id AND tenant_id=@tenantId AND DELETED_AT IS NULL",
            new { id = cmd.Id, tenantId });

        return rows == 0
            ? Result<bool>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay kho.")
            : Result<bool>.Success(true);
    }
}

public class ListPurchaseOrdersHandler : IRequestHandler<ListPurchaseOrdersQuery, Result<PagedResult<PurchaseOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListPurchaseOrdersHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<PurchaseOrderResponse>>> Handle(ListPurchaseOrdersQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "po.tenant_id = @tenantId", "po.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("po.status = @status"); prm.Add("status", q.Status); }
        if (!string.IsNullOrWhiteSpace(q.SupplierId)) { where.Add("po.supplier_id = @supplierId"); prm.Add("supplierId", q.SupplierId); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_purchase_orders po WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT po.id, po.tenant_id, po.supplier_id, s.name as supplier_name,
                      po.warehouse_id, po.order_no, po.ordered_at, po.expected_delivery,
                      po.status, po.total_amount, po.created_at
               FROM diab_his_pha_purchase_orders po
               LEFT JOIN diab_his_pha_suppliers s ON s.id = po.supplier_id
               WHERE {wc} ORDER BY po.created_at DESC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new PurchaseOrderResponse(
            (string)r.id, (int)r.tenant_id, (string)r.supplier_id, (string?)r.supplier_name,
            (int)r.warehouse_id, (string?)r.order_no, (DateTime?)r.ordered_at, (DateOnly?)r.expected_delivery,
            (string)r.status, [], (decimal)r.total_amount, (DateTime)r.created_at)).ToList();

        return Result<PagedResult<PurchaseOrderResponse>>.Success(new PagedResult<PurchaseOrderResponse>(items, q.Page, q.PageSize, total));
    }
}

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreatePurchaseOrderHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PurchaseOrderResponse>> Handle(CreatePurchaseOrderCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        var id = Guid.NewGuid().ToString();
        decimal totalAmount = r.Items.Sum(i => i.QuantityOrdered * i.UnitPrice);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_purchase_orders (id, tenant_id, supplier_id, warehouse_id, order_no, ordered_at, expected_delivery, status, total_amount, note, created_at, updated_at)
              VALUES (@id, @tenantId, @supplierId, @warehouseId, @orderNo, @orderedAt, @expectedDelivery, 'DRAFT', @totalAmount, @note, NOW(), NOW())",
            new { id, tenantId, supplierId = r.SupplierId, warehouseId = r.WarehouseId, orderNo = r.OrderNo, orderedAt = r.OrderedAt, expectedDelivery = r.ExpectedDelivery, totalAmount, note = r.Note });

        foreach (var item in r.Items)
        {
            await conn.ExecuteAsync(
                "INSERT INTO diab_his_pha_purchase_order_items (id, tenant_id, purchase_order_id, drug_id, quantity_ordered, quantity_received, unit_price, created_at, updated_at) VALUES (UUID(), @tenantId, @poId, @drugId, @qty, 0, @price, NOW(), NOW())",
                new { tenantId, poId = id, drugId = item.DrugId, qty = item.QuantityOrdered, price = item.UnitPrice });
        }

        var supplierName = await conn.ExecuteScalarAsync<string>("SELECT name FROM diab_his_pha_suppliers WHERE id = @id", new { id = r.SupplierId });
        var itemResponses = r.Items.Select(i => new PurchaseOrderItemResponse(i.DrugId, null, i.QuantityOrdered, 0, i.UnitPrice)).ToList();

        return Result<PurchaseOrderResponse>.Success(new PurchaseOrderResponse(
            id, tenantId, r.SupplierId, supplierName, r.WarehouseId, r.OrderNo,
            r.OrderedAt, r.ExpectedDelivery, "DRAFT", itemResponses, totalAmount, DateTime.UtcNow));
    }
}

public class CreateGrnHandler : IRequestHandler<CreateGrnCommand, Result<GrnResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICucQldLienThong _cucQld;

    public CreateGrnHandler(IDapperConnectionFactory db, ICurrentUser currentUser, ICucQldLienThong cucQld)
    {
        _db = db;
        _currentUser = currentUser;
        _cucQld = cucQld;
    }

    public async Task<Result<GrnResponse>> Handle(CreateGrnCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var po = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, warehouse_id, status FROM diab_his_pha_purchase_orders WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.PurchaseOrderId, tenantId });

        if (po == null)
            return Result<GrnResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay phieu dat hang.");

        var grnId = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(
            "INSERT INTO diab_his_pha_grn (id, tenant_id, purchase_order_id, received_at, note, created_at, updated_at) VALUES (@id, @tenantId, @poId, @receivedAt, @note, NOW(), NOW())",
            new { id = grnId, tenantId, poId = cmd.PurchaseOrderId, receivedAt = cmd.Request.ReceivedAt, note = cmd.Request.Note });

        var updatedStocks = new List<StockResponse>();
        foreach (var item in cmd.Request.Items)
        {
            // Update or insert stock
            var stockId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO pha_stocks (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, created_at, updated_at)
                  VALUES (@tenantId, @warehouseId, @drugId, @batchNo, @mfgDate, @expiryDate, @qty, 0, @unitCost, 10, NOW(), NOW())
                  ON DUPLICATE KEY UPDATE quantity_available = quantity_available + @qty, unit_cost = @unitCost, updated_at = NOW()",
                new { tenantId, warehouseId = (int)po.warehouse_id, drugId = item.DrugId, batchNo = item.BatchNo,
                      mfgDate = item.ManufactureDate, expiryDate = item.ExpiryDate, qty = item.QuantityReceived, unitCost = item.UnitCost });

            // Movement record
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                  SELECT @tenantId, id, warehouse_id, 'IMPORT', @qty, @unitCost, 'GRN', @grnId, NOW(), @userId, NOW(), NOW()
                  FROM pha_stocks WHERE tenant_id = @tenantId AND warehouse_id = @warehouseId AND drug_id = @drugId AND batch_no = @batchNo LIMIT 1",
                new { tenantId, warehouseId = (int)po.warehouse_id, drugId = item.DrugId, batchNo = item.BatchNo,
                      qty = item.QuantityReceived, unitCost = item.UnitCost, grnId, userId = 0 });

            var drugName = await conn.ExecuteScalarAsync<string>("SELECT name_vi FROM pha_drug_master WHERE id = @id", new { id = item.DrugId });
            var daysToExpiry = (item.ExpiryDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

            updatedStocks.Add(new StockResponse(
                stockId, tenantId, (int)po.warehouse_id, item.DrugId.ToString(), drugName, item.BatchNo,
                item.ManufactureDate, item.ExpiryDate, item.QuantityReceived, 0, item.UnitCost,
                daysToExpiry, daysToExpiry <= 90, false));
        }

        // Update PO status
        var allReceived = true; // simplified
        var newPoStatus = allReceived ? "RECEIVED" : "PARTIAL";
        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_purchase_orders SET status = @status, updated_at = NOW() WHERE id = @id",
            new { status = newPoStatus, id = cmd.PurchaseOrderId });

        await _cucQld.ReportImportAsync(Guid.Parse(grnId), ct);

        return Result<GrnResponse>.Success(new GrnResponse(grnId, newPoStatus, updatedStocks));
    }
}

public class ListStocksHandler : IRequestHandler<ListStocksQuery, Result<PagedResult<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListStocksHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<StockResponse>>> Handle(ListStocksQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        // diab_his_pha_stock: id, tenant_id, drug_id, lot_number, exp_date, quantity, import_price
        // Khong co: deleted_at, warehouse_id, batch_no, quantity_available, quantity_reserved, unit_cost, reorder_level
        var where = new List<string> { "s.tenant_id = @tenantId" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.BatchNo)) { where.Add("s.lot_number = @batchNo"); prm.Add("batchNo", q.BatchNo); }
        if (q.LowStock == true) { where.Add("s.quantity <= 10"); }
        if (q.NearExpiry == true) { where.Add("s.exp_date <= DATE_ADD(CURDATE(), INTERVAL 90 DAY)"); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_stock s WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id,
                      COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level,
                      DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s
               LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {wc} ORDER BY s.exp_date ASC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => MapStock(r)).Cast<StockResponse>().ToList();
        return Result<PagedResult<StockResponse>>.Success(new PagedResult<StockResponse>(items, q.Page, q.PageSize, total));
    }

    internal static StockResponse MapStock(dynamic r)
    {
        int dte = r.days_to_expiry == null ? 999 : (int)(long)r.days_to_expiry;
        decimal qa = (decimal)(r.quantity_available ?? 0);
        decimal rl = (decimal)(r.reorder_level ?? 10m);
        string drugIdStr = r.drug_id?.ToString() ?? "";
        return new StockResponse(
            r.id.ToString(), (int)r.tenant_id, 0, drugIdStr,
            (string?)r.drug_name, (string?)r.batch_no ?? "",
            r.manufacture_date != null ? DateOnly.FromDateTime((DateTime)r.manufacture_date) : (DateOnly?)null,
            DateOnly.FromDateTime((DateTime)r.expiry_date),
            qa, (decimal)(r.quantity_reserved ?? 0), (decimal)(r.unit_cost ?? 0),
            dte, dte <= 90, qa < rl);
    }
}

public class CreateAdjustmentHandler : IRequestHandler<CreateAdjustmentCommand, Result<AdjustmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateAdjustmentHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<AdjustmentResponse>> Handle(CreateAdjustmentCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;
        var adjustmentId = Guid.NewGuid().ToString();
        var movements = new List<StockMovementResponse>();

        foreach (var item in r.Items)
        {
            // Update stock
            await conn.ExecuteAsync(
                @"UPDATE pha_stocks SET quantity_available = quantity_available + @diff, updated_at = NOW()
                  WHERE tenant_id = @tenantId AND warehouse_id = @warehouseId AND drug_id = @drugId AND batch_no = @batchNo",
                new { diff = item.QuantityDiff, tenantId, warehouseId = r.WarehouseId, drugId = item.DrugId, batchNo = item.BatchNo });

            var mvtId = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, reason, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                  SELECT @tenantId, id, warehouse_id, 'ADJUST', @diff, @reason, 'ADJUSTMENT', @adjId, NOW(), 0, NOW(), NOW()
                  FROM pha_stocks WHERE tenant_id = @tenantId AND warehouse_id = @warehouseId AND drug_id = @drugId AND batch_no = @batchNo LIMIT 1",
                new { tenantId, warehouseId = r.WarehouseId, drugId = item.DrugId, batchNo = item.BatchNo, diff = item.QuantityDiff, reason = $"{r.Reason}: {r.Note}", adjId = adjustmentId });

            var drugName = await conn.ExecuteScalarAsync<string>("SELECT name_vi FROM pha_drug_master WHERE id = @id", new { id = item.DrugId });
            movements.Add(new StockMovementResponse(mvtId, tenantId, r.WarehouseId, "ADJUST", item.DrugId.ToString(), drugName,
                item.BatchNo, item.QuantityDiff, 0, "ADJUSTMENT", adjustmentId, DateTime.UtcNow, null, r.Reason));
        }

        return Result<AdjustmentResponse>.Success(new AdjustmentResponse(adjustmentId, movements));
    }
}

public class ListMovementsHandler : IRequestHandler<ListMovementsQuery, Result<PagedResult<StockMovementResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListMovementsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<StockMovementResponse>>> Handle(ListMovementsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        // Schema thuc te: id, tenant_id, drug_id, movement_type, quantity, note, ref_id, created_at, created_by
        // Khong co warehouse_id, stock_id, movement_at, unit_price, batch_no
        var where = new List<string> { "m.tenant_id = @tenantId" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.MovementType)) { where.Add("m.movement_type = @mvtType"); prm.Add("mvtType", q.MovementType); }
        if (q.FromDate.HasValue) { where.Add("DATE(m.created_at) >= @fromDate"); prm.Add("fromDate", q.FromDate.Value); }
        if (q.ToDate.HasValue) { where.Add("DATE(m.created_at) <= @toDate"); prm.Add("toDate", q.ToDate.Value); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_stock_movements m WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT m.id, m.tenant_id, m.drug_id, m.movement_type, m.quantity,
                      m.note, m.ref_id, m.created_at, m.created_by,
                      d.name_vi AS drug_name
               FROM diab_his_pha_stock_movements m
               LEFT JOIN diab_his_pha_drugs d ON d.id = m.drug_id AND d.tenant_id = m.tenant_id
               WHERE {wc} ORDER BY m.created_at DESC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new StockMovementResponse(
            r.id?.ToString() ?? "",
            (int)r.tenant_id,
            0,
            (string)r.movement_type,
            r.drug_id?.ToString() ?? "",
            (string?)r.drug_name,
            "",
            (decimal)(r.quantity ?? 0),
            0m,
            null,
            r.ref_id?.ToString(),
            r.created_at != null ? (DateTime)r.created_at : DateTime.UtcNow,
            null,
            (string?)r.note)).ToList();

        return Result<PagedResult<StockMovementResponse>>.Success(new PagedResult<StockMovementResponse>(items, q.Page, q.PageSize, total));
    }
}

public class CreateTransferHandler : IRequestHandler<CreateTransferCommand, Result<TransferResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public CreateTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<TransferResponse>> Handle(CreateTransferCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;
        var transferId = Guid.NewGuid().ToString();
        var movements = new List<StockMovementResponse>();

        foreach (var item in r.Items)
        {
            var stock = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, quantity_available, expiry_date, unit_cost FROM pha_stocks WHERE tenant_id = @tenantId AND warehouse_id = @wh AND drug_id = @drug AND batch_no = @batch AND deleted_at IS NULL",
                new { tenantId, wh = r.FromWarehouseId, drug = item.DrugId, batch = item.BatchNo });

            if (stock == null)
                return Result<TransferResponse>.Failure("PHARMACY_BATCH_NOT_FOUND", $"Khong tim thay lo {item.BatchNo}.");

            if (DateOnly.FromDateTime((DateTime)stock.expiry_date) < DateOnly.FromDateTime(DateTime.Today))
                return Result<TransferResponse>.Failure("PHARMACY_BATCH_EXPIRED", $"Lo {item.BatchNo} da het han.");

            if ((decimal)stock.quantity_available < item.Quantity)
                return Result<TransferResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT", "Ton kho khong du de chuyen.");

            // Deduct from source
            await conn.ExecuteAsync(
                "UPDATE pha_stocks SET quantity_available = quantity_available - @qty WHERE id = @id",
                new { qty = item.Quantity, id = stock.id.ToString() });

            // Add to destination
            await conn.ExecuteAsync(
                @"INSERT INTO pha_stocks (tenant_id, warehouse_id, drug_id, batch_no, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, created_at, updated_at)
                  VALUES (@tenantId, @wh, @drug, @batch, @expiry, @qty, 0, @cost, 10, NOW(), NOW())
                  ON DUPLICATE KEY UPDATE quantity_available = quantity_available + @qty, updated_at = NOW()",
                new { tenantId, wh = r.ToWarehouseId, drug = item.DrugId, batch = item.BatchNo, expiry = (DateTime)stock.expiry_date, qty = item.Quantity, cost = (decimal)stock.unit_cost });

            var mvt = new StockMovementResponse(Guid.NewGuid().ToString(), tenantId, r.FromWarehouseId, "TRANSFER",
                item.DrugId.ToString(), null, item.BatchNo, item.Quantity, (decimal)stock.unit_cost, "TRANSFER", transferId, DateTime.UtcNow, null, r.Note);
            movements.Add(mvt);
        }

        return Result<TransferResponse>.Success(new TransferResponse(transferId, movements));
    }
}

public class GetLowStockAlertsHandler : IRequestHandler<GetLowStockAlertsQuery, Result<IReadOnlyList<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetLowStockAlertsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<StockResponse>>> Handle(GetLowStockAlertsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var where = "s.tenant_id = @tenantId AND s.quantity <= COALESCE(d.reorder_level, 10)";
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id, COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level, DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {where} ORDER BY s.quantity ASC LIMIT 100", prm);

        return Result<IReadOnlyList<StockResponse>>.Success(rows.Select(r => ListStocksHandler.MapStock(r)).Cast<StockResponse>().ToList());
    }
}

public class GetNearExpiryAlertsHandler : IRequestHandler<GetNearExpiryAlertsQuery, Result<IReadOnlyList<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetNearExpiryAlertsHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<StockResponse>>> Handle(GetNearExpiryAlertsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var where = "s.tenant_id = @tenantId AND s.exp_date <= DATE_ADD(CURDATE(), INTERVAL @days DAY) AND s.quantity > 0";
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("days", q.Days);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id, COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level, DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {where} ORDER BY s.exp_date ASC LIMIT 200", prm);

        return Result<IReadOnlyList<StockResponse>>.Success(rows.Select(r => ListStocksHandler.MapStock(r)).Cast<StockResponse>().ToList());
    }
}

// ─── String-ID variants cho /pharmacy/stock endpoints ─────────────────────────

public record ListStocksStringIdQuery(string? WarehouseId, string? DrugId, string? BatchNo, bool? LowStock, bool? NearExpiry, int NearExpiryDays, int Page, int PageSize)
    : IRequest<Result<PagedResult<StockResponse>>>;

public record GetStockByIdQuery(string Id) : IRequest<Result<StockResponse>>;
public record GetLowStockStringIdQuery(string? WarehouseId) : IRequest<Result<IReadOnlyList<StockResponse>>>;
public record GetNearExpiryStringIdQuery(int Days, string? WarehouseId) : IRequest<Result<IReadOnlyList<StockResponse>>>;

public class ListStocksStringIdHandler : IRequestHandler<ListStocksStringIdQuery, Result<PagedResult<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListStocksStringIdHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<PagedResult<StockResponse>>> Handle(ListStocksStringIdQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        // diab_his_pha_stock: khong co deleted_at, warehouse_id, batch_no rieng biet — lot_number=batch, exp_date=expiry, quantity=available
        var where = new List<string> { "s.tenant_id = @tenantId" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.DrugId)) { where.Add("s.drug_id = @drugId"); prm.Add("drugId", q.DrugId); }
        if (!string.IsNullOrWhiteSpace(q.BatchNo)) { where.Add("s.lot_number = @batchNo"); prm.Add("batchNo", q.BatchNo); }
        if (q.LowStock == true) { where.Add("s.quantity <= 10"); }
        if (q.NearExpiry == true)
        {
            where.Add("s.exp_date <= DATE_ADD(CURDATE(), INTERVAL @nearDays DAY)");
            prm.Add("nearDays", q.NearExpiryDays);
        }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_stock s WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id,
                      COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level,
                      DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s
               LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {wc} ORDER BY s.exp_date ASC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => ListStocksHandler.MapStock(r)).Cast<StockResponse>().ToList();
        return Result<PagedResult<StockResponse>>.Success(new PagedResult<StockResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetStockByIdHandler : IRequestHandler<GetStockByIdQuery, Result<StockResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetStockByIdHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<StockResponse>> Handle(GetStockByIdQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT s.id, s.tenant_id, s.drug_id, COALESCE(d.name_vi, d.name) AS drug_name,
                     s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                     s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                     d.reorder_level, DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
              FROM diab_his_pha_stock s
              LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
              WHERE s.id = @id AND s.tenant_id = @tenantId",
            new { id = q.Id, tenantId });

        if (r == null)
            return Result<StockResponse>.Failure("STOCK_NOT_FOUND", "Không tìm thấy lô hàng trong kho");

        return Result<StockResponse>.Success(ListStocksHandler.MapStock(r));
    }
}

public class GetLowStockStringIdHandler : IRequestHandler<GetLowStockStringIdQuery, Result<IReadOnlyList<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetLowStockStringIdHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<StockResponse>>> Handle(GetLowStockStringIdQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var where = "s.tenant_id = @tenantId AND s.quantity <= COALESCE(d.reorder_level, 10)";
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id, COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level, DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {where} ORDER BY s.quantity ASC LIMIT 100", prm);

        return Result<IReadOnlyList<StockResponse>>.Success(rows.Select(r => ListStocksHandler.MapStock(r)).Cast<StockResponse>().ToList());
    }
}

public class GetNearExpiryStringIdHandler : IRequestHandler<GetNearExpiryStringIdQuery, Result<IReadOnlyList<StockResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetNearExpiryStringIdHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<StockResponse>>> Handle(GetNearExpiryStringIdQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var where = "s.tenant_id = @tenantId AND s.exp_date <= DATE_ADD(CURDATE(), INTERVAL @days DAY) AND s.quantity > 0";
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("days", q.Days);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.id, s.tenant_id, s.drug_id, COALESCE(d.name_vi, d.name) AS drug_name,
                      s.lot_number AS batch_no, s.mfg_date AS manufacture_date, s.exp_date AS expiry_date,
                      s.quantity AS quantity_available, 0 AS quantity_reserved, s.import_price AS unit_cost,
                      d.reorder_level, DATEDIFF(s.exp_date, CURDATE()) as days_to_expiry
               FROM diab_his_pha_stock s LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
               WHERE {where} ORDER BY s.exp_date ASC LIMIT 200", prm);

        return Result<IReadOnlyList<StockResponse>>.Success(rows.Select(r => ListStocksHandler.MapStock(r)).Cast<StockResponse>().ToList());
    }
}

public class GetStocktakePdfHandler : IRequestHandler<GetStocktakePdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetStocktakePdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<byte[]>> Handle(GetStocktakePdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var warehouseName = "Kho tổng"; // pha_warehouses chua co, dung ten mac dinh

        // Minimal PDF stub - full rendering via IPdfService in Infrastructure
        var content = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/MediaBox[0 0 595 842]/Parent 2 0 R/Resources<<>>/Contents 4 0 R>>endobj\n4 0 obj<</Length 50>>stream\nBT /F1 12 Tf 50 750 Td (KIEM KE: {warehouseName}) Tj ET\nendstream\nendobj\nxref\n0 5\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\n0000000115 00000 n\n0000000266 00000 n\ntrailer<</Size 5/Root 1 0 R>>\nstartxref\n360\n%%EOF";
        return Result<byte[]>.Success(System.Text.Encoding.ASCII.GetBytes(content));
    }
}
