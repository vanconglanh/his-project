namespace ProDiabHis.Application.LabResults;

// ─────────── Enums / Constants ───────────
public static class LabResultFlag
{
    public const string Normal   = "NORMAL";
    public const string H        = "H";
    public const string L        = "L";
    public const string HH       = "HH";
    public const string LL       = "LL";
    public const string Critical = "CRITICAL";
}

public static class LabResultStatus
{
    public const string Draft    = "DRAFT";
    public const string Verified = "VERIFIED";
    public const string Amended  = "AMENDED";
}

public static class LabResultSource
{
    public const string Manual  = "MANUAL";
    public const string Import  = "IMPORT";
    public const string Partner = "PARTNER";
}

// ─────────── Response DTO ───────────
public record LabResultResponse(
    Guid     Id,
    Guid     LabOrderId,
    Guid     LabOrderItemId,
    Guid     PatientId,
    Guid     EncounterId,
    string   TestCode,
    string   TestName,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    string   Flag,
    string?  Method,
    DateTime PerformedAt,
    Guid?    PerformedBy,
    string   Status,
    DateTime? VerifiedAt,
    Guid?    VerifiedBy,
    string?  Note,
    string   Source,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ─────────── Request DTOs ───────────
public record LabResultCreateRequest(
    Guid     LabOrderItemId,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    string?  Method,
    DateTime PerformedAt,
    string?  Note);

public record LabResultUpdateRequest(
    string?  Value,
    decimal? ValueNumeric,
    string?  Unit,
    string?  Method,
    string?  Note,
    string?  AmendReason);

// ─────────── Trend DTO ───────────
public record TrendPoint(DateTime PerformedAt, decimal? ValueNumeric, string Flag);

public record LabResultTrendResponse(
    string         TestCode,
    string         TestName,
    string?        Unit,
    decimal?       ReferenceRangeLow,
    decimal?       ReferenceRangeHigh,
    List<TrendPoint> Points);

// ─────────── Import DTO ───────────
public record ImportErrorItem(int Row, string Message);

public record ImportLabResultsResponse(
    int                    TotalRows,
    int                    SuccessCount,
    int                    FailedCount,
    List<ImportErrorItem>  Errors);

// ─────────── Batch Verify DTO ───────────
public record BatchVerifyErrorItem(string Id, string Code, string Message);

public record BatchVerifyResponse(
    int                        SuccessCount,
    int                        FailedCount,
    List<BatchVerifyErrorItem> Errors);
