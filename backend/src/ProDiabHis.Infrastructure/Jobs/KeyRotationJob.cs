using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>Hangfire recurring job: auto rotate encryption keys qua 365 ngay</summary>
public class KeyRotationJob
{
    private readonly IKeyRotationService _keyRotationService;
    private readonly ILogger<KeyRotationJob> _logger;

    public KeyRotationJob(IKeyRotationService keyRotationService, ILogger<KeyRotationJob> logger)
    {
        _keyRotationService = keyRotationService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[KeyRotationJob] Starting scheduled key rotation check...");
        await _keyRotationService.RotateExpiredKeysAsync(CancellationToken.None);
        _logger.LogInformation("[KeyRotationJob] Done.");
    }
}
