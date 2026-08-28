using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.EMR;

namespace ProDiabHis.Infrastructure.EMR;

/// <summary>
/// Adapter chuyển tiếp IEmrSignatureVerifier -> IDigitalSignatureProvider (FR-302/FR-402).
/// Provider cụ thể (Mock/VnptSmartCa) được chọn qua DI theo config "SignatureProvider:Type".
/// Thay thế MockEmrSignatureVerifier cũ (đã hardcode luôn accept) — hành vi mock được giữ nguyên
/// khi cấu hình = Mock, nhờ MockDigitalSignatureProvider.
/// </summary>
public class EmrSignatureVerifierAdapter : IEmrSignatureVerifier
{
    private readonly IDigitalSignatureProvider _provider;
    private readonly ILogger<EmrSignatureVerifierAdapter> _logger;

    public EmrSignatureVerifierAdapter(IDigitalSignatureProvider provider, ILogger<EmrSignatureVerifierAdapter> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<EmrSignatureVerifyResult> VerifyAsync(
        byte[] contentBytes,
        byte[] signatureBytes,
        CancellationToken ct = default)
    {
        var hash = SHA256.HashData(contentBytes);

        // Chưa có thông tin certificateSerial ở bước verify EMR (client chỉ gửi chữ ký) —
        // provider tự suy ra/tra cứu serial từ chữ ký nếu cần. Đây là hạn chế kế thừa từ luồng
        // hiện có (Sprint 3-4); khi tích hợp CA thật cần bổ sung serial vào SignEmrRequest.
        var result = await _provider.VerifySignatureAsync(hash, signatureBytes, certificateSerial: null, ct);

        if (!result.IsValid)
        {
            _logger.LogWarning("[EMR_SIGN] Xac thuc chu ky that bai: {Reason}", result.Reason);
        }

        return new EmrSignatureVerifyResult(
            IsValid: result.IsValid,
            CertificateSerial: result.CertificateSerial,
            CertificateSubject: result.CertificateSubject,
            Algorithm: result.Algorithm,
            ErrorMessage: result.Reason);
    }
}
