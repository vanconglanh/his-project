using System.Data;
using System.IO.Compression;
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
    private readonly ILogger<LegacyOcrBatchJob> _logger;

    private static readonly string[] AllowedImageExts = { ".jpg", ".jpeg", ".png" };
    private const int MaxFilesPerZip = 200;
    private const long MaxImageBytes = 15L * 1024 * 1024;
    private const long MaxTotalExtractedBytes = 500L * 1024 * 1024;

    public LegacyOcrBatchJob(IDapperConnectionFactory db, IFileStorage storage, IOcrTextProvider ocr, ILogger<LegacyOcrBatchJob> logger)
    {
        _db = db; _storage = storage; _ocr = ocr; _logger = logger;
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

            using var archive = new ZipArchive(zipMemory, ZipArchiveMode.Read);

            // Loc entry hop le: chi anh jpg/png, chan path traversal, gioi han so luong/kich thuoc (zip bomb)
            var validEntries = new List<ZipArchiveEntry>();
            long totalExtractedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // thu muc
                if (entry.FullName.Contains("..") || Path.IsPathRooted(entry.FullName)) continue; // path traversal
                var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                if (!AllowedImageExts.Contains(ext)) continue;
                if (entry.Length <= 0 || entry.Length > MaxImageBytes) continue;

                totalExtractedBytes += entry.Length;
                if (totalExtractedBytes > MaxTotalExtractedBytes) break; // chan zip bomb
                validEntries.Add(entry);
                if (validEntries.Count >= MaxFilesPerZip) break;
            }

            await conn.ExecuteAsync(
                "UPDATE diab_his_leg_import_batch SET total_items=@Total, updated_at=NOW() WHERE id=@Id",
                new { Total = validEntries.Count, Id = batchId });

            var processed = 0;
            foreach (var entry in validEntries)
            {
                ct.ThrowIfCancellationRequested();
                var itemId = Guid.NewGuid().ToString();
                try
                {
                    await using var entryStream = entry.Open();
                    using var imgMemory = new MemoryStream();
                    await entryStream.CopyToAsync(imgMemory, ct);
                    var imageBytes = imgMemory.ToArray();

                    var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                    var mime = ext == ".png" ? "image/png" : "image/jpeg";
                    var objectKey = $"images/{tenantId}/{batchId}/{itemId}{ext}";

                    imgMemory.Position = 0;
                    await _storage.UploadAsync(FileBuckets.LegacyScans, objectKey, imgMemory, mime, ct);

                    var ocrResult = await _ocr.ExtractTextAsync(imageBytes, entry.Name, ct);
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
