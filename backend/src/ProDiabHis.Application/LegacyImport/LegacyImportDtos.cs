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
    DateTime CreatedAt);

// ─── Commands / Queries ─────────────────────────────────────────────────────
public record CreateLegacyImportBatchCommand(Stream ZipStream, string FileName, string ContentType, long SizeBytes)
    : IRequest<Result<LegacyImportBatchResponse>>;

public record ListLegacyImportBatchesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<LegacyImportBatchResponse>>>;

public record GetLegacyImportBatchQuery(Guid BatchId) : IRequest<Result<LegacyImportBatchResponse>>;

public record ListLegacyImportItemsQuery(Guid BatchId, string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<LegacyImportItemResponse>>>;

public record MatchLegacyImportItemCommand(Guid ItemId, Guid PatientId) : IRequest<Result<LegacyImportItemResponse>>;

public record ConfirmLegacyImportItemCommand(Guid ItemId, string? OcrText, Guid? PatientId) : IRequest<Result<LegacyImportItemResponse>>;

public record RejectLegacyImportItemCommand(Guid ItemId) : IRequest<Result<LegacyImportItemResponse>>;
