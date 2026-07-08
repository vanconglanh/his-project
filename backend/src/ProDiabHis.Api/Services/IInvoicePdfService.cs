using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF hoa don A5 theo QuestPDF (ADR-0001), dung chuan letterhead teal chung he thong.</summary>
public interface IInvoicePdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(
        BillingResponse billing,
        PrintBillingOptions options,
        CancellationToken ct = default,
        LetterheadDto? letterhead = null);
}

public record PrintBillingOptions(string CopyLabel = "BAN GOC", bool Reprint = false);
