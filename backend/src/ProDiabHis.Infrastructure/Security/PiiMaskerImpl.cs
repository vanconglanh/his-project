using System.Text.RegularExpressions;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Mask du lieu PII truoc khi tra ra ngoai cho user khong co quyen pii.read.full.
/// Pattern: giu dau + cuoi, che phan giua bang ***.
/// </summary>
public class PiiMaskerImpl : IPiiMasker
{
    public string MaskNationalId(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "***";
        if (value.Length <= 4) return "***";

        // Giu 2 dau + 2 cuoi: 079***12
        var prefix = value[..2];
        var suffix = value[^2..];
        return $"{prefix}***{suffix}";
    }

    public string MaskBhyt(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "***";
        if (value.Length <= 6) return "***";

        // BHYT format: DN12345678901234 — giu 2 prefix + 4 suffix
        var prefix = value[..2];
        var suffix = value[^4..];
        return $"{prefix}***{suffix}";
    }

    public string MaskPhone(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "***";

        // Chuan hoa: bo dau + va spaces
        var digits = Regex.Replace(value, @"[^\d]", "");
        if (digits.Length <= 5) return "***";

        // Giu 3 dau + 2 cuoi: 098***45
        var prefix = digits[..3];
        var suffix = digits[^2..];
        return $"{prefix}***{suffix}";
    }

    public string MaskEmail(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "***";

        var atIndex = value.IndexOf('@');
        if (atIndex <= 0) return "***";

        var localPart = value[..atIndex];
        var domain = value[atIndex..]; // includes @

        if (localPart.Length <= 2) return $"***{domain}";

        // Giu 2 ky tu dau cua local part
        return $"{localPart[..2]}***{domain}";
    }

    public string MaskFullName(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "***";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return $"{parts[0][0]}.";

        // Giu ho + ten dem + chu cai dau cua ten
        var lastName = parts[0];
        var firstName = parts[^1];
        var middle = parts.Length > 2
            ? string.Join(" ", parts[1..^1]) + " "
            : "";

        return $"{lastName} {middle}{firstName[0]}.";
    }
}
