using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Mock DTQG HTTP client for dev/test.
/// In production, replace with real HTTP implementation calling donthuocquocgia.vn.
/// </summary>
public class MockDtqgClient : IDtqgClient
{
    private readonly ILogger<MockDtqgClient> _logger;

    public MockDtqgClient(ILogger<MockDtqgClient> logger)
    {
        _logger = logger;
    }

    public Task<DtqgSubmitResult> SubmitPrescriptionAsync(DtqgSubmitPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] DTQG submit for prescription {Id}", payload.PrescriptionId);

        // Generate a fake 14-char ma_don_thuoc
        var maDonThuoc = $"VN{DateTime.Now:yyMMdd}{payload.PrescriptionId:D6}"[..14];
        return Task.FromResult(new DtqgSubmitResult(true, maDonThuoc, null, null));
    }

    public Task<DtqgStatusResult> GetStatusAsync(string maDonThuoc, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] DTQG status for {MaDonThuoc}", maDonThuoc);
        return Task.FromResult(new DtqgStatusResult("ACCEPTED", maDonThuoc, null));
    }

    public Task<bool> CancelAsync(string maDonThuoc, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] DTQG cancel {MaDonThuoc}: {Reason}", maDonThuoc, reason);
        return Task.FromResult(true);
    }

    public Task<DtqgPingResult> PingAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[MOCK] DTQG ping");
        return Task.FromResult(new DtqgPingResult(true, 42, "MOCK_OK"));
    }
}
