using Microsoft.Extensions.Logging;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Patients;

/// <summary>
/// Du lieu da parse tu chuoi QR CCCD (chuan 7 field tu 2021).
/// BR-QR-006: parse thuc te chay o phia client (browser) — class nay duoc dung
/// de unit-test logic parse va lam tai lieu tham chieu (mirror voi logic TS phia FE),
/// KHONG duoc goi tu bat ky API endpoint nao.
/// </summary>
public record CccdQrData(
    string? IdNumber,
    string? OldIdNumber,
    string? FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    DateOnly? IssuedDate,
    bool HasEncodingWarning);

public record CccdQrParseResult(bool Success, CccdQrData? Data, string? ErrorCode, string? ErrorMessage);

/// <summary>Parser chuoi QR CCCD dinh dang: soCCCD|soCMNDCu|hoTen|ngaySinh|gioiTinh|diaChi|ngayCap</summary>
public static class CccdQrParser
{
    private const int ExpectedFieldCount = 7;

    public static CccdQrParseResult Parse(string? raw, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new CccdQrParseResult(false, null, "CCCD_QR_EMPTY", "Chuỗi quét rỗng");

        var fields = raw.Split('|');
        if (fields.Length != ExpectedFieldCount)
        {
            logger?.LogWarning(
                "CCCD QR parse loi: so field khong hop le ({Count}/{Expected}), prefix={Prefix}",
                fields.Length, ExpectedFieldCount, SafePrefix(raw));
            return new CccdQrParseResult(false, null, "CCCD_QR_INVALID_FIELD_COUNT",
                $"Số trường không hợp lệ ({fields.Length}/{ExpectedFieldCount} field)");
        }

        // BR-QR-002: tung field xu ly doc lap, khong throw vo luong
        var idNumber = NullIfEmpty(fields[0]);
        var oldIdNumber = NullIfEmpty(fields[1]);
        var fullName = NullIfEmpty(fields[2]);
        var dob = ParseDate(fields[3], "ngaySinh", raw, logger);
        var genderRaw = NullIfEmpty(fields[4]);
        var gender = MapGender(genderRaw);
        var address = NullIfEmpty(fields[5]);
        var issuedDate = ParseDate(fields[6], "ngayCap", raw, logger);

        // BR-QR-005: phat hien ky tu thay the do loi encoding cu
        var hasEncodingWarning = ContainsReplacementChar(fullName) || ContainsReplacementChar(address);

        var data = new CccdQrData(idNumber, oldIdNumber, fullName, dob, gender, address, issuedDate, hasEncodingWarning);
        return new CccdQrParseResult(true, data, null, null);
    }

    private static bool ContainsReplacementChar(string? s) => s != null && s.Contains('�');

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string SafePrefix(string raw) => raw.Length <= 20 ? raw : raw[..20];

    /// <summary>GA-005: gioi tinh QR chi "Nam"/"Nu"; gia tri khac -> de trong</summary>
    private static string? MapGender(string? g)
    {
        if (g is null) return null;
        var trimmed = g.Trim();
        if (trimmed.Equals("Nam", StringComparison.OrdinalIgnoreCase)) return Gender.Male;
        if (trimmed.Equals("Nữ", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Nu", StringComparison.OrdinalIgnoreCase))
            return Gender.Female;
        return null;
    }

    /// <summary>BR-QR-003: ngay phai dung dinh dang ddMMyyyy (8 chu so), khong hop le -> tra ve null</summary>
    private static DateOnly? ParseDate(string field, string fieldName, string raw, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(field) || field.Length != 8 || !field.All(char.IsDigit))
        {
            logger?.LogWarning("CCCD QR parse loi: field {FieldName} khong hop le, prefix={Prefix}",
                fieldName, SafePrefix(raw));
            return null;
        }

        var day = int.Parse(field[..2]);
        var month = int.Parse(field.Substring(2, 2));
        var year = int.Parse(field.Substring(4, 4));
        try
        {
            return new DateOnly(year, month, day);
        }
        catch (Exception)
        {
            logger?.LogWarning("CCCD QR parse loi: field {FieldName} ngay khong hop le, prefix={Prefix}",
                fieldName, SafePrefix(raw));
            return null;
        }
    }
}
