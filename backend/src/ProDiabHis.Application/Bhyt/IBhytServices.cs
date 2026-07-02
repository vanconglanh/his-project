namespace ProDiabHis.Application.Bhyt;

// ── XML Generation ────────────────────────────────────────────────────────────

public record BhytXmlGenerateResult(
    bool Success,
    int EncounterCount,
    decimal TotalRequestedAmount,
    IReadOnlyList<BhytExportItemData> Items,
    string? ErrorMessage);

public record BhytExportItemData(
    int TableNo,
    int RecordIndex,
    string RowDataJson,
    string? MaLienKet,
    string? SourceEncounterId,
    string? SourceBillingId,
    decimal RequestAmount);

public interface IBhytXmlGenerator
{
    /// <summary>Query encounters trong period_month, build XML Bang 1-5 theo QD 4750.</summary>
    Task<BhytXmlGenerateResult> GenerateAsync(
        int exportId,
        int tenantId,
        string periodMonth,
        string? scopeFilterJson,
        CancellationToken ct);
}

// ── XSD Validation ────────────────────────────────────────────────────────────

public record BhytXsdValidationResult(
    bool Valid,
    IReadOnlyList<BhytValidationError> Errors);

public interface IBhytXsdValidator
{
    /// <summary>Validate XML cua export voi XSD QD 4750.</summary>
    Task<BhytXsdValidationResult> ValidateAsync(int exportId, CancellationToken ct);
}

// ── Digital Signing ───────────────────────────────────────────────────────────

public record BhytSignResult(
    bool Success,
    string? SignedFilePath,
    string? ErrorMessage);

public interface IBhytSigner
{
    /// <summary>PKCS#7 detached signature tren file XML cua export.</summary>
    Task<BhytSignResult> SignAsync(int exportId, string? certThumbprint, string? pin, CancellationToken ct);
}

// ── Submission ────────────────────────────────────────────────────────────────

public record BhytSubmissionResult(
    bool Success,
    string? Reference,
    string? ErrorMessage);

public interface IBhytSubmissionClient
{
    /// <summary>POST file XML da ky len cong giam dinh BHYT.</summary>
    Task<BhytSubmissionResult> SubmitAsync(int exportId, int tenantId, CancellationToken ct);
}

// ── Reconcile Parser ──────────────────────────────────────────────────────────

public record BhytReconcileItemData(
    int TableNo,
    string MaLienKet,
    decimal RequestAmount,
    decimal ApprovedAmount,
    decimal RejectedAmount,
    string Status,             // APPROVED | REJECTED | ADJUSTED
    string? RejectionCode,
    string? RejectionReason);

public record BhytReconcileParseResult(
    bool Success,
    IReadOnlyList<BhytReconcileItemData> Items,
    string? ErrorMessage);

public interface IBhytReconcileParser
{
    /// <summary>Parse XML ket qua doi soat tu cong BHYT thanh danh sach items.</summary>
    Task<BhytReconcileParseResult> ParseAsync(string filePath, CancellationToken ct);
}
