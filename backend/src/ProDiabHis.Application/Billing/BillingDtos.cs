namespace ProDiabHis.Application.Billing;

// ---- Service Catalog ----

public record ServiceResponse(
    Guid Id,
    int TenantId,
    string Code,
    string Name,
    string Category,
    decimal Price,
    int VatRate,
    string? BhytCode,
    decimal? BhytMaxAmount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ServiceUpsertRequest(
    string Code,
    string Name,
    string Category,
    decimal Price,
    int VatRate,
    string? BhytCode,
    decimal? BhytMaxAmount,
    bool IsActive = true);

public record ServicePackageItemDto(
    Guid ServiceId,
    string? ServiceName,
    decimal? UnitPrice,
    int Quantity);

public record ServicePackageResponse(
    Guid Id,
    int TenantId,
    string Code,
    string Name,
    List<ServicePackageItemDto> Services,
    decimal TotalPrice,
    decimal DiscountPercent,
    decimal FinalPrice,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);

public record ServicePackageUpsertRequest(
    string Code,
    string Name,
    List<ServicePackageItemInput> Services,
    decimal DiscountPercent = 0,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null,
    bool IsActive = true);

public record ServicePackageItemInput(Guid ServiceId, int Quantity);

public record ImportResultResponse(int Total, int Inserted, int Updated, List<object> Errors);

// ---- Billing ----

public record PatientSummaryDto(
    string FullName,
    DateOnly? Dob,
    string? Gender,
    string? Phone,
    string? BhytCardNoMasked);

public record BillingItemDto(
    Guid Id,
    string Type,
    Guid? RefId,
    string? Code,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    int VatRate,
    decimal DiscountPercent,
    decimal LineTotal,
    bool BhytApplicable,
    decimal BhytAmount);

public record BillingResponse(
    Guid Id,
    int TenantId,
    Guid? EncounterId,
    Guid PatientId,
    PatientSummaryDto? PatientSummary,
    string? BillNo,
    List<BillingItemDto> Items,
    decimal Subtotal,
    decimal VatTotal,
    decimal DiscountAmount,
    decimal BhytAmount,
    decimal PatientPayable,
    decimal PaidAmount,
    decimal Balance,
    string Status,
    DateOnly? PaymentDueDate,
    string Payer,
    string? Note,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? FinalizedAt,
    string? VoidReason);

public record CreateBillingRequest(
    Guid EncounterId,
    bool IncludeDispensing = true,
    string Payer = "SELF",
    string? Note = null);

public record UpdateBillingRequest(
    string? Note,
    decimal? DiscountAmount,
    DateOnly? PaymentDueDate);

public record BillingItemUpsertRequest(
    string Type,
    Guid? RefId,
    string? Code,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    int VatRate = 0,
    decimal DiscountPercent = 0,
    bool BhytApplicable = false);

public record VoidBillingRequest(string Reason);

public record ApplyBhytRequest(
    string BhytCardNo,
    int CopayRate,
    string RightRoute = "DUNG_TUYEN");

// ---- Payments ----

public record PaymentResponse(
    Guid Id,
    int TenantId,
    Guid BillingId,
    decimal Amount,
    string Method,
    string? Reference,
    string Status,
    string? Provider,
    string? ProviderTxnId,
    DateTime? PaidAt,
    Guid? PaidBy,
    Guid? CashierShiftId,
    string? Note,
    decimal RefundedAmount,
    DateTime CreatedAt);

public record CreatePaymentRequest(
    Guid BillingId,
    decimal Amount,
    string Method,
    string? Reference,
    string? Provider,
    string? ProviderTxnId,
    string? Note);

public record RefundPaymentRequest(decimal Amount, string Reason);
public record VoidPaymentRequest(string? Reason);

public record QrGenerateApiRequest(
    Guid BillingId,
    string Provider,
    decimal Amount,
    int ExpiresInSeconds = 900);

public record QrCodeResponseDto(
    Guid Id,
    Guid BillingId,
    string Provider,
    string QrPayload,
    string? QrUrl,
    decimal Amount,
    DateTime ExpiresAt,
    DateTime? PaidAt,
    string Status,
    string TransactionRef);

public record CardChargeApiRequest(
    Guid BillingId,
    decimal Amount,
    string CardToken,
    string Provider = "VISA",
    string? ThreeDsNonce = null);

public record PaymentMethodDto(string Method, bool IsActive, string? Provider);

// ---- eInvoice ----

public record EInvoiceResponse(
    Guid Id,
    int TenantId,
    Guid BillingId,
    string Provider,
    string? InvoiceNo,
    string? InvoiceSeries,
    string? CqtCode,
    DateTime? IssueDate,
    decimal TotalAmount,
    decimal VatAmount,
    string Status,
    string? PdfUrl,
    string? XmlUrl,
    DateTime? SignedAt,
    string? CancelReason,
    DateTime? CancelledAt,
    DateTime CreatedAt,
    Guid? CreatedBy);

public record IssueEInvoiceRequest(
    Guid BillingId,
    string Provider,
    EInvoiceBuyerDto? Buyer,
    bool SendEmail = true);

public record EInvoiceBuyerDto(
    string? Name,
    string? TaxCode,
    string? Address,
    string? Email,
    string? Phone);

public record CancelEInvoiceRequest(string Reason);

// ---- Cashier Closing ----

public record BreakdownByMethodDto(string Method, decimal Amount, int Count);

public record ShiftSummaryDto(
    decimal TotalCash,
    decimal TotalCard,
    decimal TotalTransfer,
    decimal TotalQr,
    decimal TotalOther,
    decimal TotalRefund,
    decimal TotalVoid,
    int CountTransactions,
    decimal GrossCollected,
    decimal NetCollected,
    List<BreakdownByMethodDto> BreakdownByMethod);

public record CashierClosingResponse(
    Guid Id,
    int TenantId,
    Guid CashierUserId,
    string? CashierName,
    DateOnly ShiftDate,
    DateTime ShiftStart,
    DateTime? ShiftEnd,
    ShiftSummaryDto Summary,
    decimal OpeningBalance,
    decimal? ClosingBalance,
    decimal? ExpectedCash,
    decimal? ActualCash,
    decimal? Difference,
    string? Note,
    string Status,
    Guid? ClosedBy,
    DateTime CreatedAt);

public record OpenShiftRequest(decimal OpeningBalance = 0, string? Note = null);

public record CloseShiftRequest(
    Guid? ShiftId,
    decimal ActualCash,
    string? Note,
    bool AcceptDifference = false);

public record DebtResponse(
    Guid PatientId,
    string? PatientCode,
    string PatientName,
    string? Phone,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal Balance,
    int UnpaidBillsCount,
    DateTime? LastPaymentAt,
    DateTime? OldestUnpaidAt,
    int? DaysOverdue);
