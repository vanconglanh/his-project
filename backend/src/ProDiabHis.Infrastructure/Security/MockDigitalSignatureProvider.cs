using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Dev/Test mock implementation của IDigitalSignatureProvider.
/// Không gọi CA thật — chỉ giả lập luồng remote-signing để không chặn phát triển
/// khi chưa có hợp đồng/sandbox VNPT SmartCA hoặc Viettel-CA.
/// DÙNG CHO Development/Testing — KHÔNG dùng ở Production.
/// </summary>
public class MockDigitalSignatureProvider : IDigitalSignatureProvider
{
    private readonly ILogger<MockDigitalSignatureProvider> _logger;

    public MockDigitalSignatureProvider(ILogger<MockDigitalSignatureProvider> logger) => _logger = logger;

    public Task<SignCertificateInfo> GetCertificateAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogWarning("[SIGN_MOCK] GetCertificateAsync - tra ve chung thu so gia lap cho userId={UserId}", userId);

        var serial = $"MOCK-{Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(userId)).PadLeft(8, '0')[..8]}";
        return Task.FromResult(new SignCertificateInfo(
            Serial: serial,
            Issuer: "CN=Pro-Diab-HIS Mock CA,O=Pro-Diab-HIS,C=VN",
            ValidFrom: DateTime.UtcNow.AddYears(-1),
            ValidTo: DateTime.UtcNow.AddYears(1),
            SubjectName: $"CN=MockUser-{userId},O=Pro-Diab-HIS,C=VN",
            IsActive: true));
    }

    public Task<SignResult> SignDocumentHashAsync(
        string userId, byte[] documentHash, string documentType, string documentId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[SIGN_MOCK] SignDocumentHashAsync - ky gia lap, KHONG dung PKI that. "
            + "userId={UserId}, documentType={DocumentType}, documentId={DocumentId}",
            userId, documentType, documentId);

        // Chữ ký giả lập: HMAC-đơn giản trên hash + userId (không dùng khoá riêng tư thật)
        var fakeSignature = System.Security.Cryptography.SHA256.HashData(
            documentHash.Concat(System.Text.Encoding.UTF8.GetBytes(userId)).ToArray());
        var serial = $"MOCK-{Convert.ToHexString(fakeSignature.Take(8).ToArray())}";

        return Task.FromResult(new SignResult(
            Success: true,
            Signature: fakeSignature,
            Timestamp: DateTime.UtcNow,
            CertificateSerial: serial));
    }

    public Task<VerifyResult> VerifySignatureAsync(
        byte[] documentHash, byte[] signature, string? certificateSerial, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[SIGN_MOCK] VerifySignatureAsync - chap nhan chu ky khong kiem tra mat ma. "
            + "Hash={HashLen}B, Sig={SigLen}B", documentHash.Length, signature.Length);

        if (signature.Length == 0)
            return Task.FromResult(new VerifyResult(false, "Chữ ký rỗng."));

        var serial = certificateSerial ?? $"MOCK-{Convert.ToHexString(signature.Take(8).ToArray())}";
        return Task.FromResult(new VerifyResult(
            IsValid: true,
            Reason: null,
            CertificateSerial: serial,
            CertificateSubject: "CN=MOCK_CERT,O=Pro-Diab-HIS,C=VN",
            Algorithm: "SHA256withRSA"));
    }
}
