using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LegacyImport;

// ─── DTO ────────────────────────────────────────────────────────────────────
public record LegacyImportBatchResponse(
    Guid Id,
    string? ZipFileName,
    int TotalItems,
    int ProcessedItems,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record LegacyImportItemResponse(
    Guid Id,
    Guid BatchId,
    string? OriginalFilename,
    string? ImageUrl,
    string? OcrText,
    decimal? OcrConfidence,
    Guid? MatchedPatientId,
    string? MatchedPatientName,
    string? MatchedPatientCode,
    string? MatchMethod,
    string Status,
    Guid? SavedClsUploadId,
    string? ItemError,
    DateTime CreatedAt,
    string? DocType = null);

// ─── Commands / Queries ─────────────────────────────────────────────────────
public record CreateLegacyImportBatchCommand(Stream ZipStream, string FileName, string ContentType, long SizeBytes)
    : IRequest<Result<LegacyImportBatchResponse>>;

public record ListLegacyImportBatchesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<LegacyImportBatchResponse>>>;

public record GetLegacyImportBatchQuery(Guid BatchId) : IRequest<Result<LegacyImportBatchResponse>>;

public record ListLegacyImportItemsQuery(Guid BatchId, string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<LegacyImportItemResponse>>>;

public record MatchLegacyImportItemCommand(Guid ItemId, Guid PatientId) : IRequest<Result<LegacyImportItemResponse>>;

public record ConfirmLegacyImportItemCommand(Guid ItemId, string? OcrText, Guid? PatientId, string? DocType = null)
    : IRequest<Result<LegacyImportItemResponse>>;

/// <summary>
/// Whitelist phan loai tai lieu khi confirm legacy-import. Tai su dung luong legacy-import de luu
/// them "don thuoc ngoai" / "giay chuyen vien" (GAP-9) — KHONG tu tao don thuoc chinh thuc.
/// </summary>
public static class LegacyImportDocTypes
{
    public const string HoSoCuScan   = "HO_SO_CU_SCAN";   // mac dinh
    public const string DonThuocNgoai = "DON_THUOC_NGOAI"; // don thuoc ngoai (scan)
    public const string GiayChuyenVien = "GIAY_CHUYEN_VIEN"; // giay chuyen vien (scan)

    public static readonly IReadOnlyList<string> All = new[] { HoSoCuScan, DonThuocNgoai, GiayChuyenVien };

    /// <summary>Chuan hoa: null/khong hop le -> HO_SO_CU_SCAN.</summary>
    public static string Normalize(string? docType)
        => !string.IsNullOrWhiteSpace(docType) && All.Contains(docType) ? docType! : HoSoCuScan;
}

public record RejectLegacyImportItemCommand(Guid ItemId) : IRequest<Result<LegacyImportItemResponse>>;
