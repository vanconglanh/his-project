using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.InBody;

// ─── DTO ────────────────────────────────────────────────────────────────────
// Luu y: OutOfPlausibleRange/PlausibleRangeNote la field APPEND, co default value ->
// JSON cu trong extracted_fields_json (thieu 2 field nay) van deserialize duoc.
public record InBodyFieldDto(
    string IndicatorType,
    decimal? Value,
    string? Unit,
    bool Extracted,
    bool OutOfPlausibleRange = false,
    string? PlausibleRangeNote = null);

public record InBodyReportResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ExtractionStatus,
    string? FileUrl,
    IReadOnlyList<InBodyFieldDto> Fields,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);

// ─── Commands / Queries ─────────────────────────────────────────────────────
public record UploadInBodyReportCommand(Guid PatientId, Guid? EncounterId, Stream FileStream, string FileName, string ContentType)
    : IRequest<Result<InBodyReportResponse>>;

public record ConfirmInBodyFieldItem(string IndicatorType, decimal? Value, string? Unit, bool Include);

public record ConfirmInBodyReportCommand(Guid ReportId, Guid? EncounterId, IReadOnlyList<ConfirmInBodyFieldItem> Fields)
    : IRequest<Result<InBodyReportResponse>>;

public record ListInBodyReportsQuery(Guid PatientId, int Page, int PageSize) : IRequest<Result<PagedResult<InBodyReportResponse>>>;

// Soft-delete bao cao InBody (danh dau deleted_at, KHONG hard-delete).
public record DeleteInBodyReportCommand(Guid ReportId, string? Reason) : IRequest<Result<bool>>;
