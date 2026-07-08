namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF bien lai thu tien kho A5 theo QuestPDF (ADR-0001), dung chuan letterhead teal chung he thong</summary>
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
    List<ReceiptLineItem> Items,
    ProDiabHis.Application.Reports.LetterheadDto? Letterhead = null);

public record ReceiptLineItem(string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal);
