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
                    // "63/100 Points"
                    var sc = Regex.Match(text, @"(\d{1,3})\s*/\s*100", RegexOptions.IgnoreCase, RxTimeout);
                    if (sc.Success && TryParse(sc.Groups[1].Value, out var scv)
                        && !InBodyPlausibleRanges.Evaluate(indicatorType, scv).OutOfRange)
                        return scv;
                    break;
            }
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static bool TryParse(string raw, out decimal value) =>
        decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out value);
}
