using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF hoa don A5 theo QuestPDF (ADR-0001)</summary>
public interface IInvoicePdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(BillingResponse billing, PrintBillingOptions options, CancellationToken ct = default);
}

public record PrintBillingOptions(string CopyLabel = "BAN GOC", bool Reprint = false);
