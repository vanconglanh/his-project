using System.Security.Cryptography;
using System.Text;
using ProDiabHis.Application.LabIntegration;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>Xac thuc HMAC-SHA256: HMAC(secret, rawBody) == signature</summary>
public class HmacSignatureVerifier : IHmacSignatureVerifier
{
    public bool Verify(string secret, byte[] rawBody, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;

        var secretBytes  = Encoding.UTF8.GetBytes(secret);
        var expectedHash = HMACSHA256.HashData(secretBytes, rawBody);
        var expectedHex  = Convert.ToHexString(expectedHash).ToLowerInvariant();

        // So sanh constant-time
        var sigLower = signature.ToLowerInvariant().Replace("sha256=", "");
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHex),
            Encoding.ASCII.GetBytes(sigLower));
    }
}
