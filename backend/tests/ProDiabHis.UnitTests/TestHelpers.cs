using ProDiabHis.Application.Common;

namespace ProDiabHis.UnitTests;

/// <summary>Fake IEncryptionService cho unit test (encode/decode Base64 don gian)</summary>
public class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plaintext) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
    public string Decrypt(string ciphertext) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
}
