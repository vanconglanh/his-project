namespace ProDiabHis.Application.EMR;

/// <summary>
/// Verifies a PKCS#7 detached digital signature on EMR content.
/// Sprint 3-4: Mock implementation (always accepts, logs warning).
/// Sprint 8+: BouncyCastle PKCS#7 verify against CA chain.
/// </summary>
public interface IEmrSignatureVerifier
{
    /// <summary>Returns true if signature is valid. SignerInfo populated on success.</summary>
    Task<EmrSignatureVerifyResult> VerifyAsync(
        byte[] contentBytes,
        byte[] signatureBytes,
        CancellationToken ct = default);
}

public record EmrSignatureVerifyResult(
    bool IsValid,
    string? CertificateSerial,
    string? CertificateSubject,
    string? Algorithm,
    string? ErrorMessage);
