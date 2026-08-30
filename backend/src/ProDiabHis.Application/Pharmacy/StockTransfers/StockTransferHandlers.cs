using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.StockTransfers;

/// <summary>
/// E/Dot3 (muc 4.2 BRD) - state machine dieu chuyen kho noi bo giua chi nhanh.
/// Dung Dapper truc tiep tren diab_his_pha_stock_transfers/_items (migration 9151) va
/// diab_his_pha_stock (ton kho theo lo) — theo dung pattern DispensingHandlers.cs hien co.
/// BR-54: chi cung tenant (moi query/insert deu WHERE tenant_id = @tenantId tu ITenantProvider).
/// BR-60: filter list KHONG dung IgnoreBranchFilter mac dinh cua BranchSql — dieu kien tuong minh
///        "from_branch_id = @b OR to_branch_id = @b" (user o mot trong hai chi nhanh deu xem duoc).
/// </summary>
public static class StockTransferErrors
{
    public const string NotFound = "STOCK_TRANSFER_NOT_FOUND";
    public const string InvalidState = "STOCK_TRANSFER_INVALID_STATE";
    public const string EmptyItems = "STOCK_TRANSFER_EMPTY_ITEMS";
    public const string SameBranch = "STOCK_TRANSFER_SAME_BRANCH";
    public const string SelfApproval = "SELF_APPROVAL_NOT_ALLOWED";
    public const string ApprovalPermissionRequired = "APPROVAL_PERMISSION_REQUIRED";
    public const string ExpiryGuard = "STOCK_TRANSFER_NEAR_EXPIRY_LOT";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string BranchAccessDenied = "BRANCH_ACCESS_DENIED";
}

file static class StockTransferSql
{
    public const string Header = @"
        SELECT id, tenant_id, transfer_no, from_branch_id, to_branch_id, status, total_value,
               requires_approval, reason, requested_by, requested_at, approved_by, approved_at,
               rejected_reason, shipped_by, shipped_at, received_by, received_at, created_at, created_by
        FROM diab_his_pha_stock_transfers
        WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL";

    public const string Items = @"
        SELECT ti.id, ti.transfer_id, ti.drug_id, d.name AS drug_name, ti.lot_no, ti.expiry_date,
               ti.qty_requested, ti.qty_shipped, ti.qty_received, ti.unit_cost, ti.note
        FROM diab_his_pha_stock_transfer_items ti
        LEFT JOIN diab_his_pha_drugs d ON d.id = ti.drug_id
        WHERE ti.transfer_id = @id";
}

file static class StockTransferMapper
{
    public static StockTransferItemResponse ToItemResponse(dynamic r) => new(
        (string)r.id, (string)r.drug_id, (string?)r.drug_name, (string?)r.lot_no,
        r.expiry_date == null ? null : DateOnly.FromDateTime((DateTime)r.expiry_date),
        (decimal)r.qty_requested, (decimal)r.qty_shipped, (decimal)r.qty_received,
        (decimal)r.unit_cost, (string?)r.note);

    public static async Task<StockTransferResponse> ToResponseAsync(IDbConnection conn, dynamic h, IEnumerable<dynamic> itemRows,
        int tenantId)
    {
        var fromBranchName = await conn.ExecuteScalarAsync<string?>(
            "SELECT name FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId",
            new { id = (int)h.from_branch_id, tenantId });
        var toBranchName = await conn.ExecuteScalarAsync<string?>(
            "SELECT name FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tenantId",
            new { id = (int)h.to_branch_id, tenantId });

        return new StockTransferResponse(
            (string)h.id, (int)h.tenant_id, (string)h.transfer_no,
            (int)h.from_branch_id, fromBranchName, (int)h.to_branch_id, toBranchName,
            (string)h.status, (decimal)h.total_value, Convert.ToBoolean(h.requires_approval),
            (string?)h.reason, (string?)h.requested_by, (DateTime?)h.requested_at,
            (string?)h.approved_by, (DateTime?)h.approved_at, (string?)h.rejected_reason,
            (string?)h.shipped_by, (DateTime?)h.shipped_at, (string?)h.received_by, (DateTime?)h.received_at,
            itemRows.Select(ToItemResponse).ToList(),
            (DateTime)h.created_at);
    }
}

// ─── Create ───────────────────────────────────────────────────────────────────
public class CreateStockTransferHandler : IRequestHandler<CreateStockTransferCommand, Result<StockTransferResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CreateStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<StockTransferResponse>> Handle(CreateStockTransferCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (req.Items == null || req.Items.Count == 0)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.EmptyItems, "Phiếu điều chuyển phải có ít nhất một dòng hàng");
        if (req.FromBranchId == req.ToBranchId)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.SameBranch, "Chi nhánh gửi và chi nhánh nhận không được trùng nhau");

        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId?.ToString();

        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        // BR-54: 2 chi nhanh phai cung tenant
        var validBranches = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_sys_branches WHERE tenant_id = @tenantId AND id IN (@fromId, @toId) AND deleted_at IS NULL",
            new { tenantId, fromId = req.FromBranchId, toId = req.ToBranchId });
        if (validBranches != 2)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.BranchAccessDenied, "Chi nhánh gửi/nhận không hợp lệ hoặc không thuộc tenant hiện tại");

        var id = Guid.NewGuid().ToString();
        var transferNo = "DC" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Random.Shared.Next(100, 999);
        decimal totalValue = req.Items.Sum(i => i.QtyRequested * i.UnitCost);

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_pha_stock_transfers
                    (id, tenant_id, transfer_no, from_branch_id, to_branch_id, status, total_value,
                     requires_approval, reason, created_by, created_at)
                VALUES
                    (@id, @tenantId, @transferNo, @fromBranchId, @toBranchId, @status, @totalValue,
                     1, @reason, @createdBy, UTC_TIMESTAMP())",
                new
                {
                    id, tenantId, transferNo, fromBranchId = req.FromBranchId, toBranchId = req.ToBranchId,
                    status = StockTransferStatus.Draft, totalValue, reason = req.Reason, createdBy = userId
                }, tx);

            foreach (var item in req.Items)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO diab_his_pha_stock_transfer_items
                        (id, transfer_id, tenant_id, drug_id, lot_no, expiry_date, qty_requested, unit_cost, note, created_at)
                    VALUES
                        (@itemId, @transferId, @tenantId, @drugId, @lotNo, @expiryDate, @qty, @unitCost, @note, UTC_TIMESTAMP())",
                    new
                    {
                        itemId = Guid.NewGuid().ToString(), transferId = id, tenantId,
                        drugId = item.DrugId, lotNo = item.LotNo,
                        expiryDate = item.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                        qty = item.QtyRequested, unitCost = item.UnitCost, note = item.Note
                    }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        await _audit.LogAsync(Domain.Entities.AuditAction.Create, "StockTransfer", id,
            new { transferNo, req.FromBranchId, req.ToBranchId, totalValue }, ct);

        return await LoadResponse(conn, id, tenantId);
    }

    private static async Task<Result<StockTransferResponse>> LoadResponse(IDbConnection conn, string id, int tenantId)
    {
        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        var items = await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id });
        return Result<StockTransferResponse>.Success(await StockTransferMapper.ToResponseAsync(conn, header, items, tenantId));
    }
}

// ─── Base helper cho cac handler chuyen trang thai ─────────────────────────────
public abstract class StockTransferTransitionHandlerBase
{
    protected readonly IDapperConnectionFactory Db;
    protected readonly ICurrentUser CurrentUser;
    protected readonly IBranchProvider BranchProvider;
    protected readonly IAuditService Audit;

    protected StockTransferTransitionHandlerBase(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
    {
        Db = db; CurrentUser = currentUser; BranchProvider = branchProvider; Audit = audit;
    }

    protected async Task<dynamic?> LoadHeaderForUpdate(IDbConnection conn, string id, int tenantId, IDbTransaction? tx = null) =>
        await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header + " FOR UPDATE", new { id, tenantId }, tx);

    protected bool CanAccessBranch(int branchId) =>
        BranchProvider.IgnoreBranchFilter || BranchProvider.AllowedBranchIds.Contains(branchId) || BranchProvider.BranchId == branchId;

    protected async Task<Result<StockTransferResponse>> BuildResponse(IDbConnection conn, string id, int tenantId)
    {
        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        var items = await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id });
        return Result<StockTransferResponse>.Success(await StockTransferMapper.ToResponseAsync(conn, header, items, tenantId));
    }
}

// ─── Submit for approval ────────────────────────────────────────────────────
public class SubmitStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<SubmitStockTransferCommand, Result<StockTransferResponse>>
{
    public SubmitStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(SubmitStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        if ((string)header.status != StockTransferStatus.Draft)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState, "Chỉ có thể gửi duyệt phiếu ở trạng thái DRAFT");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_pha_stock_transfers
            SET status = @status, requested_by = @userId, requested_at = UTC_TIMESTAMP(), updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new { status = StockTransferStatus.PendingApproval, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId });

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id, new { action = "submit" }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Approve ─────────────────────────────────────────────────────────────────
public class ApproveStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<ApproveStockTransferCommand, Result<StockTransferResponse>>
{
    private readonly ISettingsProvider _settings;
    private readonly IPermissionChecker _permission;

    public ApproveStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IAuditService audit, ISettingsProvider settings, IPermissionChecker permission)
        : base(db, currentUser, branchProvider, audit)
    {
        _settings = settings; _permission = permission;
    }

    public async Task<Result<StockTransferResponse>> Handle(ApproveStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        if ((string)header.status != StockTransferStatus.PendingApproval)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState, "Chỉ có thể duyệt phiếu đang chờ duyệt (PENDING_APPROVAL)");

        // BR-59: nguoi tao khong tu duyet
        var createdByStr = header.requested_by != null ? (string)header.requested_by : null;
        if (!string.IsNullOrEmpty(createdByStr) && createdByStr == CurrentUser.UserId?.ToString())
            return Result<StockTransferResponse>.Failure(StockTransferErrors.SelfApproval, "Người tạo phiếu không được tự duyệt phiếu của mình");

        // BR-58: nguong duyet - >nguong yeu cau admin/vung (branch.group_view hoac IgnoreBranchFilter)
        var threshold = await _settings.GetDecimalAsync("stock_transfer_approval_threshold", 5_000_000m, ct);
        decimal totalValue = (decimal)header.total_value;
        if (totalValue > threshold)
        {
            var isRegionOrAdmin = BranchProvider.IgnoreBranchFilter || _permission.HasPermission("branch.group_view");
            if (!isRegionOrAdmin)
                return Result<StockTransferResponse>.Failure(StockTransferErrors.ApprovalPermissionRequired,
                    "Phiếu vượt ngưỡng duyệt — cần quản lý khu vực/quản trị viên phê duyệt");
        }

        // BR-56: cam dieu chuyen lo HSD < 90 ngay tru khi nguoi duyet ghi de
        var items = (await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id = cmd.Id })).ToList();
        var today = DateTime.UtcNow.Date;
        var nearExpiry = items.Where(i => i.expiry_date != null && ((DateTime)i.expiry_date - today).TotalDays < 90).ToList();
        if (nearExpiry.Count > 0 && !cmd.Request.OverrideExpiryGuard)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.ExpiryGuard,
                "Có lô hàng hạn sử dụng dưới 90 ngày — người duyệt cần xác nhận ghi đè để tiếp tục",
                new { items = nearExpiry.Select(i => new { drug_id = (string)i.drug_id, lot_no = (string?)i.lot_no, expiry_date = (DateTime?)i.expiry_date }) });

        await conn.ExecuteAsync(@"
            UPDATE diab_his_pha_stock_transfers
            SET status = @status, approved_by = @userId, approved_at = UTC_TIMESTAMP(), updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new { status = StockTransferStatus.Approved, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId });

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id,
            new { action = "approve", overrideExpiryGuard = cmd.Request.OverrideExpiryGuard }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Reject ──────────────────────────────────────────────────────────────────
public class RejectStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<RejectStockTransferCommand, Result<StockTransferResponse>>
{
    public RejectStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(RejectStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        if ((string)header.status != StockTransferStatus.PendingApproval)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState, "Chỉ có thể từ chối phiếu đang chờ duyệt (PENDING_APPROVAL)");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_pha_stock_transfers
            SET status = @status, rejected_reason = @reason, updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new { status = StockTransferStatus.Rejected, reason = cmd.Request.Reason, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId });

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id, new { action = "reject", reason = cmd.Request.Reason }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Ship (tru kho gui) ─────────────────────────────────────────────────────
public class ShipStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<ShipStockTransferCommand, Result<StockTransferResponse>>
{
    public ShipStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(ShipStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        if ((string)header.status != StockTransferStatus.Approved)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState, "Chỉ có thể xuất hàng phiếu ở trạng thái APPROVED");

        var fromBranchId = (int)header.from_branch_id;
        var items = (await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id = cmd.Id })).ToList();

        using var tx = conn.BeginTransaction();
        try
        {
            // BR-57 (buoc 1/2): tru kho gui - kiem tra du ton truoc khi tru
            foreach (var item in items)
            {
                string drugId = item.drug_id;
                string? lotNo = item.lot_no;
                decimal qty = item.qty_requested;

                var stockQuery = "SELECT id, quantity FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND branch_id = @branchId AND drug_id = @drugId" +
                                  (lotNo != null ? " AND lot_number = @lotNo" : " AND lot_number IS NULL") + " FOR UPDATE";
                dynamic? stock = await conn.QueryFirstOrDefaultAsync<dynamic>(stockQuery,
                    new { tenantId, branchId = fromBranchId, drugId, lotNo }, tx);

                if (stock is null)
                    throw new StockTransferBusinessException(StockTransferErrors.InsufficientStock,
                        $"Không đủ tồn kho tại chi nhánh gửi cho thuốc {drugId} (lô {lotNo ?? "-"})");

                decimal availableQty = stock.quantity;
                string stockId = stock.id;
                if (availableQty < qty)
                    throw new StockTransferBusinessException(StockTransferErrors.InsufficientStock,
                        $"Không đủ tồn kho tại chi nhánh gửi cho thuốc {drugId} (lô {lotNo ?? "-"})");

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_stock SET quantity = quantity - @qty WHERE id = @id",
                    new { qty, id = stockId }, tx);

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_stock_transfer_items SET qty_shipped = @qty WHERE id = @itemId",
                    new { qty, itemId = (string)item.id }, tx);
            }

            await conn.ExecuteAsync(@"
                UPDATE diab_his_pha_stock_transfers
                SET status = @status, shipped_by = @userId, shipped_at = UTC_TIMESTAMP(), updated_by = @userId
                WHERE id = @id AND tenant_id = @tenantId",
                new { status = StockTransferStatus.InTransit, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId }, tx);

            tx.Commit();
        }
        catch (StockTransferBusinessException ex)
        {
            tx.Rollback();
            return Result<StockTransferResponse>.Failure(ex.Code, ex.Message);
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id, new { action = "ship" }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Receive (cong kho nhan - toan bo) ──────────────────────────────────────
public class ReceiveStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<ReceiveStockTransferCommand, Result<StockTransferResponse>>
{
    public ReceiveStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(ReceiveStockTransferCommand cmd, CancellationToken ct)
    {
        return await StockTransferReceiveLogic.ExecuteAsync(
            Db, CurrentUser, Audit, cmd.Id, cmd.Request, partial: false, ct);
    }
}

public class PartialReceiveStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<PartialReceiveStockTransferCommand, Result<StockTransferResponse>>
{
    public PartialReceiveStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(PartialReceiveStockTransferCommand cmd, CancellationToken ct)
    {
        return await StockTransferReceiveLogic.ExecuteAsync(
            Db, CurrentUser, Audit, cmd.Id, cmd.Request, partial: true, ct);
    }
}

file class StockTransferBusinessException : Exception
{
    public string Code { get; }
    public StockTransferBusinessException(string code, string message) : base(message) => Code = code;
}

file static class StockTransferReceiveLogic
{
    public static async Task<Result<StockTransferResponse>> ExecuteAsync(
        IDapperConnectionFactory dbFactory, ICurrentUser currentUser, IAuditService audit,
        string id, ReceiveStockTransferRequest request, bool partial, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId!.Value;
        using var conn = (IDbConnection)dbFactory.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        if ((string)header.status != StockTransferStatus.InTransit)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState, "Chỉ có thể nhận hàng phiếu ở trạng thái IN_TRANSIT");

        var toBranchId = (int)header.to_branch_id;
        var fromBranchId = (int)header.from_branch_id;
        var items = (await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id })).ToList();
        var receiveMap = request.Items.ToDictionary(i => i.ItemId, i => i.QtyReceived);

        using var tx = conn.BeginTransaction();
        try
        {
            var isFullyReceived = true;
            decimal receivedValue = 0m; // BR-87: gia tri thuc nhan theo gia von, dung sinh cong no noi bo
            foreach (var item in items)
            {
                string itemId = item.id;
                string drugId = item.drug_id;
                string? lotNo = item.lot_no;
                DateTime? expiryDate = item.expiry_date;
                decimal qtyShipped = item.qty_shipped;

                var qtyReceived = receiveMap.TryGetValue(itemId, out var q) ? q : qtyShipped; // Receive toan bo neu khong chi ro
                if (qtyReceived > qtyShipped) qtyReceived = qtyShipped;
                if (qtyReceived < qtyShipped) isFullyReceived = false;
                if (qtyReceived <= 0) continue;

                // BR-57 (buoc 2/2): cong kho nhan
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, quantity FROM diab_his_pha_stock WHERE tenant_id = @tenantId AND branch_id = @branchId AND drug_id = @drugId" +
                    (lotNo != null ? " AND lot_number = @lotNo" : " AND lot_number IS NULL") + " FOR UPDATE",
                    new { tenantId, branchId = toBranchId, drugId, lotNo }, tx);

                if (existing != null)
                {
                    await conn.ExecuteAsync("UPDATE diab_his_pha_stock SET quantity = quantity + @qty WHERE id = @id",
                        new { qty = qtyReceived, id = (string)existing.id }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_pha_stock (id, tenant_id, branch_id, drug_id, lot_number, exp_date, quantity, import_price, created_at)
                        VALUES (@newId, @tenantId, @branchId, @drugId, @lotNo, @expDate, @qty, @unitCost, UTC_TIMESTAMP())",
                        new
                        {
                            newId = Guid.NewGuid().ToString(), tenantId, branchId = toBranchId, drugId,
                            lotNo = lotNo ?? "-", expDate = expiryDate ?? DateTime.UtcNow.Date.AddYears(1),
                            qty = qtyReceived, unitCost = (decimal)item.unit_cost
                        }, tx);
                }

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pha_stock_transfer_items SET qty_received = qty_received + @qty WHERE id = @itemId",
                    new { qty = qtyReceived, itemId }, tx);

                receivedValue += qtyReceived * (decimal)item.unit_cost;
            }

            var newStatus = isFullyReceived ? StockTransferStatus.Received : StockTransferStatus.PartiallyReceived;
            await conn.ExecuteAsync(@"
                UPDATE diab_his_pha_stock_transfers
                SET status = @status, received_by = @userId, received_at = UTC_TIMESTAMP(), updated_by = @userId
                WHERE id = @id AND tenant_id = @tenantId",
                new { status = newStatus, userId = currentUser.UserId?.ToString(), id, tenantId }, tx);

            // BR-87: dieu chuyen kho RECEIVED/PARTIALLY_RECEIVED -> sinh but toan doi soat noi bo
            // (debtor=chi nhanh nhan hang, creditor=chi nhanh gui hang). Day la but toan noi bo 1
            // phap nhan (BR-55/Q3=Khong voi kich ban cung MST) - KHONG xuat hoa don/chung tu ban hang,
            // chi phuc vu doi chieu cuoi ky (BR-87, muc 6.2 "Doi chieu cong no lien don vi").
            if (receivedValue > 0)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO diab_his_bil_inter_branch_debts
                        (id, tenant_id, debtor_branch_id, creditor_branch_id, amount, source_type,
                         source_ref_id, source_ref_code, status, note, created_by, created_at)
                    VALUES
                        (UUID(), @tenantId, @debtorId, @creditorId, @amount, @sourceType,
                         @sourceRefId, @sourceRefCode, 'OPEN', @note, @userId, UTC_TIMESTAMP())",
                    new
                    {
                        tenantId, debtorId = toBranchId, creditorId = fromBranchId, amount = receivedValue,
                        sourceType = ProDiabHis.Application.Billing.InterBranchDebts.InterBranchDebtSourceType.StockTransfer,
                        sourceRefId = id, sourceRefCode = (string?)header.transfer_no,
                        note = $"Dieu chuyen kho {(string?)header.transfer_no} - gia tri thuc nhan theo gia von",
                        userId = currentUser.UserId?.ToString()
                    }, tx);
            }

            tx.Commit();

            await audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", id,
                new { action = partial ? "partial_receive" : "receive", status = newStatus }, ct);

            var resultHeader = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id, tenantId });
            var resultItems = await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id });
            return Result<StockTransferResponse>.Success(await StockTransferMapper.ToResponseAsync(conn, resultHeader!, resultItems, tenantId));
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

// ─── Close ───────────────────────────────────────────────────────────────────
public class CloseStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<CloseStockTransferCommand, Result<StockTransferResponse>>
{
    public CloseStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(CloseStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        var status = (string)header.status;
        if (status != StockTransferStatus.Received && status != StockTransferStatus.PartiallyReceived)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState,
                "Chỉ có thể đóng phiếu ở trạng thái RECEIVED hoặc PARTIALLY_RECEIVED");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_pha_stock_transfers SET status = @status, updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new { status = StockTransferStatus.Closed, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId });

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id, new { action = "close" }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Cancel ──────────────────────────────────────────────────────────────────
public class CancelStockTransferHandler : StockTransferTransitionHandlerBase, IRequestHandler<CancelStockTransferCommand, Result<StockTransferResponse>>
{
    public CancelStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider, IAuditService audit)
        : base(db, currentUser, branchProvider, audit) { }

    public async Task<Result<StockTransferResponse>> Handle(CancelStockTransferCommand cmd, CancellationToken ct)
    {
        var tenantId = CurrentUser.TenantId!.Value;
        using var conn = (IDbConnection)Db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = cmd.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");
        var status = (string)header.status;
        if (status != StockTransferStatus.Draft && status != StockTransferStatus.PendingApproval && status != StockTransferStatus.Approved)
            return Result<StockTransferResponse>.Failure(StockTransferErrors.InvalidState,
                "Chỉ có thể huỷ phiếu chưa xuất hàng (DRAFT/PENDING_APPROVAL/APPROVED)");

        await conn.ExecuteAsync(@"
            UPDATE diab_his_pha_stock_transfers SET status = @status, updated_by = @userId
            WHERE id = @id AND tenant_id = @tenantId",
            new { status = StockTransferStatus.Cancelled, userId = CurrentUser.UserId?.ToString(), id = cmd.Id, tenantId });

        await Audit.LogAsync(Domain.Entities.AuditAction.Update, "StockTransfer", cmd.Id, new { action = "cancel" }, ct);
        return await BuildResponse(conn, cmd.Id, tenantId);
    }
}

// ─── Queries ─────────────────────────────────────────────────────────────────
public class ListStockTransfersHandler : IRequestHandler<ListStockTransfersQuery, Result<PagedResult<StockTransferResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public ListStockTransfersHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<PagedResult<StockTransferResponse>>> Handle(ListStockTransfersQuery q, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        var where = new List<string> { "t.tenant_id = @tenantId", "t.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);

        // BR-60: filter tuong minh - user o from_branch HOAC to_branch deu xem duoc (KHONG bo qua branch
        // filter ngoai truong hop co IgnoreBranchFilter/branch.group_view - da duoc BranchProvider tinh vao AllowedBranchIds/IgnoreBranchFilter).
        if (!_branchProvider.IgnoreBranchFilter)
        {
            if (q.BranchId.HasValue)
            {
                where.Add("(t.from_branch_id = @branchId OR t.to_branch_id = @branchId)");
                prm.Add("branchId", q.BranchId.Value);
            }
            else
            {
                var allowed = _branchProvider.AllowedBranchIds.Count > 0
                    ? _branchProvider.AllowedBranchIds.ToList()
                    : new List<int> { _branchProvider.BranchId };
                where.Add("(t.from_branch_id IN @allowed OR t.to_branch_id IN @allowed)");
                prm.Add("allowed", allowed);
            }
        }
        else if (q.BranchId.HasValue)
        {
            where.Add("(t.from_branch_id = @branchId OR t.to_branch_id = @branchId)");
            prm.Add("branchId", q.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            where.Add("t.status = @status");
            prm.Add("status", q.Status);
        }

        var wc = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_pha_stock_transfers t WHERE {wc}", prm);

        var offset = (q.Page - 1) * q.PageSize;
        prm.Add("offset", offset);
        prm.Add("limit", q.PageSize);
        var headers = (await conn.QueryAsync<dynamic>(
            $"SELECT t.* FROM diab_his_pha_stock_transfers t WHERE {wc} ORDER BY t.created_at DESC LIMIT @limit OFFSET @offset", prm)).ToList();

        var items = new List<StockTransferResponse>();
        foreach (var h in headers)
        {
            var itemRows = await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id = (string)h.id });
            items.Add(await StockTransferMapper.ToResponseAsync(conn, h, itemRows, tenantId));
        }

        return Result<PagedResult<StockTransferResponse>>.Success(new PagedResult<StockTransferResponse>(items, q.Page, q.PageSize, total));
    }
}

public class GetStockTransferHandler : IRequestHandler<GetStockTransferQuery, Result<StockTransferResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public GetStockTransferHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db; _currentUser = currentUser; _branchProvider = branchProvider;
    }

    public async Task<Result<StockTransferResponse>> Handle(GetStockTransferQuery q, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        using var conn = (IDbConnection)_db.CreateConnection();
        conn.Open();

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(StockTransferSql.Header, new { id = q.Id, tenantId });
        if (header == null) return Result<StockTransferResponse>.Failure(StockTransferErrors.NotFound, "Không tìm thấy phiếu điều chuyển");

        // BR-60: user phai thuoc from_branch hoac to_branch (tru khi IgnoreBranchFilter)
        if (!_branchProvider.IgnoreBranchFilter)
        {
            var allowed = _branchProvider.AllowedBranchIds.Count > 0
                ? _branchProvider.AllowedBranchIds.ToList()
                : new List<int> { _branchProvider.BranchId };
            int fromB = (int)header.from_branch_id, toB = (int)header.to_branch_id;
            if (!allowed.Contains(fromB) && !allowed.Contains(toB))
                return Result<StockTransferResponse>.Failure(StockTransferErrors.BranchAccessDenied, "Bạn không có quyền xem phiếu điều chuyển của chi nhánh này");
        }

        var items = await conn.QueryAsync<dynamic>(StockTransferSql.Items, new { id = q.Id });
        return Result<StockTransferResponse>.Success(await StockTransferMapper.ToResponseAsync(conn, header, items, tenantId));
    }
}
