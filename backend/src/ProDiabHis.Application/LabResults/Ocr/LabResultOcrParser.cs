using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProDiabHis.Application.LabResults.Ocr;

/// <summary>
/// Parser thuan (khong phu thuoc thu vien doc file) — trich gia tri xet nghiem tu chuoi text da
/// OCR/doc san. Tach rieng khoi viec doc file de unit test bang chuoi mau, khong can file that.
///
/// Khac InBody: KHONG co danh sach chi so co dinh. Dau vao la danh sach XN DANG CHO KET QUA cua
/// dung 1 lan chi dinh (pendingTests). Voi moi XN, tim trong text OCR mot doan gan ten/ma XN do
/// roi lay so + don vi di kem. Khong tim thay -> Extracted=false (KHONG throw), UI hien "Chua doc
/// duoc" va khong chan cac field con lai.
///
/// Chien luoc khop nhan (accent-insensitive): chuan hoa ca text lan nhan ve khong dau + lowercase
/// truoc khi so khop, vi phieu KQ tieng Viet hay co dau ("Duong huyet", "Ure mau"...).
/// </summary>
public static class LabResultOcrParser
{
    // So sau nhan: cho phep &lt;=25 ky tu rac (dau ":", "=", "Ket qua", khoang trang...) giua nhan va so,
    // khong vuot qua 1 xuong dong (de tranh vo lay so cua dong ke tiep). Bat them 1 token don vi phia sau.
    private const string GapNumberUnit =
        @"[^0-9\r\n]{0,25}(-?\d+(?:[.,]\d+)?)\s*([%a-zµ][a-zµ0-9/\.\^\-]{0,14})?";

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Tu dien alias theo MA xet nghiem thuong quy (uppercase) -> cac cach viet co the gap tren phieu
    /// (da chuan hoa khong dau, lowercase). Giup bat duoc khi phieu dung ten khac voi TestName trong he thong.
    /// </summary>
    private static readonly Dictionary<string, string[]> CodeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GLU"]     = new[] { "glucose", "duong huyet", "duong mau", "glu" },
        ["GLUCOSE"] = new[] { "glucose", "duong huyet", "duong mau" },
        ["HBA1C"]   = new[] { "hba1c", "hb a1c", "hemoglobin a1c", "a1c" },
        ["CHOL"]    = new[] { "cholesterol toan phan", "cholesterol", "chol tp", "chol" },
        ["TG"]      = new[] { "triglyceride", "triglycerid", "trigly", "tg" },
        ["TRIG"]    = new[] { "triglyceride", "triglycerid", "trigly" },
        ["HDL"]     = new[] { "hdl-c", "hdl cholesterol", "hdl" },
        ["LDL"]     = new[] { "ldl-c", "ldl cholesterol", "ldl" },
        ["URE"]     = new[] { "ure mau", "urea", "ure" },
        ["UREA"]    = new[] { "urea", "ure mau", "ure" },
        ["CRE"]     = new[] { "creatinin", "creatinine", "cre" },
        ["CREA"]    = new[] { "creatinin", "creatinine" },
        ["AST"]     = new[] { "ast (got)", "ast/got", "sgot", "got", "ast" },
        ["ALT"]     = new[] { "alt (gpt)", "alt/gpt", "sgpt", "gpt", "alt" },
        ["GGT"]     = new[] { "ggt", "gamma gt" },
        ["TSH"]     = new[] { "tsh" },
        ["FT3"]     = new[] { "ft3", "free t3" },
        ["FT4"]     = new[] { "ft4", "free t4" },
        ["NA"]      = new[] { "natri", "sodium", "na+" },
        ["K"]       = new[] { "kali", "potassium", "k+" },
        ["CL"]      = new[] { "clorua", "chloride", "cl-" },
        ["CRP"]     = new[] { "crp hs", "crp" },
        ["UA"]      = new[] { "acid uric", "uric acid", "acid uric mau" },
    };

    // Don vi hop le hay gap tren phieu XN (chuan hoa lowercase, khong dau). Neu token bat duoc khong
    // nam trong whitelist -> coi nhu khong co don vi (tranh nhan nham chu "ket", "mau"... la don vi).
    private static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "%", "mmol/l", "mg/dl", "g/l", "g/dl", "u/l", "iu/l", "ui/l", "mu/l", "uiu/ml", "µu/ml",
        "ng/ml", "pg/ml", "ng/dl", "umol/l", "µmol/l", "mmol", "mg/l", "meq/l", "fl", "pg",
        "10^9/l", "10^12/l", "x10^9/l", "x10^12/l", "t/l", "g/dl.", "mg%", "u/ml"
    };

    public static LabOcrParseResult Parse(string? rawText, IEnumerable<LabOcrPendingTest> pendingTests)
    {
        var text = rawText ?? string.Empty;
        var normText = Normalize(text);
        var fields = new List<LabOcrFieldResult>();

        foreach (var t in pendingTests)
        {
            var (value, valueNum, unit) = TryExtract(normText, t);
            fields.Add(new LabOcrFieldResult(
                t.LabOrderItemId, t.TestCode, t.TestName,
                value, valueNum, unit, value is not null));
        }

        return new LabOcrParseResult(text, fields);
    }

    private static (string? Raw, decimal? Num, string? Unit) TryExtract(string normText, LabOcrPendingTest t)
    {
        foreach (var label in BuildLabelCandidates(t))
        {
            try
            {
                // Chi chan CHU CAI 2 dau nhan (khong chan chu so): phieu KQ hay dan lien nhan voi so
                // ("HbA1c8.10", "6.4HbA1c") — phai cho phep chu so dan sat. Nhung van chan chu cai de
                // "glu" khong dinh vao "glucose", "ldl" khong dinh "aldl".
                var pattern = $@"(?<![a-z]){Regex.Escape(label)}(?![a-z]){GapNumberUnit}";
                var m = Regex.Match(normText, pattern, RegexOptions.IgnoreCase, RegexTimeout);
                if (!m.Success) continue;

                var raw = m.Groups[1].Value;
                var numStr = raw.Replace(',', '.');
                decimal? num = decimal.TryParse(numStr, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null;

                var unitToken = m.Groups[2].Success ? m.Groups[2].Value.Trim().TrimEnd('.') : null;
                var unit = !string.IsNullOrEmpty(unitToken) && KnownUnits.Contains(unitToken) ? unitToken : null;

                return (raw, num, unit);
            }
            catch (RegexMatchTimeoutException)
            {
                // Khong de 1 nhan loi lam hong toan bo qua trinh trich xuat
                continue;
            }
        }

        return (null, null, null);
    }

    /// <summary>
    /// Sinh danh sach nhan ung vien cho 1 XN, uu tien nhan DAI/CU THE truoc (giam nham lan). Gom:
    /// alias theo ma, ten XN he thong (chuan hoa), ma XN (chuan hoa). Loc trung + rong.
    /// </summary>
    private static IEnumerable<string> BuildLabelCandidates(LabOcrPendingTest t)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(t.TestCode) && CodeAliases.TryGetValue(t.TestCode.Trim(), out var aliases))
            candidates.AddRange(aliases);

        var normName = Normalize(t.TestName);
        if (normName.Length >= 2) candidates.Add(normName);

        var normCode = Normalize(t.TestCode);
        if (normCode.Length >= 2) candidates.Add(normCode);

        return candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderByDescending(c => c.Length);
    }

    /// <summary>Chuan hoa: bo dau tieng Viet, đ->d, lowercase, gom khoang trang. Giu lai ky tu don vi (%,/).</summary>
    private static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch switch { 'đ' or 'Đ' => 'd', _ => char.ToLowerInvariant(ch) });
        }

        // Gom moi cum khoang trang (gom xuong dong lien tiep giu lai 1 \n de gioi han "khong qua 1 dong")
        var collapsed = Regex.Replace(sb.ToString(), @"[ \t\f\v]+", " ");
        return collapsed;
    }
}
