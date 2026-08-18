using ProDiabHis.Application.Common;

namespace ProDiabHis.UnitTests;

/// <summary>
/// Fake IPiiProtector cho unit test.
/// Khoa HMAC o day la khoa TEST-ONLY (khong phai secret production) — khoa that lay tu
/// bien moi truong Encryption:BlindIndexKey.
/// </summary>
public class FakePiiProtector : IPiiProtector
{
    public const string Marker = "enc:v1:";
    private static readonly byte[] TestKey = System.Text.Encoding.UTF8.GetBytes("unit-test-blind-index-key-32bytes!!");
    private readonly IEncryptionService _enc = new FakeEncryptionService();

    public string? Protect(string? plaintext)
        => string.IsNullOrEmpty(plaintext) || IsProtected(plaintext) ? plaintext : Marker + _enc.Encrypt(plaintext);

    public string? Unprotect(string? stored)
        => string.IsNullOrEmpty(stored) || !IsProtected(stored) ? stored : _enc.Decrypt(stored[Marker.Length..]);

    public bool IsProtected(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Marker, StringComparison.Ordinal);

    public string? BlindIndex(string? plaintext, PiiField field)
    {
        var normalized = PiiNormalizer.Normalize(plaintext, field);
        if (string.IsNullOrEmpty(normalized)) return null;
        var hash = System.Security.Cryptography.HMACSHA256.HashData(
            TestKey, System.Text.Encoding.UTF8.GetBytes($"{field}:{normalized}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>Fake IEncryptionService cho unit test (encode/decode Base64 don gian)</summary>
public class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plaintext) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
    public string Decrypt(string ciphertext) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
}
