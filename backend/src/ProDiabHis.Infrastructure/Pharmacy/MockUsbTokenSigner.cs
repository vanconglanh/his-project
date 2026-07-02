using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Dev/Test mock implementation of IUsbTokenSigner.
/// Accepts any non-empty base64 signature and returns a fake PKCS#7 serial.
/// Production: client-side signs, BE receives and verifies cert chain.
/// </summary>
public class MockUsbTokenSigner : IUsbTokenSigner
{
    private readonly ILogger<MockUsbTokenSigner> _logger;

    public MockUsbTokenSigner(ILogger<MockUsbTokenSigner> logger)
    {
        _logger = logger;
    }

    public Task<SignatureVerifyResult> VerifyAsync(string base64Signature, string certificateThumbprint, CancellationToken ct = default)
    {
        _logger.LogWarning("[DEV] MockUsbTokenSigner: accepting signature without real PKCS#7 verification. DO NOT use in production.");

        if (string.IsNullOrWhiteSpace(base64Signature))
        {
            return Task.FromResult(new SignatureVerifyResult(false, null, null, "Signature data is empty."));
        }

        try
        {
            _ = Convert.FromBase64String(base64Signature); // validate base64
        }
        catch
        {
            return Task.FromResult(new SignatureVerifyResult(false, null, null, "Invalid base64 encoding."));
        }

        var fakeSerial = $"MOCK-{Guid.NewGuid():N}"[..20];
        return Task.FromResult(new SignatureVerifyResult(true, fakeSerial, $"CN=MockDoctor, O=ProDiab, C=VN"));
    }
}
