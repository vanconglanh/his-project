using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.RadResults.Ocr;

// ═══════════════════════════════════════════════════════════════════════════
// EXTRACT — upload file (PDF/anh) -> OCR -> tach 2 doan Mo ta/Ket luan.
// KHONG ghi DB (stateless), chi tra ve de UI xac nhan/sua truoc khi luu.
// ═══════════════════════════════════════════════════════════════════════════
public class ExtractRadResultOcrCommandHandler
    : IRequestHandler<ExtractRadResultOcrCommand, Result<RadOcrExtractResponse>>
{
    private readonly IRadOcrTextProvider _ocr;
    private readonly ILogger<ExtractRadResultOcrCommandHandler> _logger;

    // Chap nhan PDF + cac dinh dang anh scan pho bien (giong Lab OCR)
    private static readonly string[] AllowedMimes =
    {
        "application/pdf", "image/png", "image/jpeg", "image/jpg", "image/webp", "image/bmp", "image/tiff"
    };
    private const long MaxBytes = 20L * 1024 * 1024;

    public ExtractRadResultOcrCommandHandler(IRadOcrTextProvider ocr,
        ILogger<ExtractRadResultOcrCommandHandler> logger)
    { _ocr = ocr; _logger = logger; }

    public async Task<Result<RadOcrExtractResponse>> Handle(ExtractRadResultOcrCommand cmd, CancellationToken ct)
    {
        var contentType = (cmd.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedMimes.Contains(contentType))
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_INVALID_FORMAT",
                "Chỉ chấp nhận file PDF hoặc ảnh (PNG/JPG/WEBP/BMP/TIFF)");

        using var buffer = new MemoryStream();
        await cmd.FileStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_UPLOAD_FAILED", "Tải tệp thất bại, vui lòng thử lại");
        if (buffer.Length > MaxBytes)
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_TOO_LARGE", "File vượt quá dung lượng tối đa 20MB");

        // Trich text qua ha tang OCR da co (PDF: PdfPig + fallback; anh: Tesseract)
        var extract = await _ocr.ExtractTextAsync(buffer.ToArray(), cmd.FileName, contentType, ct);
        if (!extract.IsSuccess)
            return Result<RadOcrExtractResponse>.Failure(extract.ErrorCode!, extract.ErrorMessage!);

        var parsed = RadResultOcrParser.Parse(extract.Value);

        _logger.LogInformation(
            "Rad OCR extract file={FileName} findings={HasFindings} conclusion={HasConclusion}",
            cmd.FileName,
            !string.IsNullOrWhiteSpace(parsed.Findings),
            !string.IsNullOrWhiteSpace(parsed.Conclusion));

        return Result<RadOcrExtractResponse>.Success(new RadOcrExtractResponse(
            parsed.Findings, parsed.Impression, parsed.Conclusion, parsed.Recommendations,
            parsed.HasAnyExtracted, parsed.RawText));
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CONFIRM — nguoi dung xac nhan/sua tay 2 o Mo ta/Ket luan roi luu. TAI DUNG
// CreateRadResultCommand (da co payment gate G02, audit) — khong nhan doi logic.
// ═══════════════════════════════════════════════════════════════════════════
public class ConfirmRadResultOcrCommandHandler
    : IRequestHandler<ConfirmRadResultOcrCommand, Result<RadOcrConfirmResponse>>
{
    private readonly IMediator _mediator;

    public ConfirmRadResultOcrCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<RadOcrConfirmResponse>> Handle(ConfirmRadResultOcrCommand cmd, CancellationToken ct)
    {
        if (cmd.RadOrderId == Guid.Empty)
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_ORDER_REQUIRED",
                "Vui lòng chọn chỉ định CĐHA để lưu kết quả");
        if (string.IsNullOrWhiteSpace(cmd.Findings))
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_FINDINGS_REQUIRED",
                "Mô tả hình ảnh không được để trống");
        if (string.IsNullOrWhiteSpace(cmd.Conclusion))
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_CONCLUSION_REQUIRED",
                "Kết luận không được để trống");

        var req = new RadResultCreateRequest(
            cmd.RadOrderId,
            cmd.Findings.Trim(),
            string.IsNullOrWhiteSpace(cmd.Impression) ? null : cmd.Impression!.Trim(),
            cmd.Conclusion.Trim(),
            string.IsNullOrWhiteSpace(cmd.Recommendations) ? null : cmd.Recommendations!.Trim(),
            cmd.PerformedAt);

        var result = await _mediator.Send(new CreateRadResultCommand(req), ct);
        if (!result.IsSuccess)
            return Result<RadOcrConfirmResponse>.Failure(result.ErrorCode!, result.ErrorMessage!, result.ErrorDetails);

        return Result<RadOcrConfirmResponse>.Success(
            new RadOcrConfirmResponse(result.Value!.Id, result.Value.Status));
    }
}
