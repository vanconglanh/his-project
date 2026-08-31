using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LegacyImport;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: giai nen ZIP ho so giay cu (an toan), OCR tung anh (Tesseract), tao item
/// cho admin review/match/confirm. KHONG tu dong tao benh an/luot kham - chi tao item cho.
/// </summary>
public class LegacyOcrBatchJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IFileStorage _storage;
    private readonly IOcrTextProvider _ocr;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ILogger<LegacyOcrBatchJob> _logger;

    // Whitelist dinh dang (anh/pdf/heic-guard) - xem LegacyImportFileClassifier de biet chi tiet
    // tung nhom xu ly ra sao (Image = OCR truc tiep, Pdf = IPdfTextExtractor, UnsupportedGuard =
    // HEIC/HEIF chua ho tro, tao item 'failed' voi thong bao ro rang thay vi am tham bo qua).
    private const int MaxFilesPerZip = 200;
    private const long MaxImageBytes = 15L * 1024 * 1024;
    private const long MaxTotalExtractedBytes = 500L * 1024 * 1024;

    public LegacyOcrBatchJob(IDapperConnectionFactory db, IFileStorage storage, IOcrTextProvider ocr, IPdfTextExtractor pdfExtractor, ILogger<LegacyOcrBatchJob> logger)
    {
        _db = db; _storage = storage; _ocr = ocr; _pdfExtractor = pdfExtractor; _logger = logger;
    }

    [Hangfire.Queue("ocr")]
    public async Task ExecuteAsync(string batchId, int tenantId, CancellationToken ct)
    {
        _logger.LogInformation("LegacyOcrBatchJob: start batchId={Id}", batchId);
        using var conn = (IDbConnection)_db.CreateConnection();

        try
        {
            var batch = await conn.QueryFirstOrDefaultAsync(
                "SELECT * FROM diab_his_leg_import_batch WHERE id=@Id AND tenant_id=@TenantId",
                new { Id = batchId, TenantId = tenantId });
            if (batch is null)
            {
                _logger.LogWarning("LegacyOcrBatchJob: khong tim thay batch {Id}", batchId);
                return;
            }

            await conn.ExecuteAsync(
                "UPDATE diab_his_leg_import_batch SET status='processing', updated_at=NOW() WHERE id=@Id",
                new { Id = batchId });

            var zipObjectKey = (string)batch.zip_object_key;
            await using var zipStream = await _storage.DownloadAsync(FileBuckets.LegacyScans, zipObjectKey, ct);
            using var zipMemory = new MemoryStream();
            await zipStream.CopyToAsync(zipMemory, ct);
            zipMemory.Position = 0;

            // Giai nen an toan bang co che dung chung (SafeZipExtractor): chan path traversal, chan zip
            // bomb (tong dung luong + so file + kich thuoc moi file). Giu ca heic/heif (Classify !=
            // Ignored) de duoi tao item 'failed' bao loi ro rang - KHONG am tham bo qua.
            var validEntries = await SafeZipExtractor.ExtractAsync(
                zipMemory,
                name => LegacyImportFileClassifier.Classify(name) != LegacyImportFileKind.Ignored,
                new ZipExtractLimits(MaxFilesPerZip, MaxImageBytes, MaxTotalExtractedBytes),
                ct);

            await conn.ExecuteAsync(
                "UPDATE diab_his_leg_import_batch SET total_items=@Total, updated_at=NOW() WHERE id=@Id",
                new { Total = validEntries.Count, Id = batchId });

            var processed = 0;
            foreach (var entry in validEntries)
            {
                ct.ThrowIfCancellationRequested();
                var itemId = Guid.NewGuid().ToString();
                var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                var kind = LegacyImportFileClassifier.Classify(entry.Name);
                try
                {
                    // HEIC/HEIF: guard ro rang, khong xu ly - bao loi cho admin biet phai chuyen doi.
                    if (kind == LegacyImportFileKind.UnsupportedGuard)
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO diab_his_leg_import_item
                                (id, tenant_id, batch_id, original_filename, status, item_error, created_at, updated_at)
                            VALUES
                                (@Id, @TenantId, @BatchId, @FileName, 'failed', @Err, NOW(), NOW())",
                            new
                            {
                                Id = itemId,
                                TenantId = tenantId,
                                BatchId = batchId,
                                FileName = entry.Name,
                                Err = "Định dạng HEIC/HEIF chưa được hỗ trợ, vui lòng chuyển đổi sang JPG/PNG hoặc PDF trước khi upload"
                            });
                        continue;
                    }

                    var fileBytes = entry.Bytes;
                    using var fileMemory = new MemoryStream(fileBytes);

                    var isPdf = kind == LegacyImportFileKind.Pdf;
                    var mime = isPdf ? "application/pdf" : ext switch
                    {
                        ".png" => "image/png",
                        ".tiff" or ".tif" => "image/tiff",
                        ".bmp" => "image/bmp",
                        _ => "image/jpeg"
                    };
                    var objectKey = $"images/{tenantId}/{batchId}/{itemId}{ext}";

                    fileMemory.Position = 0;
                    await _storage.UploadAsync(FileBuckets.LegacyScans, objectKey, fileMemory, mime, ct);

                    var ocrResult = isPdf
                        ? await _pdfExtractor.ExtractTextAsync(fileBytes, entry.Name, ct)
                        : await _ocr.ExtractTextAsync(fileBytes, entry.Name, ct);
                    var ocrText = ocrResult.IsSuccess ? ocrResult.Value : null;

                    // Parse ma benh nhan tu ten file: phan truoc dau "_" dau tien lam ung vien ma
                    var baseName = Path.GetFileNameWithoutExtension(entry.Name);
                    var candidateCode = baseName.Contains('_') ? baseName[..baseName.IndexOf('_')] : baseName;

                    string? matchedPatientId = null;
                    string? matchMethod = null;
                    if (!string.IsNullOrWhiteSpace(candidateCode))
                    {
                        var matches = (await conn.QueryAsync<string>(
                            "SELECT id FROM pat_patients WHERE tenant_id=@TenantId AND code=@Code AND deleted_at IS NULL",
                            new { TenantId = tenantId, Code = candidateCode })).ToList();
                        if (matches.Count == 1)
                        {
                            matchedPatientId = matches[0];
                            matchMethod = "filename_auto";
                        }
                    }

                    var status = matchedPatientId is not null ? "pending_review" : "pending_match";

                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_leg_import_item
                            (id, tenant_id, batch_id, original_filename, image_object_key, ocr_text, ocr_confidence,
                             matched_patient_id, match_method, status, created_at, updated_at)
                        VALUES
                            (@Id, @TenantId, @BatchId, @FileName, @ImageKey, @OcrText, NULL,
                             @MatchedPatientId, @MatchMethod, @Status, NOW(), NOW())",
                        new
                        {
                            Id = itemId,
                            TenantId = tenantId,
                            BatchId = batchId,
                            FileName = entry.Name,
                            ImageKey = objectKey,
                            OcrText = ocrText,
                            MatchedPatientId = matchedPatientId,
                            MatchMethod = matchMethod,
                            Status = status
                        });
                }
                catch (Exception exItem)
                {
                    _logger.LogError(exItem, "LegacyOcrBatchJob: loi xu ly anh {File} trong batch {Id}", entry.Name, batchId);
                    await conn.ExecuteAsync(@"
                        INSERT INTO diab_his_leg_import_item
                            (id, tenant_id, batch_id, original_filename, status, item_error, created_at, updated_at)
                        VALUES
                            (@Id, @TenantId, @BatchId, @FileName, 'failed', @Err, NOW(), NOW())",
                        new { Id = itemId, TenantId = tenantId, BatchId = batchId, FileName = entry.Name, Err = exItem.Message });
                }
                finally
                {
                    processed++;
                    await conn.ExecuteAsync(
                        "UPDATE diab_his_leg_import_batch SET processed_items=@Processed, updated_at=NOW() WHERE id=@Id",
                        new { Processed = processed, Id = batchId });
                }
            }

            await conn.ExecuteAsync(
                "UPDATE diab_his_leg_import_batch SET status='done', updated_at=NOW() WHERE id=@Id",
                new { Id = batchId });

            _logger.LogInformation("LegacyOcrBatchJob: done batchId={Id}, items={Count}", batchId, validEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LegacyOcrBatchJob: loi toan cuc batchId={Id}", batchId);
            await conn.ExecuteAsync(
                "UPDATE diab_his_leg_import_batch SET status='failed', error_message=@Msg, updated_at=NOW() WHERE id=@Id",
                new { Msg = $"Job error: {ex.Message}", Id = batchId });
            throw; // Hangfire retry
        }
    }
}
