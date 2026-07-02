using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire cron job: daily drug sync with CSDL Duoc Quoc Gia (Cuc QLD).
/// Cron: 0 2 * * * (2:00 AM daily).
/// </summary>
public class CucQldSyncDailyJob
{
    private readonly IDrugCucQldSync _sync;
    private readonly ILogger<CucQldSyncDailyJob> _logger;

    public CucQldSyncDailyJob(IDrugCucQldSync sync, ILogger<CucQldSyncDailyJob> logger)
    {
        _sync = sync;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("CucQldSyncDailyJob: starting INCREMENTAL sync at {Time}", DateTime.Now);
        var jobId = await _sync.EnqueueSyncJobAsync("INCREMENTAL", DateTime.UtcNow.AddDays(-1), CancellationToken.None);
        _logger.LogInformation("CucQldSyncDailyJob: sync job enqueued with id={JobId}", jobId);
    }
}
