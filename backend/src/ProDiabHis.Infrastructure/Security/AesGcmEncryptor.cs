using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>Ma hoa AES-256-GCM. Key doc tu appsettings Encryption:MasterKey (32 bytes base64)</summary>
public class AesGcmEncryptor : IEncryptionService
{
    private readonly byte[] _key;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public AesGcmEncryptor(IConfiguration configuration)
    {
        var masterKey = configuration["Encryption:MasterKey"]
            ?? throw new InvalidOperationException("Encryption:MasterKey chua duoc cau hinh");

        _key = Convert.FromBase64String(masterKey);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:MasterKey phai la 32 bytes (256 bit)");
    }

    /// <summary>Ma hoa plaintext. Dinh dang tra ve: base64(nonce[12] + tag[16] + ciphertext)</summary>
    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertextBytes, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertextBytes.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertextBytes.CopyTo(result, NonceSizeBytes + TagSizeBytes);

        return Convert.ToBase64String(result);
    }

    /// <summary>Giai ma chuoi da ma hoa bang Encrypt()</summary>
    public string Decrypt(string encryptedBase64)
    {
        var data = Convert.FromBase64String(encryptedBase64);
        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Du lieu ma hoa khong hop le");

        var nonce = data[..NonceSizeBytes];
        var tag = data[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var ciphertext = data[(NonceSizeBytes + TagSizeBytes)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
