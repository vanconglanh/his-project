using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — retry cac thao tac Docosan bi loi (outbox pattern, giong
/// DtqgSubmitRetryJob). Cron moi 2 phut. Backoff: 1p, 5p, 15p, 60p, 6h; attempt_count >= 6 -> DEAD.
/// Luu y: Docosan chan spam BLOCK_APT_REQUEST TTL 5s/user -> backoff toi thieu > 5s (da bao dam
/// vi buoc backoff dau la 1 phut).
/// </summary>
public class DocosanOutboxRetryJob
{
    private static readonly int[] BackoffMinutes = [1, 5, 15, 60, 360];
    private const int MaxAttempts = 6;

    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<DocosanOutboxRetryJob> _logger;

    public DocosanOutboxRetryJob(IDapperConnectionFactory db, ILogger<DocosanOutboxRetryJob> logger)
    { _db = db; _logger = logger; }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("DocosanOutboxRetryJob started at {Time}", DateTime.UtcNow);
        using var conn = (IDbConnection)_db.CreateConnection();

        var due = (await conn.QueryAsync<dynamic>(@"
            SELECT * FROM diab_his_int_docosan_outbox
            WHERE deleted_at IS NULL AND status = 'PENDING'
              AND (next_attempt_at IS NULL OR next_attempt_at <= UTC_TIMESTAMP())
              AND attempt_count < @Max
            ORDER BY created_at ASC LIMIT 100", new { Max = MaxAttempts })).ToList();

        _logger.LogInformation("DocosanOutboxRetryJob: {Count} muc can retry", due.Count);

        foreach (var item in due)
        {
            try { await RetryOneAsync(conn, item); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocosanOutboxRetryJob: loi retry muc {Id}", (string)item.id);
            }
        }

        _logger.LogInformation("DocosanOutboxRetryJob finished");
    }

    private async Task RetryOneAsync(IDbConnection conn, dynamic item)
    {
        // NOTE: Xu ly thuc te theo tung 'operation' (CREATE_ORDER|CANCEL|RESCHEDULE|SYNC_DETAIL|REGISTER_USER)
        // se duoc thuc hien boi handler tuong ung khi enqueue that bai (xem TelehealthHandlers.cs).
        // O day chi quan ly vong doi attempt_count/backoff/DEAD cua ban ghi outbox.
        string id = (string)item.id;
        int attemptCount = (int)item.attempt_count;
        var nextAttempt = attemptCount + 1;

        if (nextAttempt >= MaxAttempts)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_docosan_outbox SET status='DEAD', attempt_count=@Ac, updated_at=@Now WHERE id=@Id",
                new { Ac = nextAttempt, Now = DateTime.UtcNow, Id = id });
            _logger.LogError("DocosanOutboxRetryJob: muc {Id} vuot qua {Max} lan retry, chuyen DEAD", id, MaxAttempts);
            return;
        }

        var delay = BackoffMinutes[Math.Min(attemptCount, BackoffMinutes.Length - 1)];
        await conn.ExecuteAsync(@"
            UPDATE diab_his_int_docosan_outbox
            SET attempt_count=@Ac, next_attempt_at=@Next, updated_at=@Now
            WHERE id=@Id",
            new
            {
                Ac = nextAttempt, Next = DateTime.UtcNow.AddMinutes(delay),
                Now = DateTime.UtcNow, Id = id
            });
    }
}
