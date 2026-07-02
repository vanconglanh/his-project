namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF bien lai K80 (58mm x chieu cao dong) theo QuestPDF (ADR-0001)</summary>
public interface IReceiptPdfService
{
    Task<byte[]> GenerateReceiptPdfAsync(ReceiptData receipt, bool reprint = false, CancellationToken ct = default);
}

public record ReceiptData(
    string ReceiptNo,
    string? TenantName,
    string? TenantAddress,
    string? TenantCskcbCode,
    string? PatientCode,
    string PatientName,
    string? Phone,
    DateTime PaidAt,
    string Method,
    decimal Amount,
    string? Reference,
    string? CashierName,
    List<ReceiptLineItem> Items);

public record ReceiptLineItem(string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal);
