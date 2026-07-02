using Microsoft.Extensions.Logging;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Infrastructure.Sms;

/// <summary>Development SMS gateway — chi ghi log, khong gui that</summary>
public class MockSmsGateway : ISmsGateway
{
    private readonly ILogger<MockSmsGateway> _logger;

    public MockSmsGateway(ILogger<MockSmsGateway> logger) => _logger = logger;

    public Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK SMS] To: {Phone} | Message: {Message}", phoneE164, message);
        return Task.CompletedTask;
    }
}

/// <summary>SpeedSMS gateway stub (HTTP POST)</summary>
public class SpeedSmsGateway : ISmsGateway
{
    private readonly ILogger<SpeedSmsGateway> _logger;
    public SpeedSmsGateway(ILogger<SpeedSmsGateway> logger) => _logger = logger;

    public Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SpeedSMS STUB] To: {Phone}", phoneE164);
        return Task.CompletedTask;
    }
}

/// <summary>Viettel SMS gateway stub</summary>
public class ViettelSmsGateway : ISmsGateway
{
    private readonly ILogger<ViettelSmsGateway> _logger;
    public ViettelSmsGateway(ILogger<ViettelSmsGateway> logger) => _logger = logger;

    public Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ViettelSMS STUB] To: {Phone}", phoneE164);
        return Task.CompletedTask;
    }
}

/// <summary>eSMS gateway stub</summary>
public class EsmsGateway : ISmsGateway
{
    private readonly ILogger<EsmsGateway> _logger;
    public EsmsGateway(ILogger<EsmsGateway> logger) => _logger = logger;

    public Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[eSMS STUB] To: {Phone}", phoneE164);
        return Task.CompletedTask;
    }
}
