using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using ProDiabHis.Api.Filters;
using ProDiabHis.Infrastructure.Persistence;
using StackExchange.Redis;

namespace ProDiabHis.Api.Controllers;

/// <summary>Kiem tra trang thai he thong</summary>
[ApiController]
[Route("api/v1/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMinioClient? _minio;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        AppDbContext db,
        ILogger<HealthController> logger,
        IConfiguration configuration,
        IConnectionMultiplexer? redis = null,
        IMinioClient? minio = null)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _redis = redis;
        _minio = minio;
    }

    /// <summary>Kiem tra suc khoe he thong co ban</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        var dbStatus = "OK";
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            dbStatus = "FAIL";
        }

        var status = dbStatus == "OK" ? "Healthy" : "Unhealthy";
        var statusCode = dbStatus == "OK"
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        var result = new
        {
            data = new
            {
                status,
                db = dbStatus,
                redis = "N/A",
                timestamp = DateTime.UtcNow
            },
            meta = new { }
        };

        return StatusCode(statusCode, result);
    }

    /// <summary>Kiem tra suc khoe chi tiet (admin only)</summary>
    [HttpGet("detailed")]
    [Authorize]
    [RequirePermission("system.config")]
    public async Task<IActionResult> Detailed(CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, object>();

        // Database latency
        var dbStart = DateTime.UtcNow;
        string dbStatus;
        long dbLatencyMs = -1;
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            dbLatencyMs = (long)(DateTime.UtcNow - dbStart).TotalMilliseconds;
            dbStatus = "OK";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB detailed health check failed");
            dbStatus = "FAIL";
        }
        checks["database"] = new { status = dbStatus, latency_ms = dbLatencyMs };

        // Redis ping
        string redisStatus = "N/A";
        long redisLatencyMs = -1;
        if (_redis != null)
        {
            try
            {
                var redisStart = DateTime.UtcNow;
                var db = _redis.GetDatabase();
                await db.PingAsync();
                redisLatencyMs = (long)(DateTime.UtcNow - redisStart).TotalMilliseconds;
                redisStatus = _redis.IsConnected ? "OK" : "DEGRADED";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                redisStatus = "FAIL";
            }
        }
        checks["redis"] = new { status = redisStatus, latency_ms = redisLatencyMs };

        // MinIO bucket exist
        string minioStatus = "N/A";
        if (_minio != null)
        {
            try
            {
                var bucketName = _configuration["Minio:BucketName"] ?? "prodiab";
                var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken);
                minioStatus = exists ? "OK" : "BUCKET_MISSING";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO health check failed");
                minioStatus = "FAIL";
            }
        }
        checks["minio"] = new { status = minioStatus };

        // Hangfire queue depth
        long hangfireEnqueued = -1;
        string hangfireStatus = "N/A";
        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            var stats = monitoring.GetStatistics();
            hangfireEnqueued = stats.Enqueued;
            hangfireStatus = hangfireEnqueued < 1000 ? "OK" : "DEGRADED";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire health check failed");
            hangfireStatus = "FAIL";
        }
        checks["hangfire"] = new { status = hangfireStatus, enqueued = hangfireEnqueued };

        // Disk space
        string diskStatus = "N/A";
        long diskFreeGb = -1;
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "/");
            diskFreeGb = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            diskStatus = diskFreeGb > 1 ? "OK" : "LOW";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk health check failed");
            diskStatus = "FAIL";
        }
        checks["disk"] = new { status = diskStatus, free_gb = diskFreeGb };

        var overallOk = checks.Values.All(c =>
        {
            var status = c.GetType().GetProperty("status")?.GetValue(c)?.ToString();
            return status is "OK" or "N/A";
        });

        return StatusCode(
            overallOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            new
            {
                data = new
                {
                    status = overallOk ? "Healthy" : "Degraded",
                    checks,
                    timestamp = DateTime.UtcNow
                },
                meta = new { }
            });
    }
}
