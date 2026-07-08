using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Billing;

/// <summary>Sinh PDF Bao cao ca thu ngan (QuestPDF), kho A4, dung chung khung thuong hieu diaB.</summary>
public interface ICashierShiftReportPdfBuilder
{
    byte[] Build(CashierShiftReportData data);
}

/// <summary>Du lieu render Bao cao ca thu ngan.</summary>
public record CashierShiftReportData(
    LetterheadDto Letterhead,
    Guid ShiftId,
    DateOnly ShiftDate,
    string? CashierName,
    DateTime ShiftStart,
    DateTime? ShiftEnd,
    string Status,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalCash,
    decimal TotalCard,
    decimal TotalTransfer,
    decimal TotalQr,
    decimal TotalOther,
    decimal TotalRefund,
    decimal TotalVoid,
    int CountTransactions,
    decimal? ExpectedCash,
    decimal? ActualCash,
    decimal? Difference,
    string? Note);
