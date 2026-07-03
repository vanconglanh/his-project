namespace ProDiabHis.Application.Pharmacy.Dispensing;

public record DispenseQueueItem(
    string PrescriptionId,
    string? PrescriptionCode,
    string? PatientId,
    string? PatientName,
    string? DoctorName,
    DateTime? SignedAt,
    int ItemsCount,
    decimal TotalAmount,
    bool IsBhyt);

public record DispenseRequest(
    string WarehouseId,
    string? Note,
    IReadOnlyList<DispenseItemRequest> Items);

public record DispenseItemRequest(
    string PrescriptionItemId,
    IReadOnlyList<BatchPickRequest> BatchPicks);

public record BatchPickRequest(string BatchNo, decimal Quantity);

public record DispenseRecordResponse(
    string Id,
    int TenantId,
    string PrescriptionId,
    string WarehouseId,
    DateTime DispensedAt,
    int? DispensedBy,
    string? DispensedByName,
    string Status,
    string? Note,
    IReadOnlyList<DispenseItemResponse> Items,
    decimal TotalAmount);

public record DispenseItemResponse(
    string Id,
    string PrescriptionItemId,
    string DrugId,
    string? DrugName,
    string BatchNo,
    DateOnly ExpiryDate,
    decimal Quantity,
    decimal UnitCost,
    decimal LineAmount);

public record RejectDispenseRequest(string Reason);
public record ReturnDispenseRequest(string Reason, IReadOnlyList<ReturnItemRequest> Items);
public record ReturnItemRequest(string DispenseItemId, decimal Quantity);
