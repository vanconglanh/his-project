using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: generate XML Bang 1-5 cho ky export BHYT.
/// Long-running (toi 30 phut cho period nhieu BN).
/// Sau khi xong: UPDATE status = GENERATED + luu items + luu file XML that (IFileStorage).
/// </summary>
public class BhytGenerateXmlJob
{
    private readonly IBhytXmlGenerator _generator;
    private readonly IBhytXmlSerializer _serializer;
    private readonly IFileStorage _storage;
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BhytGenerateXmlJob> _logger;

    public BhytGenerateXmlJob(IBhytXmlGenerator generator, IBhytXmlSerializer serializer,
        IFileStorage storage, IDapperConnectionFactory db, ILogger<BhytGenerateXmlJob> logger)
    {
        _generator = generator; _serializer = serializer; _storage = storage; _db = db; _logger = logger;
    }

    [Hangfire.Queue("bhyt")]
    public async Task ExecuteAsync(int exportId, int tenantId, string periodMonth, string? scopeFilterJson)
    {
        _logger.LogInformation("BhytGenerateXmlJob: start exportId={Id}", exportId);
        using var conn = (IDbConnection)_db.CreateConnection();

        try
        {
            var result = await _generator.GenerateAsync(exportId, tenantId, periodMonth, scopeFilterJson,
                CancellationToken.None);

            if (!result.Success)
            {
                var errMsg = result.ErrorMessage ?? "Unknown error";
                _logger.LogWarning("BhytGenerateXmlJob: generation failed exportId={Id}: {Err}", exportId, errMsg);

                await conn.ExecuteAsync(
                    "UPDATE diab_his_int_bhyt_exports SET status='DRAFT', response_message=@msg, updated_at=NOW() WHERE id=@id",
                    new { id = exportId, msg = errMsg });
                return;
            }

            // Xoa items cu (regenerate case)
            await conn.ExecuteAsync(
                "DELETE FROM diab_his_int_bhyt_export_items WHERE export_id=@id", new { id = exportId });

            // Bulk insert items
            foreach (var item in result.Items)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_int_bhyt_export_items
                      (tenant_id, export_id, table_no, record_index, row_data_json,
                       source_encounter_id, source_billing_id, ma_lien_ket,
                       request_amount, generated_at, created_at, updated_at)
                      VALUES (@tid, @eid, @tn, @ri, @rdj, @seid, @sbid, @mlk, @ra, NOW(), NOW(), NOW())",
                    new
                    {
                        tid = tenantId,
                        eid = exportId,
                        tn = item.TableNo,
                        ri = item.RecordIndex,
                        rdj = item.RowDataJson,
                        seid = item.SourceEncounterId,
                        sbid = item.SourceBillingId,
                        mlk = item.MaLienKet,
                        ra = item.RequestAmount
                    });
            }

            // Sinh file XML that (Bang 1-5 theo QD 3176) va luu vao object storage
            var tenantCode = await conn.ExecuteScalarAsync<string>(
                "SELECT IFNULL(NULLIF(code, ''), CAST(id AS CHAR)) FROM diab_his_sys_tenants WHERE id=@t",
                new { t = tenantId }) ?? tenantId.ToString();

            var xml = _serializer.Serialize(exportId, tenantCode, periodMonth, result.Items);
            var objectKey = $"{tenantId}/{exportId}/bang_all.xml";
            await using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                await _storage.UploadAsync(FileBuckets.BhytExports, objectKey, ms, "application/xml");
            }

            // Update export: status=GENERATED
            await conn.ExecuteAsync(
                @"UPDATE diab_his_int_bhyt_exports
                  SET status='GENERATED', generated_at=NOW(),
                      encounter_count=@ec, total_requested_amount=@tra,
                      xml_file_path=@xfp, updated_at=NOW()
                  WHERE id=@id",
                new { id = exportId, ec = result.EncounterCount, tra = result.TotalRequestedAmount, xfp = objectKey });

            _logger.LogInformation("BhytGenerateXmlJob: done exportId={Id}, encounters={Ec}, items={Items}, xmlPath={Path}",
                exportId, result.EncounterCount, result.Items.Count, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytGenerateXmlJob: unhandled error exportId={Id}", exportId);
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_bhyt_exports SET status='DRAFT', response_message=@msg, updated_at=NOW() WHERE id=@id",
                new { id = exportId, msg = $"Job error: {ex.Message}" });
            throw;  // Hangfire will retry
        }
    }
}
