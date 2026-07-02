using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// PKCS#7 detached signature tren XML BHYT.
/// Dev: dung self-signed cert tu cert store (hoac tao moi).
/// Prod: doi chung thu so HSM / USB token.
/// </summary>
public class BhytSignerImpl : IBhytSigner
{
    private readonly ILogger<BhytSignerImpl> _logger;

    public BhytSignerImpl(ILogger<BhytSignerImpl> logger) => _logger = logger;

    public async Task<BhytSignResult> SignAsync(
        int exportId, string? certThumbprint, string? pin, CancellationToken ct)
    {
        _logger.LogInformation("BhytSigner: signing exportId={Id}", exportId);

        try
        {
            // Dev: tao signature mock bang RSA tu dong
            using var rsa = RSA.Create(2048);
            var certRequest = new CertificateRequest(
                "CN=BHYT-Dev-Sign",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            using var cert = certRequest.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

            // Tao payload gia lap (thuc te se doc file XML tu MinIO)
            var payload = Encoding.UTF8.GetBytes($"<BhytXml exportId=\"{exportId}\" signedAt=\"{DateTime.UtcNow:O}\"/>");

            var contentInfo = new ContentInfo(payload);
            var signedCms = new SignedCms(contentInfo, detached: true);
            var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
            signer.IncludeOption = X509IncludeOption.EndCertOnly;
            signedCms.ComputeSignature(signer);

            // Luu sig (dev: chi log, khong luu MinIO that)
            var sigPath = $"bhyt/signed/{exportId}/bang_all.xml.p7s";
            _logger.LogInformation("BhytSigner: exportId={Id} signed OK, sigPath={Path}", exportId, sigPath);

            return new BhytSignResult(true, sigPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytSigner: exportId={Id} failed", exportId);
            return new BhytSignResult(false, null, ex.Message);
        }
    }
}
