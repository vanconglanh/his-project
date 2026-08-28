namespace ProDiabHis.Application.Packages;

public record EntitlementDefinitionRequest(string ItemType, Guid ItemRefId, decimal Quantity, string? Unit);

public record EntitlementDefinitionResponse(
    Guid Id, string ItemType, Guid ItemRefId, string ItemCode, string ItemName, string Unit, decimal Quantity, int SortOrder);

public record PackageUpsertRequest(
    string Code, string Name, string? Description, int DurationDays, decimal ListPrice,
    int VatRate, decimal? MinDepositPercent, bool IsActive,
    DateOnly? ValidFrom, DateOnly? ValidTo,
    List<EntitlementDefinitionRequest> Entitlements);

public record PackageResponse(
    Guid Id, int TenantId, string Code, string Name, string? Description,
    int DurationDays, decimal ListPrice, int VatRate, decimal? MinDepositPercent, bool IsActive,
    DateOnly? ValidFrom, DateOnly? ValidTo,
    List<EntitlementDefinitionResponse> Entitlements,
    decimal EstimatedValue,
    DateTime CreatedAt, DateTime UpdatedAt);

public record InitialPaymentRequest(decimal Amount, string Method, bool IssueEinvoice);

public record CreateSubscriptionRequest(
    Guid PatientId, Guid PackageId, decimal TotalPrice, DateOnly? EffectiveDate, string? Note,
    InitialPaymentRequest InitialPayment);

public record AddPaymentRequest(decimal Amount, string Method, bool IssueEinvoice, string? Note);

public record CancelSubscriptionRequest(string Reason);

public record BalanceResponse(
    Guid Id, string ItemType, string ItemCode, string ItemName, string Unit,
    decimal TotalQuantity, decimal UsedQuantity, decimal RemainingQuantity);

public record SubscriptionResponse(
    Guid Id, string SubscriptionNo, Guid PatientId, Guid PackageId,
    string PackageNameSnapshot, DateOnly PurchaseDate, DateOnly EffectiveDate, DateOnly ExpiryDate,
    decimal TotalPrice, decimal AmountPaid, decimal AmountDue, decimal? DepositPercentPaid,
    string PaymentStatus, string Status, DateTime? ActivatedAt,
    decimal RefundedAmount, DateTime? CancelledAt, string? CancelReason,
    List<BalanceResponse> Balances);
