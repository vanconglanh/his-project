using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: retry DTQG submission with exponential backoff.
/// Schedule: 1m, 5m, 30m, 2h, 12h (max 5 retries).
/// </summary>
public class DtqgSubmitRetryJob
{
    private static readonly int[] BackoffMinutes = [1, 5, 30, 120, 720];

    private readonly IDapperConnectionFactory _db;
    private readonly IDtqgClient _dtqgClient;
    private readonly ILogger<DtqgSubmitRetryJob> _logger;

    public DtqgSubmitRetryJob(IDapperConnectionFactory db, IDtqgClient dtqgClient, ILogger<DtqgSubmitRetryJob> logger)
    {
        _db = db;
        _dtqgClient = dtqgClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(string submissionId)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, tenant_id, prescription_id, retry_count, status FROM diab_his_int_dtqg_submissions WHERE id = @id",
            new { id = submissionId });

        if (sub == null)
        {
            _logger.LogWarning("DtqgSubmitRetryJob: submission {Id} not found", submissionId);
            return;
        }

        int retryCount = (int)sub.retry_count;
        string status = (string)sub.status;

        if (status == "ACCEPTED" || retryCount >= 5)
        {
            _logger.LogInformation("DtqgSubmitRetryJob: submission {Id} already terminal (status={Status}, retry={Retry})", submissionId, status, retryCount);
            return;
        }

        _logger.LogInformation("DtqgSubmitRetryJob: retrying submission {Id} (attempt {Attempt})", submissionId, retryCount + 1);

        var payload = new DtqgSubmitPayload((int)sub.tenant_id, (int)sub.prescription_id, "", "", new { });
        var result = await _dtqgClient.SubmitPrescriptionAsync(payload, CancellationToken.None);

        if (result.Success && result.MaDonThuoc?.Length == 14)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_dtqg_submissions SET status = 'ACCEPTED', ma_don_thuoc = @code, accepted_at = NOW(), retry_count = @rc, updated_at = NOW() WHERE id = @id",
                new { code = result.MaDonThuoc, rc = retryCount + 1, id = submissionId });

            await conn.ExecuteAsync(
                "UPDATE pha_prescriptions SET dtqg_code = @code, dtqg_status = 'ACCEPTED', status = 'SUBMITTED_DTQG', UPDATED_AT = NOW() WHERE ID = @presId",
                new { code = result.MaDonThuoc, presId = (int)sub.prescription_id });

            _logger.LogInformation("DtqgSubmitRetryJob: submission {Id} accepted, ma_don_thuoc={Code}", submissionId, result.MaDonThuoc);
        }
        else
        {
            var nextRetry = retryCount + 1;
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_dtqg_submissions SET retry_count = @rc, last_retry_at = NOW(), status = 'REJECTED', error_code = @ec, error_message = @em, updated_at = NOW() WHERE id = @id",
                new { rc = nextRetry, ec = result.ErrorCode, em = result.ErrorMessage, id = submissionId });

            // Schedule next retry if under limit
            if (nextRetry < 5)
            {
                var delayMinutes = BackoffMinutes[Math.Min(nextRetry, BackoffMinutes.Length - 1)];
                _logger.LogWarning("DtqgSubmitRetryJob: submission {Id} failed attempt {Attempt}, next retry in {Delay}m", submissionId, nextRetry, delayMinutes);

                // Hangfire schedules next job
                Hangfire.BackgroundJob.Schedule<DtqgSubmitRetryJob>(
                    job => job.ExecuteAsync(submissionId),
                    TimeSpan.FromMinutes(delayMinutes));
            }
            else
            {
                _logger.LogError("DtqgSubmitRetryJob: submission {Id} exceeded max retries (5). Giving up.", submissionId);
            }
        }
    }
}
