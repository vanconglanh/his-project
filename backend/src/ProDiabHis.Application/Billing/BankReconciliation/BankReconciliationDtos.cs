namespace ProDiabHis.Application.Billing.BankReconciliation;

// ── DTO / Response ──────────────────────────────────────────────────────────

public record BankStatementResponse(
    Guid Id,
    string FileName,
    string? BankCode,
    DateOnly? StatementDate,
    int TotalLines,
    int MatchedLines,
    int UnmatchedLines,
    DateTime UploadedAt,
    string? UploadedByName);

public record MatchedPaymentDto(
    Guid Id,
    string? Reference,
    string Method,
    decimal Amount,
    DateTime? PaidAt,
    Guid BillingId);

public record BankStatementLineResponse(
    Guid Id,
    DateOnly? TransactionDate,
    decimal Amount,
    string? ReferenceNo,
    string? Description,
    string MatchStatus,
    Guid? MatchedPaymentId,
    MatchedPaymentDto? MatchedPayment);

public record BankStatementLinesResponse(
    BankStatementResponse Statement,
    List<BankStatementLineResponse> Lines);

public record PaymentCandidateDto(
    Guid Id,
    string? Reference,
    string Method,
    decimal Amount,
    DateTime? PaidAt,
    Guid BillingId);

// ── Raw line parsed tu file (Excel/CSV) ─────────────────────────────────────

public record BankStatementRawLine(
    DateOnly? TransactionDate,
    decimal Amount,
    string? ReferenceNo,
    string? Description);

// ── Parser interface (Infrastructure implement) ─────────────────────────────

public interface IBankStatementParser
{
    /// <summary>
    /// Parse file sao ke ngan hang (.xlsx qua ClosedXML, .csv qua text). Header dong 1,
    /// du lieu tu dong 2: A=transaction_date, B=amount, C=reference_no, D=description.
    /// Throw InvalidOperationException voi message "BANK_STATEMENT_INVALID_FORMAT:..." neu file loi dinh dang.
    /// </summary>
    Task<List<BankStatementRawLine>> ParseAsync(Stream fileStream, string fileName, string? contentType, CancellationToken ct = default);
}
