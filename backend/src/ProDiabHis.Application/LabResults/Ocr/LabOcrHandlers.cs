using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.LabResults.Ocr;

// EXTRACT -- upload file -> OCR -> parse XN dang cho. Luu file goc len MinIO + fil_files (GAP-8),
// timeout OCR 90s (GAP-7), co canh bao ngoai khoang vat ly (GAP-3). KHONG ghi LabResult.
public class ExtractLabResultOcrCommandHandler
    : IRequestHandler<ExtractLabResultOcrCommand, Result<LabOcrExtractResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly ILabOcrTextProvider _ocr;
    private readonly ILogger<ExtractLabResultOcrCommandHandler> _logger;

    private static readonly string[] AllowedMimes =
    {
        "application/pdf", "image/png", "image/jpeg", "image/jpg", "image/webp", "image/bmp", "image/tiff"
    };
    private const long MaxBytes = 20L * 1024 * 1024;
    private static readonly TimeSpan OcrTimeout = TimeSpan.FromSeconds(90);

    public ExtractLabResultOcrCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, ILabOcrTextProvider ocr,
        ILogger<ExtractLabResultOcrCommandHandler> logger)
    { _db = db; _tenant = tenant; _user = user; _storage = storage; _ocr = ocr; _logger = logger; }

    public async Task<Result<LabOcrExtractResponse>> Handle(ExtractLabResultOcrCommand cmd, CancellationToken ct)
    {
        var contentType = (cmd.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedMimes.Contains(contentType))
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_INVALID_FORMAT",
                "Ch\u1ec9 ch\u1ea5p nh\u1eadn file PDF ho\u1eb7c \u1ea3nh (PNG/JPG/WEBP/BMP/TIFF)");

        using var buffer = new MemoryStream();
        await cmd.FileStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_UPLOAD_FAILED", "T\u1ea3i t\u1ec7p th\u1ea5t b\u1ea1i, vui l\u00f2ng th\u1eed l\u1ea1i");
        if (buffer.Length > MaxBytes)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_TOO_LARGE", "File v\u01b0\u1ee3t qu\u00e1 dung l\u01b0\u1ee3ng t\u1ed1i \u0111a 20MB");

        var pending = await LoadPendingTestsAsync(cmd.EncounterId, ct);
        if (pending.Count == 0)
            return Result<LabOcrExtractResponse>.Failure("LAB_OCR_NO_PENDING",
                "L\u01b0\u1ee3t kh\u00e1m n\u00e0y kh\u00f4ng c\u00f2n ch\u1ec9 \u0111\u1ecbnh x\u00e9t nghi\u1ec7m n\u00e0o \u0111ang ch\u1edd k\u1ebft qu\u1ea3");

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
                _logger.LogWarning("Lab OCR timeout encounter={EncounterId} file={FileName}", cmd.EncounterId, cmd.FileName);
                return Result<LabOcrExtractResponse>.Failure("LAB_OCR_TIMEOUT",
                    "X\u1eed l\u00fd OCR qu\u00e1 th\u1eddi gian cho ph\u00e9p (90 gi\u00e2y). File c\u00f3 th\u1ec3 qu\u00e1 l\u1edbn ho\u1eb7c nhi\u1ec1u trang, vui l\u00f2ng th\u1eed l\u1ea1i v\u1edbi file nh\u1ecf h\u01a1n.");
            }
        }

        if (!extract.IsSuccess)
            return Result<LabOcrExtractResponse>.Failure(extract.ErrorCode!, extract.ErrorMessage!);

        var parseResult = LabResultOcrParser.Parse(extract.Value, pending);

        // GAP-8: luu file goc
        var sourceFileId = await SaveSourceFileAsync(buffer, cmd.FileName, contentType, ct);

        _logger.LogInformation("Lab OCR extract encounter={EncounterId} pending={Pending} extracted={Extracted} fileId={FileId}",
            cmd.EncounterId, pending.Count, parseResult.ExtractedCount, sourceFileId);

        var fields = parseResult.Fields
            .Select(f =>
            {
                var (outOfRange, note) = LabPlausibleRanges.Check(f.TestCode, f.ValueNumeric, f.Unit);
                return new LabOcrExtractFieldDto(f.LabOrderItemId, f.TestCode, f.TestName,
                    f.RawValue, f.ValueNumeric, f.Unit, f.Extracted, outOfRange, note);
            })
            .ToList();

        return Result<LabOcrExtractResponse>.Success(new LabOcrExtractResponse(
            cmd.EncounterId, pending.Count, parseResult.ExtractedCount, fields, sourceFileId));
    }

    private async Task<Guid?> SaveSourceFileAsync(MemoryStream buffer, string? fileName, string contentType, CancellationToken ct)
    {
        try
        {
            var fileId = Guid.NewGuid();
            var ext = GuessExtension(contentType, fileName);
            var objectKey = $"lab-ocr/{_tenant.TenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{ext}";
            buffer.Position = 0;
            await _storage.UploadAsync(FileBuckets.LabOcrSources, objectKey, buffer, contentType, ct);

            using var conn = (IDbConnection)_db.CreateConnection();
            var now = DateTime.UtcNow;
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
                VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, @Size, 'LAB_OCR', @UploadedBy, @Now, @Now)",
                new
                {
                    Id = fileId.ToString(), TenantId = _tenant.TenantId,
                    Bucket = FileBuckets.LabOcrSources, Key = objectKey,
                    FileName = fileName, Mime = contentType, Size = buffer.Length,
                    UploadedBy = _user.UserId?.ToString(), Now = now
                }, cancellationToken: ct));
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lab OCR luu file goc that bai (bo qua)");
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

    private async Task<IReadOnlyList<LabOcrPendingTest>> LoadPendingTestsAsync(Guid encounterId, CancellationToken ct)
    {
        const string sql = @"
            SELECT  o.id         AS Id,
                    o.test_code  AS TestCode,
                    o.test_name  AS TestName
            FROM diab_his_cli_lab_orders o
            WHERE o.tenant_id = @TId
              AND o.encounter_id = @EncId
              AND o.deleted_at IS NULL
              AND o.status <> 'cancelled'
              AND NOT EXISTS (
                    SELECT 1 FROM diab_his_lab_results r
                    WHERE r.lab_order_item_id = o.id
                      AND r.tenant_id = o.tenant_id
                      AND r.deleted_at IS NULL)
            ORDER BY o.ordered_at";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(new CommandDefinition(sql,
            new { TId = _tenant.TenantId, EncId = encounterId.ToString() }, cancellationToken: ct));

        var list = new List<LabOcrPendingTest>();
        foreach (var r in rows)
        {
            if (!Guid.TryParse((string?)r.Id, out var itemId)) continue;
            list.Add(new LabOcrPendingTest(itemId, (string?)r.TestCode ?? string.Empty, (string?)r.TestName ?? string.Empty));
        }
        return list;
    }
}

// CONFIRM -- tai dung CreateLabResultCommand. Truyen SourceFileId (chung ca dot) + OcrRawValue (moi item).
public class ConfirmLabResultOcrCommandHandler
    : IRequestHandler<ConfirmLabResultOcrCommand, Result<LabOcrConfirmResponse>>
{
    private readonly IMediator _mediator;
    public ConfirmLabResultOcrCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<LabOcrConfirmResponse>> Handle(ConfirmLabResultOcrCommand cmd, CancellationToken ct)
    {
        var included = cmd.Items.Where(i => i.Include).ToList();
        if (included.Count == 0)
            return Result<LabOcrConfirmResponse>.Failure("LAB_OCR_NOTHING_TO_SAVE", "Ch\u01b0a ch\u1ecdn k\u1ebft qu\u1ea3 n\u00e0o \u0111\u1ec3 l\u01b0u");

        var errors = new List<ImportErrorItem>();
        var created = 0;
        var row = 0;
        foreach (var item in included)
        {
            row++;
            if (string.IsNullOrWhiteSpace(item.Value))
            {
                errors.Add(new ImportErrorItem(row, $"XN d\u00f2ng {row}: gi\u00e1 tr\u1ecb tr\u1ed1ng, b\u1ecf qua"));
                continue;
            }
            var req = new LabResultCreateRequest(
                item.LabOrderItemId,
                item.Value.Trim(),
                item.ValueNumeric,
                string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit!.Trim(),
                string.IsNullOrWhiteSpace(item.Method) ? "\u0110\u1ecdc t\u1eeb file k\u1ebft qu\u1ea3 (OCR)" : item.Method!.Trim(),
                cmd.PerformedAt,
                Note: null,
                SourceFileId: cmd.SourceFileId,
                OcrRawValue: string.IsNullOrWhiteSpace(item.OcrRawValue) ? null : item.OcrRawValue!.Trim());

            var result = await _mediator.Send(new CreateLabResultCommand(req), ct);
            if (result.IsSuccess) created++;
            else errors.Add(new ImportErrorItem(row, $"{result.ErrorCode}: {result.ErrorMessage}"));
        }
        return Result<LabOcrConfirmResponse>.Success(new LabOcrConfirmResponse(created, errors.Count, errors));
    }
}
