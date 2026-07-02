using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Mock client: dev environment — tra ve thanh cong voi ma tham chieu gia.
/// Prod: thay bang HttpClient thuc goi cong.baohiemxahoi.gov.vn
/// </summary>
public class MockBhytSubmissionClient : IBhytSubmissionClient
{
    private readonly ILogger<MockBhytSubmissionClient> _logger;

    public MockBhytSubmissionClient(ILogger<MockBhytSubmissionClient> logger) => _logger = logger;

    public Task<BhytSubmissionResult> SubmitAsync(int exportId, int tenantId, CancellationToken ct)
    {
        var fakeRef = $"BHYT-{tenantId:D4}-{exportId:D6}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        _logger.LogInformation("MockBhytSubmissionClient: exportId={Id} submitted OK, ref={Ref}", exportId, fakeRef);
        return Task.FromResult(new BhytSubmissionResult(true, fakeRef, null));
    }
}
