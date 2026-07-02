using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities.Bhyt;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: parse file ket qua doi soat BHYT trong background.
/// Update diab_his_int_bhyt_reconcile_items voi approved/rejected amounts.
/// Match items bang ma_lien_ket.
/// </summary>
public class BhytReconcileParseJob
{
    private readonly IBhytReconcileParser _parser;
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BhytReconcileParseJob> _logger;

    public BhytReconcileParseJob(IBhytReconcileParser parser, IDapperConnectionFactory db,
        ILogger<BhytReconcileParseJob> logger)
    {
        _parser = parser; _db = db; _logger = logger;
    }

    [Hangfire.Queue("bhyt")]
    public async Task ExecuteAsync(string uploadId, int exportId, int tenantId, string filePath)
    {
        _logger.LogInformation("BhytReconcileParseJob: start uploadId={Id}", uploadId);
        using var conn = (IDbConnection)_db.CreateConnection();

        // Mark PARSING
        await conn.ExecuteAsync(
            "UPDATE diab_his_int_bhyt_reconcile_uploads SET parse_status='PARSING', updated_at=NOW() WHERE id=@id",
            new { id = uploadId });

        try
        {
            var result = await _parser.ParseAsync(filePath, CancellationToken.None);

            if (!result.Success)
            {
                await conn.ExecuteAsync(
                    "UPDATE diab_his_int_bhyt_reconcile_uploads SET parse_status='FAILED', parse_error=@err, updated_at=NOW() WHERE id=@id",
                    new { id = uploadId, err = result.ErrorMessage });
                return;
            }

            // Upsert reconcile items, match by ma_lien_ket
            foreach (var item in result.Items)
            {
                // Tim export_item_id tuong ung
                var exportItemId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT id FROM diab_his_int_bhyt_export_items WHERE export_id=@eid AND ma_lien_ket=@mlk AND table_no=@tn LIMIT 1",
                    new { eid = exportId, mlk = item.MaLienKet, tn = item.TableNo });

                var existingId = await conn.ExecuteScalarAsync<string?>(
                    "SELECT id FROM diab_his_int_bhyt_reconcile_items WHERE upload_id=@uid AND ma_lien_ket=@mlk AND table_no=@tn LIMIT 1",
                    new { uid = uploadId, mlk = item.MaLienKet, tn = item.TableNo });

                if (existingId != null)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE diab_his_int_bhyt_reconcile_items
                          SET approved_amount=@aa, rejected_amount=@rja, status=@st,
                              rejection_code=@rc, rejection_reason=@rr, updated_at=NOW()
                          WHERE id=@id",
                        new { id = existingId, aa = item.ApprovedAmount, rja = item.RejectedAmount,
                              st = item.Status, rc = item.RejectionCode, rr = item.RejectionReason });
                }
                else
                {
                    var newId = Guid.NewGuid().ToString();
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_int_bhyt_reconcile_items
                          (id, tenant_id, upload_id, export_id, export_item_id, table_no, ma_lien_ket,
                           request_amount, approved_amount, rejected_amount, status,
                           rejection_code, rejection_reason, created_at, updated_at)
                          VALUES (@id, @t, @uid, @eid, @eiid, @tn, @mlk,
                                  @ra, @aa, @rja, @st, @rc, @rr, NOW(), NOW())",
                        new { id = newId, t = tenantId, uid = uploadId, eid = exportId,
                              eiid = exportItemId, tn = item.TableNo, mlk = item.MaLienKet,
                              ra = item.RequestAmount, aa = item.ApprovedAmount,
                              rja = item.RejectedAmount, st = item.Status,
                              rc = item.RejectionCode, rr = item.RejectionReason });
                }

                // Update export_item approved_amount
                if (exportItemId.HasValue)
                {
                    await conn.ExecuteAsync(
                        "UPDATE diab_his_int_bhyt_export_items SET approved_amount=@aa, rejection_code=@rc, rejection_reason=@rr WHERE id=@id",
                        new { aa = item.ApprovedAmount, rc = item.RejectionCode, rr = item.RejectionReason, id = exportItemId });
                }
            }

            // Tinh tong va update export
            var totals = await conn.QueryFirstAsync<dynamic>(
                @"SELECT COALESCE(SUM(approved_amount),0) as approved, COALESCE(SUM(rejected_amount),0) as rejected
                  FROM diab_his_int_bhyt_reconcile_items WHERE export_id=@eid AND tenant_id=@t",
                new { eid = exportId, t = tenantId });

            // Xac dinh status moi cho export
            var itemCounts = await conn.QueryFirstAsync<dynamic>(
                @"SELECT
                    SUM(CASE WHEN status='REJECTED' THEN 1 ELSE 0 END) as rejected_count,
                    COUNT(*) as total_count
                  FROM diab_his_int_bhyt_reconcile_items WHERE export_id=@eid AND tenant_id=@t",
                new { eid = exportId, t = tenantId });

            int rejectedCount = (int)(itemCounts.rejected_count ?? 0);
            int totalCount = (int)(itemCounts.total_count ?? 1);

            var newExportStatus = rejectedCount == 0
                ? BhytExportStatus.Approved
                : rejectedCount == totalCount
                    ? BhytExportStatus.Rejected
                    : BhytExportStatus.PartiallyRejected;

            await conn.ExecuteAsync(
                @"UPDATE diab_his_int_bhyt_exports
                  SET status=@st, total_approved_amount=@aa, total_rejected_amount=@rja,
                      response_at=NOW(), updated_at=NOW()
                  WHERE id=@eid",
                new { st = newExportStatus, aa = (decimal)totals.approved, rja = (decimal)totals.rejected, eid = exportId });

            await conn.ExecuteAsync(
                "UPDATE diab_his_int_bhyt_reconcile_uploads SET parse_status='PARSED', parsed_at=NOW(), updated_at=NOW() WHERE id=@id",
                new { id = uploadId });

            _logger.LogInformation("BhytReconcileParseJob: done uploadId={Id}, {Count} items", uploadId, result.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytReconcileParseJob: error uploadId={Id}", uploadId);
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_bhyt_reconcile_uploads SET parse_status='FAILED', parse_error=@err, updated_at=NOW() WHERE id=@id",
                new { id = uploadId, err = ex.Message });
            throw;
        }
    }
}
