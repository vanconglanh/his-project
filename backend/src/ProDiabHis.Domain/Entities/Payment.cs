namespace ProDiabHis.Domain.Entities;

/// <summary>Thanh toan. Map bang diab_his_bil_payments</summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid BillingId { get; set; }
    public Guid? CashierShiftId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = PaymentStatus.Pending;
    public string? Reference { get; set; }
    public string? Provider { get; set; }
    public string? ProviderTxnId { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public string? Note { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class PaymentStatus
{
    public const string Pending = "PENDING";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string Refunded = "REFUNDED";
    public const string Void = "VOID";
}

/// <summary>QR code thanh toan. Map bang diab_his_bil_qr_codes</summary>
public class QrCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid BillingId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public string? QrUrl { get; set; }
    public decimal Amount { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Hoa don dien tu. Map bang diab_his_bil_einvoices</summary>
public class EInvoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid BillingId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? InvoiceNo { get; set; }
    public string? InvoiceSeries { get; set; }
    public string? CqtCode { get; set; }
    public DateTime? IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? PdfUrl { get; set; }
    public string? XmlUrl { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Ca thu ngan. Map bang diab_his_bil_cashier_shifts</summary>
public class CashierShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid CashierUserId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTime ShiftStart { get; set; } = DateTime.UtcNow;
    public DateTime? ShiftEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Difference { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalQr { get; set; }
    public decimal TotalOther { get; set; }
    public decimal TotalRefund { get; set; }
    public decimal TotalVoid { get; set; }
    public int CountTransactions { get; set; }
    public string? BreakdownJson { get; set; }
    public string Status { get; set; } = "OPEN";
    public string? Note { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
