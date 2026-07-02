using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>Mock IDrugCucQldSync - stub trả empty list, enqueue Hangfire job UUID.</summary>
public class MockDrugCucQldSync : IDrugCucQldSync
{
    private readonly ILogger<MockDrugCucQldSync> _logger;

    public MockDrugCucQldSync(ILogger<MockDrugCucQldSync> logger) { _logger = logger; }

    public Task<Guid> EnqueueSyncJobAsync(string mode, DateTime? since, CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid();
        _logger.LogInformation("[MOCK] CucQLD sync enqueued: mode={Mode} since={Since} jobId={JobId}", mode, since, jobId);
        return Task.FromResult(jobId);
    }
}

/// <summary>Mock ICucQldLienThong - GPP reporting stub.</summary>
public class MockCucQldLienThong : ICucQldLienThong
{
    private readonly ILogger<MockCucQldLienThong> _logger;

    public MockCucQldLienThong(ILogger<MockCucQldLienThong> logger) { _logger = logger; }

    public Task ReportImportAsync(Guid grnId, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] CucQLD bao nhap kho: grnId={GrnId}", grnId);
        return Task.CompletedTask;
    }

    public Task ReportExportAsync(Guid dispenseRecordId, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] CucQLD bao xuat kho: dispenseId={DispenseId}", dispenseRecordId);
        return Task.CompletedTask;
    }
}
