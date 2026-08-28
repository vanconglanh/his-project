using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Adapter chuyển tiếp IUsbTokenSigner -> IDigitalSignatureProvider (FR-302/FR-402).
/// Provider cụ thể (Mock/VnptSmartCa) được chọn qua DI theo config "SignatureProvider:Type".
/// Thay thế MockUsbTokenSigner cũ — hành vi mock giữ nguyên khi cấu hình = Mock.
/// </summary>
public class UsbTokenSignerAdapter : IUsbTokenSigner
{
    private readonly IDigitalSignatureProvider _provider;
    private readonly ILogger<UsbTokenSignerAdapter> _logger;

    public UsbTokenSignerAdapter(IDigitalSignatureProvider provider, ILogger<UsbTokenSignerAdapter> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<SignatureVerifyResult> VerifyAsync(
        string base64Signature, string certificateThumbprint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Signature))
            return new SignatureVerifyResult(false, null, null, "Signature data is empty.");

        byte[] sigBytes;
        try
        {
            sigBytes = Convert.FromBase64String(base64Signature);
        }
        catch
        {
            return new SignatureVerifyResult(false, null, null, "Invalid base64 encoding.");
        }

        // IUsbTokenSigner hien tai khong nhan document hash rieng (chu ky client-side gui len).
        // Tam thoi dung hash cua chinh chu ky de tra cuu/kiem tra qua provider - gioi han ke thua
        // tu luong hien co. Khi tich hop CA that can bo sung document hash vao request ky don thuoc.
        var hash = SHA256.HashData(sigBytes);
        var result = await _provider.VerifySignatureAsync(hash, sigBytes, certificateThumbprint, ct);

        if (!result.IsValid)
        {
            _logger.LogWarning("[RX_SIGN] Xac thuc chu ky don thuoc that bai: {Reason}", result.Reason);
            return new SignatureVerifyResult(false, null, null, result.Reason ?? "Chu ky khong hop le.");
        }

        return new SignatureVerifyResult(true, result.CertificateSerial, result.CertificateSubject);
    }
}
