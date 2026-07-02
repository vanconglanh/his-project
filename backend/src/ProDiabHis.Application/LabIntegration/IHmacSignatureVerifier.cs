namespace ProDiabHis.Application.LabIntegration;

/// <summary>Xac thuc HMAC-SHA256 tu header X-Partner-Signature</summary>
public interface IHmacSignatureVerifier
{
    /// <summary>
    /// Tinh HMAC-SHA256(secret, rawBody) va so sanh voi signature tu header.
    /// Tra ve true neu hop le.
    /// </summary>
    bool Verify(string secret, byte[] rawBody, string signature);
}
