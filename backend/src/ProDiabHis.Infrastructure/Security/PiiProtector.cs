using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Bao ve PII: AES-256-GCM cho ciphertext + HMAC-SHA256 cho blind index.
/// Khoa lay tu cau hinh (bien moi truong), KHONG hardcode:
///   Encryption:MasterKey     -> khoa ma hoa (dung lai IEncryptionService)
///   Encryption:BlindIndexKey -> khoa RIENG cho blind index (base64, >= 32 bytes)
/// Neu thieu BlindIndexKey: blind index bi tat (tra ve null) va he thong van chay,
/// nhung tra cuu theo SDT/CMND/so the se khong hoat dong -> log canh bao o startup.
/// </summary>
public class PiiProtector : IPiiProtector
{
    /// <summary>Tien to danh dau chuoi da ma hoa — dung de backfill idempotent</summary>
    public const string Marker = "enc:v1:";

    private readonly IEncryptionService _enc;
    private readonly byte[]? _blindIndexKey;

    public PiiProtector(IEncryptionService enc, IConfiguration configuration)
    {
        _enc = enc;
        var raw = configuration["Encryption:BlindIndexKey"];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var key = Convert.FromBase64String(raw);
            if (key.Length < 32)
                throw new InvalidOperationException("Encryption:BlindIndexKey phai toi thieu 32 bytes (256 bit)");
            _blindIndexKey = key;
        }
    }

    /// <summary>Co bat blind index hay khong (phuc vu health-check / log startup)</summary>
    public bool BlindIndexEnabled => _blindIndexKey is not null;

    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;
        if (IsProtected(plaintext)) return plaintext; // idempotent
        return Marker + _enc.Encrypt(plaintext);
    }

    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored;
        if (!IsProtected(stored)) return stored; // du lieu cu chua backfill
        return _enc.Decrypt(stored[Marker.Length..]);
    }

    public bool IsProtected(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Marker, StringComparison.Ordinal);

    public string? BlindIndex(string? plaintext, PiiField field)
    {
        if (_blindIndexKey is null) return null;

        var normalized = PiiNormalizer.Normalize(plaintext, field);
        if (string.IsNullOrEmpty(normalized)) return null;

        // Domain separation: cung 1 gia tri o 2 truong khac nhau -> hash khac nhau
        var payload = Encoding.UTF8.GetBytes($"{field}:{normalized}");
        var hash = HMACSHA256.HashData(_blindIndexKey, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
