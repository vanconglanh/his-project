using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Web Push message encryption (RFC 8291, content-encoding aes128gcm) + VAPID (RFC 8292).
/// Khong dung package ngoai — chi dung System.Security.Cryptography (ECDH P-256, HKDF, AES-GCM, ECDSA ES256).
/// Da verify bang test vector chinh thuc RFC 8291 muc 5 (xem WebPushCryptoTests).
/// </summary>
public static class WebPushCrypto
{
    // ---------- Base64Url ----------
    public static string B64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] B64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var len = parts.Sum(p => p.Length);
        var buf = new byte[len];
        int o = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, buf, o, p.Length); o += p.Length; }
        return buf;
    }

    // ---------- RFC 8291 payload encryption ----------
    /// <summary>
    /// Ma hoa payload theo RFC 8291. Tra ve body aes128gcm hoan chinh
    /// (salt[16] || rs[4] || idlen[1] || as_public[65] || ciphertext+tag).
    /// asKey + salt duoc truyen vao de test theo vector co dinh; production dung EncryptPayload(...).
    /// </summary>
    public static byte[] EncryptPayloadWith(byte[] uaPublic, byte[] authSecret, byte[] plaintext,
        ECDiffieHellman asKey, byte[] salt)
    {
        byte[] asPublic = ExportRawPublic(asKey);

        using var uaEc = ImportRawPublic(uaPublic);
        byte[] ecdhSecret = asKey.DeriveRawSecretAgreement(uaEc.PublicKey);

        // IKM = HKDF(salt=auth_secret, ikm=ecdh, info="WebPush: info"||0x00||ua_public||as_public, L=32)
        byte[] keyInfo = Concat(Encoding.ASCII.GetBytes("WebPush: info"), new byte[] { 0x00 }, uaPublic, asPublic);
        byte[] ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, ecdhSecret, 32, authSecret, keyInfo);

        // RFC 8188 aes128gcm: CEK + NONCE (salt = record salt)
        byte[] cek = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt,
            Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        byte[] nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt,
            Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));

        // content = plaintext || 0x02 (padding delimiter cho record cuoi/duy nhat)
        byte[] content = new byte[plaintext.Length + 1];
        Buffer.BlockCopy(plaintext, 0, content, 0, plaintext.Length);
        content[plaintext.Length] = 0x02;

        byte[] cipher = new byte[content.Length];
        byte[] tag = new byte[16];
        using (var gcm = new AesGcm(cek, 16))
            gcm.Encrypt(nonce, content, cipher, tag);

        uint rs = 4096; // record size (RFC 8188)
        using var ms = new MemoryStream();
        ms.Write(salt, 0, salt.Length);
        ms.Write(new[] { (byte)(rs >> 24), (byte)(rs >> 16), (byte)(rs >> 8), (byte)rs }, 0, 4);
        ms.WriteByte((byte)asPublic.Length); // 65
        ms.Write(asPublic, 0, asPublic.Length);
        ms.Write(cipher, 0, cipher.Length);
        ms.Write(tag, 0, tag.Length);
        return ms.ToArray();
    }

    /// <summary>Production: sinh ephemeral ECDH keypair + salt ngau nhien.</summary>
    public static byte[] EncryptPayload(byte[] uaPublic, byte[] authSecret, byte[] plaintext)
    {
        using var asKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        return EncryptPayloadWith(uaPublic, authSecret, plaintext, asKey, salt);
    }

    // ---------- RFC 8292 VAPID ----------
    /// <summary>Tao header Authorization: "vapid t=&lt;jwt&gt;, k=&lt;public&gt;" cho endpoint.</summary>
    public static string BuildVapidAuthHeader(string endpoint, string subject,
        byte[] vapidPublicRaw, ECDsa vapidSigningKey, DateTimeOffset now)
    {
        var uri = new Uri(endpoint);
        string aud = uri.GetLeftPart(UriPartial.Authority); // origin: scheme://host[:port]

        string header = B64Url(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"ES256\"}"));
        long exp = now.AddHours(12).ToUnixTimeSeconds();
        string payload = B64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["aud"] = aud, ["exp"] = exp, ["sub"] = subject
        }));

        string signingInput = header + "." + payload;
        byte[] sig = vapidSigningKey.SignData(Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string token = signingInput + "." + B64Url(sig);

        return $"vapid t={token}, k={B64Url(vapidPublicRaw)}";
    }

    // ---------- ECDH helpers ----------
    private static ECDiffieHellman ImportRawPublic(byte[] raw65)
    {
        var ec = ECDiffieHellman.Create();
        ec.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = raw65[1..33], Y = raw65[33..65] }
        });
        return ec;
    }

    private static byte[] ExportRawPublic(ECDiffieHellman key)
    {
        var q = key.ExportParameters(false).Q;
        return Concat(new byte[] { 0x04 }, q.X!, q.Y!);
    }

    /// <summary>Import ECDH key tu private (D) + public raw — dung cho test vector co dinh.</summary>
    public static ECDiffieHellman ImportEcdhKey(byte[] privateD, byte[] publicRaw65)
    {
        var ec = ECDiffieHellman.Create();
        ec.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = privateD,
            Q = new ECPoint { X = publicRaw65[1..33], Y = publicRaw65[33..65] }
        });
        return ec;
    }

    /// <summary>Chuyen SubjectPublicKeyInfo (DER) -&gt; raw 65 byte uncompressed point.</summary>
    public static byte[] SpkiToRawPublic(byte[] spki)
    {
        using var ec = ECDsa.Create();
        ec.ImportSubjectPublicKeyInfo(spki, out _);
        var q = ec.ExportParameters(false).Q;
        return Concat(new byte[] { 0x04 }, q.X!, q.Y!);
    }
}
