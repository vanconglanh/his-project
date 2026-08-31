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
    DateTime UpdatedAt,
    // BUG FIX (UX): danh sach/form nhap ket qua XN truoc day KHONG co ten benh nhan,
    // nhan vien XN khong biet dang nhap/xem ket qua cua ai neu chi nhin PatientId (GUID).
    // Them 2 field cuoi (append, khong chen giua) de khong pha vo cac noi goi constructor
    // positional cu.
    string?  PatientName = null,
    string?  PatientCode = null,
    string?  SourceFileUrl = null);

// ─────────── Request DTOs ───────────
public record LabResultCreateRequest(
    Guid     LabOrderItemId,
    string   Value,
    decimal? ValueNumeric,
    string?  Unit,
    string?  Method,
    DateTime PerformedAt,
    string?  Note,
    Guid?    SourceFileId = null,
    string?  OcrRawValue  = null);

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
