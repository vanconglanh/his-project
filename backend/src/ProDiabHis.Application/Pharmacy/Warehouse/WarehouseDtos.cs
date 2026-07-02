namespace ProDiabHis.Application.Pharmacy.Warehouse;

public record WarehouseRequest(string Code, string Name, string Type, string? Address, int? ManagerUserId);

public record WarehouseResponse(
    string Id,
    int TenantId,
    string Code,
    string Name,
    string Type,
    string? Address,
    int? ManagerUserId,
    DateTime CreatedAt);

public record PurchaseOrderRequest(
    string SupplierId,
    int WarehouseId,
    string? OrderNo,
    DateTime? OrderedAt,
    DateOnly? ExpectedDelivery,
    string? Note,
    IReadOnlyList<PurchaseOrderItemRequest> Items);

public record PurchaseOrderItemRequest(int DrugId, decimal QuantityOrdered, decimal UnitPrice);

public record PurchaseOrderResponse(
    string Id,
    int TenantId,
    string SupplierId,
    string? SupplierName,
    int WarehouseId,
    string? OrderNo,
    DateTime? OrderedAt,
    DateOnly? ExpectedDelivery,
    string Status,
    IReadOnlyList<PurchaseOrderItemResponse> Items,
    decimal TotalAmount,
    DateTime CreatedAt);

public record PurchaseOrderItemResponse(
    int DrugId,
    string? DrugName,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal UnitPrice);

public record GoodsReceivedRequest(
    DateTime ReceivedAt,
    string? Note,
    IReadOnlyList<GrnItemRequest> Items);

public record GrnItemRequest(
    int DrugId,
    string BatchNo,
    DateOnly? ManufactureDate,
    DateOnly ExpiryDate,
    decimal QuantityReceived,
    decimal UnitCost);

public record GrnResponse(string GrnId, string PoStatus, IReadOnlyList<StockResponse> StocksUpdated);

public record StockResponse(
    string Id,
    int TenantId,
    int WarehouseId,
    string DrugId,
    string? DrugName,
    string BatchNo,
    DateOnly? ManufactureDate,
    DateOnly ExpiryDate,
    decimal QuantityAvailable,
    decimal QuantityReserved,
    decimal UnitCost,
    int DaysToExpiry,
    bool IsNearExpiry,
    bool IsLowStock);

public record StockMovementResponse(
    string Id,
    int TenantId,
    int WarehouseId,
    string MovementType,
    string DrugId,
    string? DrugName,
    string BatchNo,
    decimal Quantity,
    decimal UnitCost,
    string? ReferenceType,
    string? ReferenceId,
    DateTime MovementAt,
    int? PerformedBy,
    string? Reason);

public record StockAdjustmentRequest(
    int WarehouseId,
    string Reason,
    string? Note,
    IReadOnlyList<StockAdjustmentItem> Items);

public record StockAdjustmentItem(int DrugId, string BatchNo, decimal QuantityDiff);

public record AdjustmentResponse(string AdjustmentId, IReadOnlyList<StockMovementResponse> Movements);

public record TransferRequest(
    int FromWarehouseId,
    int ToWarehouseId,
    string? Note,
    IReadOnlyList<TransferItem> Items);

public record TransferItem(int DrugId, string BatchNo, decimal Quantity);

public record TransferResponse(string TransferId, IReadOnlyList<StockMovementResponse> Movements);
