using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>Moi 5 phut: mark QR da qua han -> EXPIRED</summary>
public class QrExpireJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<QrExpireJob> _logger;

    public QrExpireJob(IDapperConnectionFactory db, ILogger<QrExpireJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            @"UPDATE diab_his_bil_qr_codes
              SET status = 'EXPIRED'
              WHERE status = 'PENDING' AND expires_at < NOW()");
        _logger.LogInformation("QrExpireJob: marked {Count} QR codes as EXPIRED", affected);
    }
}
