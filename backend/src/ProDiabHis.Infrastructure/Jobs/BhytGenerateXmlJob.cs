using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: generate XML Bang 1-5 cho ky export BHYT.
/// Long-running (toi 30 phut cho period nhieu BN).
/// Sau khi xong: UPDATE status = GENERATED + luu items.
/// </summary>
public class BhytGenerateXmlJob
{
    private readonly IBhytXmlGenerator _generator;
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BhytGenerateXmlJob> _logger;

    public BhytGenerateXmlJob(IBhytXmlGenerator generator, IDapperConnectionFactory db,
        ILogger<BhytGenerateXmlJob> logger)
    {
        _generator = generator; _db = db; _logger = logger;
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

            // Update export: status=GENERATED
            await conn.ExecuteAsync(
                @"UPDATE diab_his_int_bhyt_exports
                  SET status='GENERATED', generated_at=NOW(),
                      encounter_count=@ec, total_requested_amount=@tra,
                      updated_at=NOW()
                  WHERE id=@id",
                new { id = exportId, ec = result.EncounterCount, tra = result.TotalRequestedAmount });

            _logger.LogInformation("BhytGenerateXmlJob: done exportId={Id}, encounters={Ec}, items={Items}",
                exportId, result.EncounterCount, result.Items.Count);
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
