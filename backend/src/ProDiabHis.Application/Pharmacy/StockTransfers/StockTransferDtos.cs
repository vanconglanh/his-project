namespace ProDiabHis.Application.Pharmacy.StockTransfers;

/// <summary>Trang thai vong doi phieu dieu chuyen kho noi bo (muc 4.2 BRD - 8 trang thai).</summary>
public static class StockTransferStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string InTransit = "IN_TRANSIT";
    public const string Received = "RECEIVED";
    public const string PartiallyReceived = "PARTIALLY_RECEIVED";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";
}

public record StockTransferItemRequest(
    string DrugId,
    string? LotNo,
    DateOnly? ExpiryDate,
    decimal QtyRequested,
    decimal UnitCost,
    string? Note);

public record CreateStockTransferRequest(
    int FromBranchId,
    int ToBranchId,
    string? Reason,
    IReadOnlyList<StockTransferItemRequest> Items);

public record RejectStockTransferRequest(string Reason);

public record ReceiveItemRequest(string ItemId, decimal QtyReceived);

public record ReceiveStockTransferRequest(IReadOnlyList<ReceiveItemRequest> Items, string? Note);

public record ApproveStockTransferRequest(bool OverrideExpiryGuard = false);

public record StockTransferItemResponse(
    string Id,
    string DrugId,
    string? DrugName,
    string? LotNo,
    DateOnly? ExpiryDate,
    decimal QtyRequested,
    decimal QtyShipped,
    decimal QtyReceived,
    decimal UnitCost,
    string? Note);

public record StockTransferResponse(
    string Id,
    int TenantId,
    string TransferNo,
    int FromBranchId,
    string? FromBranchName,
    int ToBranchId,
    string? ToBranchName,
    string Status,
    decimal TotalValue,
    bool RequiresApproval,
    string? Reason,
    string? RequestedBy,
    DateTime? RequestedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? RejectedReason,
    string? ShippedBy,
    DateTime? ShippedAt,
    string? ReceivedBy,
    DateTime? ReceivedAt,
    IReadOnlyList<StockTransferItemResponse> Items,
    DateTime CreatedAt);
