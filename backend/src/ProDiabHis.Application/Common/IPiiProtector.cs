namespace ProDiabHis.Application.Common;

/// <summary>Loai truong PII — dung lam domain separation cho blind index HMAC</summary>
public enum PiiField
{
    /// <summary>So dien thoai</summary>
    Phone,
    /// <summary>CMND / CCCD</summary>
    IdNumber,
    /// <summary>So the BHYT</summary>
    InsuranceCardNo
}

/// <summary>
/// Bao ve du lieu ca nhan (PII) khi luu tru:
///  - Protect/Unprotect: AES-256-GCM (qua IEncryptionService), co tien to danh dau "enc:v1:"
///  - BlindIndex: HMAC-SHA256 co khoa (khoa RIENG, khac khoa ma hoa) de tra cuu exact-match
/// </summary>
public interface IPiiProtector
{
    /// <summary>Ma hoa gia tri. Neu gia tri da duoc ma hoa roi thi tra ve nguyen ven (idempotent).</summary>
    string? Protect(string? plaintext);

    /// <summary>Giai ma. Neu gia tri KHONG mang tien to ma hoa (du lieu cu chua backfill) thi tra ve nguyen ven.</summary>
    string? Unprotect(string? stored);

    /// <summary>True neu chuoi da o dang ciphertext cua he thong (dung cho backfill idempotent).</summary>
    bool IsProtected(string? stored);

    /// <summary>
    /// Blind index HMAC-SHA256 (hex 64 ky tu) tren gia tri DA CHUAN HOA theo loai truong.
    /// Tra ve null neu gia tri rong sau chuan hoa.
    /// </summary>
    string? BlindIndex(string? plaintext, PiiField field);
}

/// <summary>Chuan hoa gia tri PII truoc khi bam blind index (bat buoc de tra cuu on dinh)</summary>
public static class PiiNormalizer
{
    /// <summary>Chuan hoa theo loai truong</summary>
    public static string? Normalize(string? value, PiiField field) => field switch
    {
        PiiField.Phone => NormalizePhone(value),
        PiiField.IdNumber => NormalizeDigitsOrUpper(value),
        PiiField.InsuranceCardNo => NormalizeDigitsOrUpper(value),
        _ => Compact(value)
    };

    /// <summary>
    /// So dien thoai VN ve dang chuan quoc gia (bat dau bang 0, chi con chu so).
    /// "+84 912-345.678", "0084912345678", "84912345678", " 0912 345 678 " => "0912345678"
    /// </summary>
    public static string? NormalizePhone(string? value)
    {
        var digits = KeepDigits(value);
        if (string.IsNullOrEmpty(digits)) return null;

        if (digits.StartsWith("0084")) digits = "0" + digits[4..];
        else if (digits.StartsWith("84") && digits.Length >= 11) digits = "0" + digits[2..];
        else if (!digits.StartsWith("0")) digits = "0" + digits;

        return digits;
    }

    /// <summary>Bo khoang trang / gach noi / cham, in hoa (CMND, so the BHYT co ky tu chu)</summary>
    public static string? NormalizeDigitsOrUpper(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string? Compact(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? KeepDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}

/// <summary>
/// Ambient accessor cho IPiiProtector.
/// Ly do ton tai: rat nhieu read-path dung Dapper raw SQL (PDF, portal, recall, report) doc
/// truc tiep cot *_enc; neu bat buoc inject qua constructor se phai sua hang chuc handler.
/// Duoc set 1 lan luc startup (Program.cs) va trong unit test.
/// Khi CHUA set: Protect/Unprotect la pass-through (khong ma hoa) — bao dam test/tooling
/// khong cau hinh khoa van chay duoc, va KHONG bao gio lam hong du lieu.
/// </summary>
public static class PiiCrypto
{
    private static IPiiProtector? _current;

    /// <summary>Gan implementation (goi 1 lan luc khoi dong ung dung)</summary>
    public static void Configure(IPiiProtector protector) => _current = protector;

    /// <summary>Implementation hien tai (null neu chua cau hinh)</summary>
    public static IPiiProtector? Current => _current;

    /// <summary>Ma hoa (pass-through neu chua cau hinh)</summary>
    public static string? Protect(string? plaintext) => _current is null ? plaintext : _current.Protect(plaintext);

    /// <summary>Giai ma (pass-through neu chua cau hinh hoac du lieu chua ma hoa)</summary>
    public static string? Unprotect(string? stored) => _current is null ? stored : _current.Unprotect(stored);

    /// <summary>Blind index (null neu chua cau hinh)</summary>
    public static string? BlindIndex(string? plaintext, PiiField field) => _current?.BlindIndex(plaintext, field);
}
