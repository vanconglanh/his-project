using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.RadResults.Ocr;

// EXTRACT -- upload file -> OCR -> tach Mo ta/Ket luan. Luu file goc len MinIO + fil_files (GAP-8),
// timeout OCR 90s (GAP-7). KHONG ghi RadResult.
public class ExtractRadResultOcrCommandHandler
    : IRequestHandler<ExtractRadResultOcrCommand, Result<RadOcrExtractResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly IRadOcrTextProvider _ocr;
    private readonly ILogger<ExtractRadResultOcrCommandHandler> _logger;

    private static readonly string[] AllowedMimes =
    {
        "application/pdf", "image/png", "image/jpeg", "image/jpg", "image/webp", "image/bmp", "image/tiff"
    };
    private const long MaxBytes = 20L * 1024 * 1024;
    private static readonly TimeSpan OcrTimeout = TimeSpan.FromSeconds(90);

    public ExtractRadResultOcrCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, IRadOcrTextProvider ocr,
        ILogger<ExtractRadResultOcrCommandHandler> logger)
    { _db = db; _tenant = tenant; _user = user; _storage = storage; _ocr = ocr; _logger = logger; }

    public async Task<Result<RadOcrExtractResponse>> Handle(ExtractRadResultOcrCommand cmd, CancellationToken ct)
    {
        var contentType = (cmd.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedMimes.Contains(contentType))
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_INVALID_FORMAT",
                "Ch\u1ec9 ch\u1ea5p nh\u1eadn file PDF ho\u1eb7c \u1ea3nh (PNG/JPG/WEBP/BMP/TIFF)");

        using var buffer = new MemoryStream();
        await cmd.FileStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_UPLOAD_FAILED", "T\u1ea3i t\u1ec7p th\u1ea5t b\u1ea1i, vui l\u00f2ng th\u1eed l\u1ea1i");
        if (buffer.Length > MaxBytes)
            return Result<RadOcrExtractResponse>.Failure("RAD_OCR_TOO_LARGE", "File v\u01b0\u1ee3t qu\u00e1 dung l\u01b0\u1ee3ng t\u1ed1i \u0111a 20MB");

        // GAP-7: timeout OCR 90s
        Result<string> extract;
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            cts.CancelAfter(OcrTimeout);
            try
            {
                extract = await _ocr.ExtractTextAsync(buffer.ToArray(), cmd.FileName, contentType, cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Rad OCR timeout file={FileName}", cmd.FileName);
                return Result<RadOcrExtractResponse>.Failure("RAD_OCR_TIMEOUT",
                    "X\u1eed l\u00fd OCR qu\u00e1 th\u1eddi gian cho ph\u00e9p (90 gi\u00e2y). File c\u00f3 th\u1ec3 qu\u00e1 l\u1edbn ho\u1eb7c nhi\u1ec1u trang, vui l\u00f2ng th\u1eed l\u1ea1i v\u1edbi file nh\u1ecf h\u01a1n.");
            }
        }

        if (!extract.IsSuccess)
            return Result<RadOcrExtractResponse>.Failure(extract.ErrorCode!, extract.ErrorMessage!);

        var parsed = RadResultOcrParser.Parse(extract.Value);

        // GAP-8: luu file goc
        var sourceFileId = await SaveSourceFileAsync(buffer, cmd.FileName, contentType, ct);

        _logger.LogInformation("Rad OCR extract file={FileName} findings={HasFindings} conclusion={HasConclusion} fileId={FileId}",
            cmd.FileName, !string.IsNullOrWhiteSpace(parsed.Findings), !string.IsNullOrWhiteSpace(parsed.Conclusion), sourceFileId);

        return Result<RadOcrExtractResponse>.Success(new RadOcrExtractResponse(
            parsed.Findings, parsed.Impression, parsed.Conclusion, parsed.Recommendations,
            parsed.HasAnyExtracted, parsed.RawText, sourceFileId));
    }

    private async Task<Guid?> SaveSourceFileAsync(MemoryStream buffer, string? fileName, string contentType, CancellationToken ct)
    {
        try
        {
            var fileId = Guid.NewGuid();
            var ext = GuessExtension(contentType, fileName);
            var objectKey = $"rad-ocr/{_tenant.TenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{ext}";
            buffer.Position = 0;
            await _storage.UploadAsync(FileBuckets.RadOcrSources, objectKey, buffer, contentType, ct);

            using var conn = (IDbConnection)_db.CreateConnection();
            var now = DateTime.UtcNow;
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
                VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, @Size, 'RAD_OCR', @UploadedBy, @Now, @Now)",
                new
                {
                    Id = fileId.ToString(), TenantId = _tenant.TenantId,
                    Bucket = FileBuckets.RadOcrSources, Key = objectKey,
                    FileName = fileName, Mime = contentType, Size = buffer.Length,
                    UploadedBy = _user.UserId?.ToString(), Now = now
                }, cancellationToken: ct));
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rad OCR luu file goc that bai (bo qua)");
            return null;
        }
    }

    internal static string GuessExtension(string contentType, string? fileName)
    {
        var ct = (contentType ?? string.Empty).ToLowerInvariant();
        var ext = ct switch
        {
            "application/pdf" => ".pdf",
            "image/png"       => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp"      => ".webp",
            "image/bmp"       => ".bmp",
            "image/tiff"      => ".tiff",
            _ => null
        };
        if (ext is not null) return ext;
        var fromName = System.IO.Path.GetExtension(fileName ?? string.Empty);
        return string.IsNullOrWhiteSpace(fromName) ? ".bin" : fromName.ToLowerInvariant();
    }
}

// CONFIRM -- tai dung CreateRadResultCommand. Truyen SourceFileId + OcrRawText xuong.
public class ConfirmRadResultOcrCommandHandler
    : IRequestHandler<ConfirmRadResultOcrCommand, Result<RadOcrConfirmResponse>>
{
    private readonly IMediator _mediator;
    public ConfirmRadResultOcrCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<RadOcrConfirmResponse>> Handle(ConfirmRadResultOcrCommand cmd, CancellationToken ct)
    {
        if (cmd.RadOrderId == Guid.Empty)
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_ORDER_REQUIRED", "Vui l\u00f2ng ch\u1ecdn ch\u1ec9 \u0111\u1ecbnh C\u0110HA \u0111\u1ec3 l\u01b0u k\u1ebft qu\u1ea3");
        if (string.IsNullOrWhiteSpace(cmd.Findings))
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_FINDINGS_REQUIRED", "M\u00f4 t\u1ea3 h\u00ecnh \u1ea3nh kh\u00f4ng \u0111\u01b0\u1ee3c \u0111\u1ec3 tr\u1ed1ng");
        if (string.IsNullOrWhiteSpace(cmd.Conclusion))
            return Result<RadOcrConfirmResponse>.Failure("RAD_OCR_CONCLUSION_REQUIRED", "K\u1ebft lu\u1eadn kh\u00f4ng \u0111\u01b0\u1ee3c \u0111\u1ec3 tr\u1ed1ng");

        var req = new RadResultCreateRequest(
            cmd.RadOrderId,
            cmd.Findings.Trim(),
            string.IsNullOrWhiteSpace(cmd.Impression) ? null : cmd.Impression!.Trim(),
            cmd.Conclusion.Trim(),
            string.IsNullOrWhiteSpace(cmd.Recommendations) ? null : cmd.Recommendations!.Trim(),
            cmd.PerformedAt,
            SourceFileId: cmd.SourceFileId,
            OcrRawText: string.IsNullOrWhiteSpace(cmd.OcrRawText) ? null : cmd.OcrRawText!.Trim());

        var result = await _mediator.Send(new CreateRadResultCommand(req), ct);
        if (!result.IsSuccess)
            return Result<RadOcrConfirmResponse>.Failure(result.ErrorCode!, result.ErrorMessage!, result.ErrorDetails);

        return Result<RadOcrConfirmResponse>.Success(new RadOcrConfirmResponse(result.Value!.Id, result.Value.Status));
    }
}
