using System.Security.Cryptography;
using System.Text;

namespace ProDiabHis.Application.Auth.Sso;

public static class SsoReturnUrl
{
    /// <summary>Mac dinh sau khi dang nhap (backend tu chen /sso/complete khi redirect)</summary>
    public const string Default = "/sso/complete?returnUrl=/dashboard";

    /// <summary>Chi chap nhan path noi bo (co the kem query) bat dau "/", khong "//", khong "/\".
    /// Sai -> Default. Chong open redirect.</summary>
    public static string Sanitize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return Default;
        var u = returnUrl.Trim();
        if (u.Length > 2000 || u[0] != '/') return Default;
        if (u.StartsWith("//", StringComparison.Ordinal) || u.StartsWith("/\\", StringComparison.Ordinal)) return Default;
        if (u.Contains('\\')) return Default;
        foreach (var c in u)
            if (char.IsControl(c)) return Default;
        return u;
    }
}

public static class SsoCrypto
{
    public static string RandomUrlSafe(int bytes)
        => Base64Url(RandomNumberGenerator.GetBytes(bytes));

    public static string S256(string verifier)
        => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static string Base64Url(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool FixedEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
