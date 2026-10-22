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
public record CreateGrnCommand(string? PurchaseOrderId, GoodsReceivedRequest Request) : IRequest<Result<GrnResponse>>;

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
    private readonly IBranchProvider _branch;

    public ListWarehousesHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<IReadOnlyList<WarehouseResponse>>> Handle(ListWarehousesQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT id, tenant_id, code, name, type, address, manager_user_id, created_at FROM pha_warehouses WHERE tenant_id = @tenantId AND deleted_at IS NULL AND {BranchSql.Condition("")} ORDER BY code",
            new { tenantId, branchId = _branch.BranchId, ignoreBranch = _branch.IgnoreBranchFilter });
        var items = rows.Select(r => new WarehouseResponse(
            r.id.ToString(), (int)r.tenant_id, (string)r.code, (string)r.name,
            (string?)r.type, (string?)r.address, (int?)r.manager_user_id, (DateTime)r.created_at)).ToList();
        return Result<IReadOnlyList<WarehouseResponse>>.Success(items);
    }
}

public class GetWarehouseHandler : IRequestHandler<GetWarehouseQuery, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public GetWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<WarehouseResponse>> Handle(GetWarehouseQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $"SELECT id, tenant_id, code, name, type, address, manager_user_id, created_at FROM pha_warehouses WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL AND {BranchSql.Condition("")}",
            new { id = q.Id, tenantId, branchId = _branch.BranchId, ignoreBranch = _branch.IgnoreBranchFilter });
        if (r == null)
            return Result<WarehouseResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Không tìm thấy kho.");
        return Result<WarehouseResponse>.Success(new WarehouseResponse(
            r.id.ToString(), (int)r.tenant_id, (string)r.code, (string)r.name,
            (string?)r.type, (string?)r.address, (int?)r.manager_user_id, (DateTime)r.created_at));
    }
}

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public CreateWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<WarehouseResponse>> Handle(CreateWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;
        // branch_id luon gan tu IBranchProvider, khong trust input client
        var branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null;

        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO pha_warehouses (tenant_id, branch_id, CODE, NAME, TYPE, ADDRESS, MANAGER_USER_ID, CREATED_AT, UPDATED_AT)
              VALUES (@tenantId, @branchId, @code, @name, @type, @address, @mgr, NOW(), NOW());
              SELECT LAST_INSERT_ID();",
            new { tenantId, branchId, code = r.Code, name = r.Name, type = r.Type, address = r.Address, mgr = r.ManagerUserId });

        return Result<WarehouseResponse>.Success(new WarehouseResponse(
            id.ToString(), tenantId, r.Code, r.Name, r.Type, r.Address, r.ManagerUserId, DateTime.UtcNow));
    }
}

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, Result<WarehouseResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public UpdateWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<WarehouseResponse>> Handle(UpdateWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;

        var rows = await conn.ExecuteAsync(
            $"UPDATE pha_warehouses SET CODE=@code, NAME=@name, TYPE=@type, ADDRESS=@address, MANAGER_USER_ID=@mgr, UPDATED_AT=NOW() WHERE ID=@id AND tenant_id=@tenantId AND DELETED_AT IS NULL AND {BranchSql.Condition("")}",
            new { code = r.Code, name = r.Name, type = r.Type, address = r.Address, mgr = r.ManagerUserId, id = cmd.Id, tenantId, branchId = _branch.BranchId, ignoreBranch = _branch.IgnoreBranchFilter });

        if (rows == 0)
            return Result<WarehouseResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay kho.");

        return await new GetWarehouseHandler(_db, _currentUser, _branch).Handle(new GetWarehouseQuery(cmd.Id), ct);
    }
}

public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public DeleteWarehouseHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<bool>> Handle(DeleteWarehouseCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.ExecuteAsync(
            $"UPDATE pha_warehouses SET DELETED_AT=NOW(), UPDATED_AT=NOW() WHERE ID=@id AND tenant_id=@tenantId AND DELETED_AT IS NULL AND {BranchSql.Condition("")}",
            new { id = cmd.Id, tenantId, branchId = _branch.BranchId, ignoreBranch = _branch.IgnoreBranchFilter });

        return rows == 0
            ? Result<bool>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay kho.")
            : Result<bool>.Success(true);
    }
}

public class ListPurchaseOrdersHandler : IRequestHandler<ListPurchaseOrdersQuery, Result<PagedResult<PurchaseOrderResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public ListPurchaseOrdersHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<PagedResult<PurchaseOrderResponse>>> Handle(ListPurchaseOrdersQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "po.tenant_id = @tenantId", "po.deleted_at IS NULL", BranchSql.Condition("po") };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);
        prm.Add("branchId", _branch.BranchId); prm.Add("ignoreBranch", _branch.IgnoreBranchFilter);

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
            (int)r.warehouse_id, (string?)r.order_no, (DateTime?)r.ordered_at,
            r.expected_delivery is DateTime ed ? DateOnly.FromDateTime(ed) : (DateOnly?)null,
            (string)r.status, [], (decimal)r.total_amount, (DateTime)r.created_at)).ToList();

        return Result<PagedResult<PurchaseOrderResponse>>.Success(new PagedResult<PurchaseOrderResponse>(items, q.Page, q.PageSize, total));
    }
}

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public CreatePurchaseOrderHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<PurchaseOrderResponse>> Handle(CreatePurchaseOrderCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var r = cmd.Request;
        var branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null;

        var id = Guid.NewGuid().ToString();
        decimal totalAmount = r.Items.Sum(i => i.QuantityOrdered * i.UnitPrice);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_purchase_orders (id, tenant_id, branch_id, supplier_id, warehouse_id, order_no, ordered_at, expected_delivery, status, total_amount, note, created_at, updated_at)
              VALUES (@id, @tenantId, @branchId, @supplierId, @warehouseId, @orderNo, @orderedAt, @expectedDelivery, 'DRAFT', @totalAmount, @note, NOW(), NOW())",
            new { id, tenantId, branchId, supplierId = r.SupplierId, warehouseId = r.WarehouseId, orderNo = r.OrderNo, orderedAt = r.OrderedAt, expectedDelivery = r.ExpectedDelivery, totalAmount, note = r.Note });

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

    private readonly IBranchProvider _branch;

    public CreateGrnHandler(IDapperConnectionFactory db, ICurrentUser currentUser, ICucQldLienThong cucQld, IBranchProvider branch)
    {
        _branch = branch;
        _db = db;
        _currentUser = currentUser;
        _cucQld = cucQld;
    }

    public async Task<Result<GrnResponse>> Handle(CreateGrnCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var hasPo = !string.IsNullOrWhiteSpace(cmd.PurchaseOrderId);
        int warehouseId;
        if (hasPo)
        {
            var po = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, warehouse_id, status FROM diab_his_pha_purchase_orders WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { id = cmd.PurchaseOrderId, tenantId });

            if (po == null)
                return Result<GrnResponse>.Failure("PHARMACY_WAREHOUSE_NOT_FOUND", "Khong tim thay phieu dat hang.");
            warehouseId = (int)po.warehouse_id;
        }
        else
        {
            if (!int.TryParse(cmd.Request.WarehouseId, out warehouseId) || warehouseId <= 0)
                return Result<GrnResponse>.Failure("VALIDATION_ERROR", "Cần chọn kho khi không có đơn đặt hàng");
            if (cmd.Request.Items == null || cmd.Request.Items.Count == 0)
                return Result<GrnResponse>.Failure("VALIDATION_ERROR", "Cần ít nhất một dòng thuốc nhập kho");
        }

        // Kiem tra thuoc ton tai (GUID, bang active diab_his_pha_drugs) truoc khi ghi.
        var drugNames = new Dictionary<string, string?>();
        foreach (var item in cmd.Request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.DrugId) || string.IsNullOrWhiteSpace(item.BatchNo))
                return Result<GrnResponse>.Failure("VALIDATION_ERROR", "Mỗi dòng nhập kho cần có thuốc và số lô");
            if (item.QuantityReceived <= 0)
                return Result<GrnResponse>.Failure("VALIDATION_ERROR", "Số lượng nhập phải lớn hơn 0");
            if (drugNames.ContainsKey(item.DrugId)) continue;
            var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT COALESCE(NULLIF(name_vi, ''), name) AS name FROM diab_his_pha_drugs WHERE id = @id AND tenant_id = @tenantId",
                new { id = item.DrugId, tenantId });
            if (d == null)
                return Result<GrnResponse>.Failure("PHARMACY_DRUG_NOT_FOUND", "Không tìm thấy thuốc trong danh mục");
            drugNames[item.DrugId] = (string?)d.name;
        }

        var grnId = Guid.NewGuid().ToString();
        var userId = _currentUser.UserId?.ToString();
        var branchId = _branch.BranchId > 0 ? _branch.BranchId : (int?)null;
        var updatedStocks = new List<StockResponse>();

        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(
                "INSERT INTO diab_his_pha_grn (id, tenant_id, purchase_order_id, received_at, note, created_at, updated_at) VALUES (@id, @tenantId, @poId, @receivedAt, @note, NOW(), NOW())",
                new { id = grnId, tenantId, poId = hasPo ? cmd.PurchaseOrderId : null, receivedAt = cmd.Request.ReceivedAt == default ? DateTime.UtcNow : cmd.Request.ReceivedAt, note = cmd.Request.Note }, tx);

            foreach (var item in cmd.Request.Items)
            {
                var qty = (int)Math.Round(item.QuantityReceived);
                // Cong don vao lo da co (tenant, thuoc, so lo); chua co thi tao lo moi.
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND drug_id = @drugId AND lot_number = @batchNo LIMIT 1 FOR UPDATE",
                    new { tenantId, drugId = item.DrugId, batchNo = item.BatchNo }, tx);
                string stockId;
                if (existing != null)
                {
                    stockId = existing.id.ToString();
                    await conn.ExecuteAsync(
                        "UPDATE diab_his_pha_stock SET quantity = quantity + @qty, import_price = @unitCost, exp_date = @expiryDate, updated_at = NOW() WHERE id = @id",
                        new { qty, unitCost = item.UnitCost, expiryDate = item.ExpiryDate.ToDateTime(TimeOnly.MinValue), id = stockId }, tx);
                }
                else
                {
                    stockId = Guid.NewGuid().ToString();
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pha_stock (id, tenant_id, branch_id, drug_id, lot_number, mfg_date, exp_date, quantity, import_price, created_at, updated_at)
                          VALUES (@id, @tenantId, @branchId, @drugId, @batchNo, @mfgDate, @expiryDate, @qty, @unitCost, NOW(), NOW())",
                        new { id = stockId, tenantId, branchId, drugId = item.DrugId, batchNo = item.BatchNo,
                              mfgDate = item.ManufactureDate?.ToDateTime(TimeOnly.MinValue), expiryDate = item.ExpiryDate.ToDateTime(TimeOnly.MinValue),
                              qty, unitCost = item.UnitCost }, tx);
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                      VALUES (@tenantId, @stockId, @wh, 'IMPORT', @qty, @unitCost, 'GRN', @grnId, NOW(), @userId, NOW(), NOW())",
                    new { tenantId, stockId, wh = warehouseId.ToString(), qty, unitCost = item.UnitCost, grnId, userId }, tx);

                var cur = await conn.ExecuteScalarAsync<decimal>("SELECT quantity FROM diab_his_pha_stock WHERE id = @id", new { id = stockId }, tx);
                var daysToExpiry = (item.ExpiryDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
                updatedStocks.Add(new StockResponse(
                    stockId, tenantId, warehouseId, item.DrugId, drugNames[item.DrugId], item.BatchNo,
                    item.ManufactureDate, item.ExpiryDate, cur, 0, item.UnitCost,
                    daysToExpiry, daysToExpiry >= 0 && daysToExpiry <= 30, false, daysToExpiry < 0));
            }

            if (hasPo)
            {
                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_purchase_orders SET status = 'RECEIVED', updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
                    new { id = cmd.PurchaseOrderId, tenantId }, tx);
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
        var newPoStatus = hasPo ? "RECEIVED" : "NONE";

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
                      d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
        // BM-59: phan biet "Het han" (dte < 0) va "Sap het han" (0 <= dte <= 30).
        // Truoc day: dte <= 90 -> ca lo het han lan sap het han deu IsNearExpiry=true
        // -> FE hien thi "Sap het han" cho lo da het han.
        bool isExpired   = dte < 0;
        bool isNearExpiry = !isExpired && dte <= 30;
        return new StockResponse(
            r.id.ToString(), (int)r.tenant_id, 0, drugIdStr,
            (string?)r.drug_name, (string?)r.batch_no ?? "",
            r.manufacture_date != null ? DateOnly.FromDateTime((DateTime)r.manufacture_date) : (DateOnly?)null,
            DateOnly.FromDateTime((DateTime)r.expiry_date),
            qa, (decimal)(r.quantity_reserved ?? 0), (decimal)(r.unit_cost ?? 0),
            dte, isNearExpiry, qa < rl, isExpired, (string?)r.drug_code);
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

        var userId = _currentUser.UserId?.ToString();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var item in r.Items)
            {
                var stock = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT s.id, s.quantity, s.import_price, COALESCE(NULLIF(d.name_vi, ''), d.name) AS drug_name
                      FROM diab_his_pha_stock s LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id
                      WHERE s.tenant_id = @tenantId AND s.drug_id = @drugId AND s.lot_number = @batchNo LIMIT 1 FOR UPDATE",
                    new { tenantId, drugId = item.DrugId, batchNo = item.BatchNo }, tx);
                if (stock == null)
                {
                    tx.Rollback();
                    return Result<AdjustmentResponse>.Failure("PHARMACY_BATCH_NOT_FOUND", $"Không tìm thấy lô {item.BatchNo} của thuốc trong kho");
                }
                var diff = (int)Math.Round(item.QuantityDiff);
                if ((decimal)stock.quantity + diff < 0)
                {
                    tx.Rollback();
                    return Result<AdjustmentResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT", $"Tồn kho lô {item.BatchNo} không đủ để điều chỉnh giảm");
                }

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_stock SET quantity = quantity + @diff, updated_at = NOW() WHERE id = @id",
                    new { diff, id = stock.id.ToString() }, tx);

                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, reason, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                      VALUES (@tenantId, @stockId, @wh, 'ADJUST', @diff, @reason, 'ADJUSTMENT', @adjId, NOW(), @userId, NOW(), NOW())",
                    new { tenantId, stockId = stock.id.ToString(), wh = r.WarehouseId.ToString(), diff, reason = $"{r.Reason}: {r.Note}", adjId = adjustmentId, userId }, tx);

                movements.Add(new StockMovementResponse(Guid.NewGuid().ToString(), tenantId, r.WarehouseId, "ADJUST", item.DrugId, (string?)stock.drug_name,
                    item.BatchNo, item.QuantityDiff, 0, "ADJUSTMENT", adjustmentId, DateTime.UtcNow, null, r.Reason));
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        return Result<AdjustmentResponse>.Success(new AdjustmentResponse(adjustmentId, movements));
    }
}

public class ListMovementsHandler : IRequestHandler<ListMovementsQuery, Result<PagedResult<StockMovementResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branch;

    public ListMovementsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch) { _db = db; _currentUser = currentUser; _branch = branch; }

    public async Task<Result<PagedResult<StockMovementResponse>>> Handle(ListMovementsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        // Schema thuc te (mig 0013): id, tenant_id, stock_id, warehouse_id, movement_type, quantity,
        // unit_price, reason, reference_type, reference_id, movement_at, performed_by, audit cols.
        // drug_id lay qua join diab_his_pha_stock.
        var where = new List<string> { "m.tenant_id = @tenantId", BranchSql.Condition("m") };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId); prm.Add("offset", offset); prm.Add("limit", q.PageSize);
        prm.Add("branchId", _branch.BranchId); prm.Add("ignoreBranch", _branch.IgnoreBranchFilter);

        if (!string.IsNullOrWhiteSpace(q.MovementType)) { where.Add("m.movement_type = @mvtType"); prm.Add("mvtType", q.MovementType); }
        if (q.FromDate.HasValue) { where.Add("DATE(m.created_at) >= @fromDate"); prm.Add("fromDate", q.FromDate.Value); }
        if (q.ToDate.HasValue) { where.Add("DATE(m.created_at) <= @toDate"); prm.Add("toDate", q.ToDate.Value); }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_stock_movements m WHERE {wc}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT m.id, m.tenant_id, s.drug_id, m.movement_type, m.quantity,
                      m.reason AS note, m.reference_id AS ref_id, m.created_at, m.created_by,
                      COALESCE(NULLIF(d.name_vi, ''), d.name) AS drug_name
               FROM diab_his_pha_stock_movements m
               LEFT JOIN diab_his_pha_stock s ON s.id = m.stock_id AND s.tenant_id = m.tenant_id
               LEFT JOIN diab_his_pha_drugs d ON d.id = s.drug_id AND d.tenant_id = m.tenant_id
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

        // diab_his_pha_stock (active) khong co warehouse_id nen chuyen kho o day chi kiem tra lo + ton
        // va ghi movement TRANSFER; chuyen ton giua chi nhanh dung module StockTransfers.
        var userId = _currentUser.UserId?.ToString();
        foreach (var item in r.Items)
        {
            var stock = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, quantity, exp_date, import_price FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND drug_id = @drug AND lot_number = @batch LIMIT 1",
                new { tenantId, drug = item.DrugId, batch = item.BatchNo });

            if (stock == null)
                return Result<TransferResponse>.Failure("PHARMACY_BATCH_NOT_FOUND", $"Không tìm thấy lô {item.BatchNo}.");

            if (DateOnly.FromDateTime((DateTime)stock.exp_date) < DateOnly.FromDateTime(DateTime.Today))
                return Result<TransferResponse>.Failure("PHARMACY_BATCH_EXPIRED", $"Lô {item.BatchNo} đã hết hạn.");

            if ((decimal)stock.quantity < item.Quantity)
                return Result<TransferResponse>.Failure("PHARMACY_STOCK_INSUFFICIENT", "Tồn kho không đủ để chuyển.");

            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reason, reference_type, reference_id, movement_at, performed_by, created_at, updated_at)
                  VALUES (@tenantId, @stockId, @wh, 'TRANSFER', @qty, @cost, @reason, 'TRANSFER', @transferId, NOW(), @userId, NOW(), NOW())",
                new { tenantId, stockId = stock.id.ToString(), wh = r.FromWarehouseId.ToString(), qty = item.Quantity, cost = (decimal)stock.import_price, reason = r.Note, transferId, userId });

            movements.Add(new StockMovementResponse(Guid.NewGuid().ToString(), tenantId, r.FromWarehouseId, "TRANSFER",
                item.DrugId, null, item.BatchNo, item.Quantity, (decimal)stock.import_price, "TRANSFER", transferId, DateTime.UtcNow, null, r.Note));
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
            $@"SELECT s.id, s.tenant_id, s.drug_id, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
            $@"SELECT s.id, s.tenant_id, s.drug_id, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
                      d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
            @"SELECT s.id, s.tenant_id, s.drug_id, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
            $@"SELECT s.id, s.tenant_id, s.drug_id, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
            $@"SELECT s.id, s.tenant_id, s.drug_id, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name,
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
    private readonly IBranchProvider _branch;
    private readonly IStocktakePdfBuilder _builder;

    public GetStocktakePdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branch, IStocktakePdfBuilder builder)
    { _db = db; _currentUser = currentUser; _branch = branch; _builder = builder; }

    public async Task<Result<byte[]>> Handle(GetStocktakePdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var branchId = _branch.BranchId;
        var ignoreBranch = _branch.IgnoreBranchFilter;

        // Ghi chu: diab_his_pha_stocktakes chua co cot warehouse_id (chi co "location" dang text
        // tu do), nen KHONG the loc chinh xac theo q.WarehouseId. Lay phien kiem ke gan nhat
        // cua tenant + chi nhanh dang lam viec (theo stocktake_date DESC) — pha_warehouses.id chi
        // dung de tra ten kho hien thi.
        var warehouseName = await conn.ExecuteScalarAsync<string?>(
            $"SELECT name FROM pha_warehouses WHERE id = @id AND tenant_id = @tenantId AND {BranchSql.Condition("")}",
            new { id = q.WarehouseId, tenantId, branchId, ignoreBranch });

        var st = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $@"SELECT id, code, stocktake_date, location, note
              FROM diab_his_pha_stocktakes
              WHERE tenant_id = @tenantId AND deleted_at IS NULL AND {BranchSql.Condition("")}
              ORDER BY stocktake_date DESC, created_at DESC LIMIT 1",
            new { tenantId, branchId, ignoreBranch });

        if (st == null)
            return Result<byte[]>.Failure("STOCKTAKE_NOT_FOUND", "Không tìm thấy phiếu kiểm kê kho.");

        var itemRows = await conn.QueryAsync<dynamic>(
            @"SELECT it.lot_number, it.system_qty, it.counted_qty, it.difference,
                     d.code AS drug_code, d.code AS drug_code, COALESCE(d.name_vi, d.name) AS drug_name, d.unit AS unit
              FROM diab_his_pha_stocktake_items it
              LEFT JOIN diab_his_pha_drugs d ON d.id = it.drug_id AND d.tenant_id = it.tenant_id
              WHERE it.stocktake_id = @stId AND it.tenant_id = @tenantId AND it.deleted_at IS NULL
              ORDER BY drug_name, it.lot_number",
            new { stId = (string)st.id, tenantId });

        var items = itemRows.Select((r, idx) => new StocktakeItemRow(
            idx + 1,
            (string?)r.drug_code,
            (string?)r.drug_name ?? "—",
            (string?)r.unit,
            (string?)r.lot_number,
            (int)r.system_qty,
            (int)r.counted_qty,
            (int)r.difference)).ToList();

        var lh = await conn.QueryFirstOrDefaultAsync<Application.Reports.LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });
        lh ??= new Application.Reports.LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var location = !string.IsNullOrWhiteSpace(warehouseName) ? warehouseName : (string?)st.location;

        var data = new StocktakeData(
            lh, (string?)st.code, DateOnly.FromDateTime((DateTime)st.stocktake_date), location, (string?)st.note, items);

        var pdf = _builder.Build(data);
        return Result<byte[]>.Success(pdf);
    }
}
