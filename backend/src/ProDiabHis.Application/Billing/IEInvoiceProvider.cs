namespace ProDiabHis.Application.Billing;

public record EInvoiceIssueRequest(
    Guid BillingId,
    decimal TotalAmount,
    decimal VatAmount,
    string? BuyerName,
    string? BuyerTaxCode,
    string? BuyerAddress,
    string? BuyerEmail,
    string? BuyerPhone,
    bool SendEmail);

public record EInvoiceIssueResult(
    string CqtCode,
    string InvoiceNo,
    string? InvoiceSeries,
    string? PdfUrl,
    string? XmlUrl);

public record EInvoiceCancelResult(bool Success, string? ErrorMessage);

/// <summary>Abstraction cho nha cung cap hoa don dien tu</summary>
public interface IEInvoiceProvider
{
    string ProviderName { get; }
    Task<EInvoiceIssueResult> IssueAsync(EInvoiceIssueRequest request, CancellationToken ct = default);
    Task<EInvoiceCancelResult> CancelAsync(string invoiceNo, string reason, CancellationToken ct = default);
    Task<string> GetXmlAsync(string invoiceNo, CancellationToken ct = default);
}
