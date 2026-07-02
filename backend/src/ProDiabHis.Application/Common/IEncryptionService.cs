namespace ProDiabHis.Application.Common;

/// <summary>Dich vu ma hoa / giai ma AES-256-GCM cho du lieu nhay cam</summary>
public interface IEncryptionService
{
    /// <summary>Ma hoa plaintext, tra ve chuoi base64 chua nonce+tag+ciphertext</summary>
    string Encrypt(string plaintext);

    /// <summary>Giai ma chuoi da ma hoa, tra ve plaintext goc</summary>
    string Decrypt(string ciphertext);
}
