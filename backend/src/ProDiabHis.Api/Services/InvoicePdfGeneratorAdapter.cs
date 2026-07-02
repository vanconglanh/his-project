using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Services;

/// <summary>
/// Adapter ket noi Application.IInvoicePdfGenerator voi InvoicePdfService (QuestPDF).
/// Dang ky DI trong Program.cs.
/// </summary>
public class InvoicePdfGeneratorAdapter : IInvoicePdfGenerator
{
    private readonly IInvoicePdfService _svc;

    public InvoicePdfGeneratorAdapter(IInvoicePdfService svc) => _svc = svc;

    public Task<byte[]> GenerateAsync(
        BillingResponse billing,
        string copyLabel,
        bool reprint,
        CancellationToken ct)
        => _svc.GenerateInvoicePdfAsync(billing, new PrintBillingOptions(copyLabel, reprint), ct);
}

/// <summary>
/// Adapter ket noi Application.IReceiptPdfGenerator voi ReceiptPdfService (QuestPDF).
/// </summary>
public class ReceiptPdfGeneratorAdapter : IReceiptPdfGenerator
{
    private readonly IReceiptPdfService _svc;

    public ReceiptPdfGeneratorAdapter(IReceiptPdfService svc) => _svc = svc;

    public Task<byte[]> GenerateAsync(
        ReceiptPrintData data,
        bool reprint,
        CancellationToken ct)
    {
        var receiptData = new ReceiptData(
            data.ReceiptNo,
            data.TenantName,
            data.TenantAddress,
            data.TenantCskcbCode,
            data.PatientCode,
            data.PatientName,
            data.Phone,
            data.PaidAt,
            data.Method,
            data.Amount,
            data.Reference,
            data.CashierName,
            data.Lines.Select(l => new ReceiptLineItem(l.Name, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());

        return _svc.GenerateReceiptPdfAsync(receiptData, reprint, ct);
    }
}
