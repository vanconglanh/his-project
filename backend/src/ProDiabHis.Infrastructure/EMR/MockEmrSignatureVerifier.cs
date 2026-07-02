using Microsoft.Extensions.Logging;
using ProDiabHis.Application.EMR;

namespace ProDiabHis.Infrastructure.EMR;

/// <summary>
/// Sprint 3-4 MOCK: accepts any signature, logs warning.
/// Sprint 8+: replace with BouncyCastle PKCS#7 verify.
/// </summary>
public class MockEmrSignatureVerifier : IEmrSignatureVerifier
{
    private readonly ILogger<MockEmrSignatureVerifier> _logger;

    public MockEmrSignatureVerifier(ILogger<MockEmrSignatureVerifier> logger) => _logger = logger;

    public Task<EmrSignatureVerifyResult> VerifyAsync(
        byte[] contentBytes,
        byte[] signatureBytes,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[EMR_SIGN_MOCK] Sprint 3-4 mock verifier — accepting signature without cryptographic check. "
            + "Content={ContentLen}B, Sig={SigLen}B",
            contentBytes.Length, signatureBytes.Length);

        // Mock: derive fake cert info from signature bytes for auditability
        var serial = $"MOCK-{Convert.ToHexString(signatureBytes.Take(8).ToArray())}";
        return Task.FromResult(new EmrSignatureVerifyResult(
            IsValid: true,
            CertificateSerial: serial,
            CertificateSubject: "CN=MOCK_CERT,O=Pro-Diab-HIS,C=VN",
            Algorithm: "SHA256withRSA",
            ErrorMessage: null));
    }
}
