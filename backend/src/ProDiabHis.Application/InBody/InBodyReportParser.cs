using System.Globalization;
using System.Text.RegularExpressions;

namespace ProDiabHis.Application.InBody;

/// <summary>
/// Parser thuan (khong phu thuoc thu vien doc PDF) — trich cac chi so InBody tu chuoi text da
/// duoc trich san (text layer PdfPig HOAC OCR Tesseract tu anh scan). Tach rieng khoi viec doc
/// file de de unit test bang chuoi mau, khong can file PDF that.
///
/// Chien luoc (BM-InBody OCR): tim theo LABEL (khong theo toa do co dinh, vi layout khac nhau
/// giua cac model InBody 270/370/570/770), lay so NGAY SAU label (truoc cot "(range)"), tren
/// CUNG MOT DONG. Uu tien PRECISION: gia tri ngoai khoang vat ly kha di (xem
/// <see cref="InBodyPlausibleRanges"/>) bi BO QUA de thu match ke tiep; neu khong co match hop ly
/// -> de TRONG (Extracted=false) cho nguoi dung nhap tay, KHONG dien sai.
///
/// Xu ly dac thu phieu InBody270 SCAN roi OCR:
///  - So in kieu may co KHOANG TRANG sau dau thap phan: "76. 6", "3. 25", "28. 4" -> "76.6"...
///  - Dau phay thap phan "28,4"/"28, 4" -> "28.4".
///  - Loi OCR trong NGU CANH SO: chu 'O'/'o' -> '0', "()" -> '0' (khi ke sat chu so).
///  - Don vi tach roi "(kg)", "(L)", "(%)" nam giua label va so -> gap cho phep bo qua.
///  - Gia tri do nam TRUOC cot range "( 49.2~66.6 )" -> chi lay so dau sau label, khong lan range.
///  - Bo muc "Body Composition History" o cuoi phieu truoc khi map, de khong lay nham so lan do cu.
///  - Anchor rieng: Visceral Fat Level "Level N", InBody Score "N / 100".
///
/// LUU Y: KHONG ap dung sua 'B'->'8' (nhap nhang: B co the la 5/6/8) — tung gay doc sai BMR
/// (13B3 -> 1383 trong khi dung la 1353). Uu tien de trong hon la dien sai chi so y te.
/// </summary>
public static class InBodyReportParser
{
    private static readonly (string IndicatorType, string LabelPattern, string? Unit)[] LabelDefs =
    {
        (InBodyIndicatorTypes.Weight,      @"Weight",                                  "kg"),
        (InBodyIndicatorTypes.Smm,         @"Skeletal\s*Muscle\s*Mass|SMM",             "kg"),
        (InBodyIndicatorTypes.BodyFatMass, @"Body\s*Fat\s*Mass",                        "kg"),
        (InBodyIndicatorTypes.Pbf,         @"Percent\s*Body\s*Fat|PBF",                 "%"),
        (InBodyIndicatorTypes.Bmi,         @"Body\s*Mass\s*Index|BMI",                  "kg/m2"),
        (InBodyIndicatorTypes.VisceralFat, @"Visceral\s*Fat\s*Level",                   null),
        (InBodyIndicatorTypes.Tbw,         @"Total\s*Body\s*Water|TBW",                 "L"),
        (InBodyIndicatorTypes.Bmr,         @"Basal\s*Metabolic\s*Rate|BMR",             "kcal"),
        (InBodyIndicatorTypes.InBodyScore, @"InBody\s*Score",                           null),
    };

    /// <summary>
    /// Khoang tu label den so + rang buoc do sach de chong lay nham khi OCR nhieu:
    ///  - GAP: toi da 18 ky tu, KHONG chua chu so / xuong dong / '~' / '|' (khong lan cot range,
    ///    khong vuot dong, khong qua dau phan cot '|').
    ///  - So KHONG duoc dinh ngay sau '(' hoac chu so/dau cham (vd don vi "(9", "(%)", "(0@)" bi OCR
    ///    doc thanh so) -> negative lookbehind.
    ///  - So phai KET THUC "sach": theo sau la khoang trang / % / ngoac / chu (don vi) / '~' / het
    ///    dong — KHONG duoc theo sau boi '|' hay '.' lac (vd garble "3|.|" cua so 31.1 bi loai).
    ///  - So KHONG duoc theo sau ngay boi 1 so khac (vd vach thang do "70 80 90 100..." o cac bar
    ///    Muscle-Fat/Obesity) -> tranh lay nham gia tri dau tien cua thang do.
    /// </summary>
    private const string GapAndNumber =
        @"[^0-9\r\n~]{0,18}(?<![(\d.])(\d+(?:\.\d+)?)(?=[\s%)(A-Za-z~]|$)(?![\s]*\d)";

    private static readonly TimeSpan RxTimeout = TimeSpan.FromSeconds(2);

    public static InBodyReportData Parse(string? rawText)
    {
        var text = Normalize(rawText ?? string.Empty);
        var mainText = StripHistorySection(text);
        var fields = new List<InBodyFieldResult>();

        foreach (var (indicatorType, labelPattern, unit) in LabelDefs)
        {
            var value = TryExtractValue(mainText, labelPattern, indicatorType)
                        ?? TryExtractByAnchor(text, indicatorType);
            fields.Add(new InBodyFieldResult(indicatorType, value, unit, value.HasValue));
        }

        // Fallback CO KIEM CHEO (chi dien khi dang tin cay) cho 3 chi so quan trong SMM/PBF/BMI,
        // vi o cac bar/callout thuong bi OCR lam hong.
        ApplyCrossCheckedFallbacks(text, fields);

        return new InBodyReportData(text, fields);
    }

    /// <summary>Chuan hoa text OCR truoc khi map (xem chi tiet o doc class).</summary>
    internal static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        try
        {
            // Loi OCR trong ngu canh so: 'O'/'o' -> '0' (chu O ke sat chu so), "()" ke sat so -> '0'
            text = Regex.Replace(text, @"(?<=\d)\s*\(\)", "0", RegexOptions.None, RxTimeout);
            text = Regex.Replace(text, @"(?<=\d)[Oo](?=[\d.\s])", "0", RegexOptions.None, RxTimeout);
            text = Regex.Replace(text, @"(?<=[\s.])[Oo](?=\d)", "0", RegexOptions.None, RxTimeout);
            // "28, 4" / "28,4" -> "28.4" (phay thap phan giua 2 chu so)
            text = Regex.Replace(text, @"(?<=\d),\s*(?=\d)", ".", RegexOptions.None, RxTimeout);
            // "76. 6" -> "76.6" (cham thap phan co khoang trang theo sau)
            text = Regex.Replace(text, @"(?<=\d)\.\s+(?=\d)", ".", RegexOptions.None, RxTimeout);
        }
        catch (RegexMatchTimeoutException) { /* giu nguyen text goc neu regex timeout */ }
        return text;
    }

    /// <summary>Bo muc lich su o cuoi phieu de khong lay nham chi so cua lan do truoc.</summary>
    private static string StripHistorySection(string text)
    {
        var idx = text.IndexOf("Body Composition History", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? text[..idx] : text;
    }

    private static decimal? TryExtractValue(string text, string labelPattern, string indicatorType)
    {
        try
        {
            var pattern = $@"(?:{labelPattern})\b{GapAndNumber}";
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase, RxTimeout))
            {
                if (!TryParse(match.Groups[1].Value, out var value)) continue;
                // Uu tien precision: bo qua gia tri ngoai khoang vat ly kha di (vd checkbox "1",
                // "0" do OCR rac) -> thu match ke tiep thay vi dien sai.
                var (outOfRange, _) = InBodyPlausibleRanges.Evaluate(indicatorType, value);
                if (outOfRange) continue;
                return value;
            }
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    /// <summary>Anchor rieng cho mot so chi so khi layout khong cho so ngay sau label.</summary>
    private static decimal? TryExtractByAnchor(string text, string indicatorType)
    {
        try
        {
            switch (indicatorType)
            {
                case InBodyIndicatorTypes.VisceralFat:
                    // "Visceral Fat Level" roi dong duoi "Level 15"
                    foreach (Match m in Regex.Matches(text, @"Level\s+(\d{1,2})\b", RegexOptions.IgnoreCase, RxTimeout))
                        if (TryParse(m.Groups[1].Value, out var v) && !InBodyPlausibleRanges.Evaluate(indicatorType, v).OutOfRange)
                            return v;
                    break;
                case InBodyIndicatorTypes.InBodyScore:
                    // "63/100 Points" — tren phieu InBody chi co 1 cho "/100" (diem). Nhan "InBody
                    // Score" va gia tri thuong bi OCR tach xa (doc cheo cot) nen KHONG neo theo nhan.
                    // Gop chu so bi OCR tach boi khoang trang (vd "6 3/100" -> 63).
                    var sc = Regex.Match(text,
                        @"(\d[\d\s]{0,2}\d|\d)\s*/\s*100\b",
                        RegexOptions.IgnoreCase, RxTimeout);
                    if (sc.Success)
                    {
                        var digits = Regex.Replace(sc.Groups[1].Value, @"\s+", "");
                        if (TryParse(digits, out var scv)
                            && !InBodyPlausibleRanges.Evaluate(indicatorType, scv).OutOfRange)
                            return scv;
                    }
                    break;
            }
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    // ───────────────────────────── Fallback co kiem cheo ─────────────────────────────

    /// <summary>
    /// Dien 3 chi so SMM/PBF/BMI bang nguon du phong CHI KHI qua kiem cheo (uu tien precision):
    ///  - BMI: tinh ky vong = Weight / (Height/100)^2 roi chon so ung vien gan ky vong nhat trong
    ///    vung Obesity Analysis (sai lech <= 1.5) -> loai vach thang do, chi dien khi khop cong thuc.
    ///  - SMM/PBF: lay cot MOI NHAT (so cuoi) o bang "Body Composition History" — chi khi dong do
    ///    OCR du tot (>= 3 so) VA so Weight moi nhat cua history KHOP voi Weight hien tai (xac nhan
    ///    day la lan do hien tai). Khong khop -> de trong.
    /// </summary>
    private static void ApplyCrossCheckedFallbacks(string text, List<InBodyFieldResult> fields)
    {
        var weight = Val(fields, InBodyIndicatorTypes.Weight);

        // BMI qua cong thuc + kiem cheo
        if (!Has(fields, InBodyIndicatorTypes.Bmi) && weight is > 0)
        {
            var heightCm = ExtractHeightCm(text);
            if (heightCm is >= 80 and <= 230)
            {
                var m = heightCm.Value / 100m;
                var expected = weight.Value / (m * m);
                var cand = BmiCandidates(text)
                    .Select(c => (c, diff: Math.Abs(c - expected)))
                    .OrderBy(x => x.diff)
                    .FirstOrDefault(x => x.diff <= 1.5m);
                if (cand.c > 0) SetField(fields, InBodyIndicatorTypes.Bmi, cand.c);
            }
        }

        // SMM/PBF qua cot moi nhat cua history (gated)
        var (histWeight, histSmm, histPbf) = ParseHistoryLastColumn(text);
        if (weight is > 0 && histWeight is > 0 && Math.Abs(histWeight.Value - weight.Value) <= Math.Max(0.6m, weight.Value * 0.01m))
        {
            if (!Has(fields, InBodyIndicatorTypes.Smm) && histSmm is > 0
                && !InBodyPlausibleRanges.Evaluate(InBodyIndicatorTypes.Smm, histSmm).OutOfRange)
                SetField(fields, InBodyIndicatorTypes.Smm, histSmm.Value);
            if (!Has(fields, InBodyIndicatorTypes.Pbf) && histPbf is > 0
                && !InBodyPlausibleRanges.Evaluate(InBodyIndicatorTypes.Pbf, histPbf).OutOfRange)
                SetField(fields, InBodyIndicatorTypes.Pbf, histPbf.Value);
        }
    }

    private static decimal? ExtractHeightCm(string text)
    {
        try
        {
            // "Height ... 164. lem/cm" (OCR '1'->'l'). Bat so 100..229 di kem cm/lem/lom/em.
            var m = Regex.Match(text, @"\b(1[0-9]{2}|2[0-2][0-9])\b[^\n\d]{0,6}(?:cm|[lL][eoc]?m|em)",
                RegexOptions.IgnoreCase, RxTimeout);
            if (m.Success && TryParse(m.Groups[1].Value, out var h)) return h;
        }
        catch (RegexMatchTimeoutException) { }
        return null;
    }

    private static IEnumerable<decimal> BmiCandidates(string text)
    {
        // Lay cac so thap phan 10..60 trong vung tu "Obesity Analysis" den "Segmental"/"Research".
        var region = Slice(text, "Obesity Analysis", new[] { "Segmental", "Research Parameters" });
        var list = new List<decimal>();
        try
        {
            foreach (Match m in Regex.Matches(region, @"\b(\d{2}(?:\.\d)?)\b", RegexOptions.None, RxTimeout))
                if (TryParse(m.Groups[1].Value, out var v) && v is >= 10m and <= 60m)
                    list.Add(v);
        }
        catch (RegexMatchTimeoutException) { }
        return list;
    }

    private static (decimal? Weight, decimal? Smm, decimal? Pbf) ParseHistoryLastColumn(string text)
    {
        var idx = text.IndexOf("Body Composition History", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return (null, null, null);
        var hist = text[idx..];
        return (
            LastNumberOnRow(hist, @"Weight", 3),
            LastNumberOnRow(hist, @"Skeletal\s*Muscle\s*Mass|SMM", 3),
            LastNumberOnRow(hist, @"Percent\s*Body\s*Fat|PBF", 3));
    }

    /// <summary>Lay so CUOI tren dong chua label, chi khi dong do co >= minCount so (OCR du tot).</summary>
    private static decimal? LastNumberOnRow(string text, string labelPattern, int minCount)
    {
        try
        {
            foreach (Match line in Regex.Matches(text, $@"(?:{labelPattern})[^\r\n]*", RegexOptions.IgnoreCase, RxTimeout))
            {
                var nums = Regex.Matches(line.Value, @"\d+(?:\.\d+)?", RegexOptions.None, RxTimeout);
                if (nums.Count >= minCount && TryParse(nums[^1].Value, out var v)) return v;
            }
        }
        catch (RegexMatchTimeoutException) { }
        return null;
    }

    private static string Slice(string text, string start, string[] ends)
    {
        var s = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (s < 0) return text;
        var from = s + start.Length;
        var end = text.Length;
        foreach (var e in ends)
        {
            var i = text.IndexOf(e, from, StringComparison.OrdinalIgnoreCase);
            if (i > 0 && i < end) end = i;
        }
        return text[from..end];
    }

    private static decimal? Val(List<InBodyFieldResult> f, string t) =>
        f.FirstOrDefault(x => x.IndicatorType == t)?.Value;
    private static bool Has(List<InBodyFieldResult> f, string t) =>
        f.Any(x => x.IndicatorType == t && x.Extracted);
    private static void SetField(List<InBodyFieldResult> f, string t, decimal v)
    {
        var i = f.FindIndex(x => x.IndicatorType == t);
        if (i >= 0) f[i] = new InBodyFieldResult(t, v, f[i].Unit, true);
    }

    private static bool TryParse(string raw, out decimal value) =>
        decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out value);
}
