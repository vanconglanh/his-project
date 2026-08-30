using System.Globalization;
using System.Text.RegularExpressions;

namespace ProDiabHis.Application.InBody;

/// <summary>
/// Parser thuan (khong phu thuoc thu vien doc PDF) — trich cac chi so InBody tu chuoi text da
/// duoc trich san (vd tu PdfPig text layer). Tach rieng khoi viec doc file de de unit test bang
/// chuoi mau, khong can file PDF that.
///
/// Chien luoc: tim theo LABEL (khong theo toa do co dinh, vi layout khac nhau giua cac model
/// InBody 270/370/570/770), lay so gan nhat theo sau label trong pham vi hop ly. Neu khong tim
/// thay label hoac khong parse duoc so -> field do Extracted=false, Value=null (KHONG throw).
/// </summary>
public static class InBodyReportParser
{
    // Thu tu quan trong: label dai/cu the hon phai dat truoc de tranh nham (vd "Percent Body Fat"
    // truoc "Body Fat Mass" khong xung dot vi khac tu, nhung "SMM" ngan nen de sau ten day du).
    private static readonly (string IndicatorType, string LabelPattern, string? Unit)[] LabelDefs =
    {
        (InBodyIndicatorTypes.Weight,      @"Weight",                                  "kg"),
        (InBodyIndicatorTypes.Smm,         @"Skeletal\s*Muscle\s*Mass|SMM",             "kg"),
        (InBodyIndicatorTypes.BodyFatMass, @"Body\s*Fat\s*Mass",                        "kg"),
        (InBodyIndicatorTypes.Pbf,         @"Percent\s*Body\s*Fat|PBF",                 "%"),
        (InBodyIndicatorTypes.Bmi,         @"BMI",                                      "kg/m2"),
        (InBodyIndicatorTypes.VisceralFat, @"Visceral\s*Fat\s*Level",                   null),
        (InBodyIndicatorTypes.Tbw,         @"Total\s*Body\s*Water|TBW",                 "L"),
        (InBodyIndicatorTypes.Bmr,         @"Basal\s*Metabolic\s*Rate|BMR",             "kcal"),
        (InBodyIndicatorTypes.InBodyScore, @"InBody\s*Score",                           null),
    };

    /// <summary>So sau label, cho phep don vi/ky tu rac giua label va so (toi da 30 ky tu, khong qua 1 dong trong).</summary>
    private const string GapAndNumber = @"[^0-9\-\r\n]{0,30}(-?\d+(?:[.,]\d+)?)";

    public static InBodyReportData Parse(string? rawText)
    {
        var text = rawText ?? string.Empty;
        var fields = new List<InBodyFieldResult>();

        foreach (var (indicatorType, labelPattern, unit) in LabelDefs)
        {
            var value = TryExtractValue(text, labelPattern);
            fields.Add(new InBodyFieldResult(indicatorType, value, unit, value.HasValue));
        }

        return new InBodyReportData(text, fields);
    }

    private static decimal? TryExtractValue(string text, string labelPattern)
    {
        try
        {
            var pattern = $@"(?:{labelPattern})\b{GapAndNumber}";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var raw = match.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            // Khong de mot label loi lam hong toan bo qua trinh trich xuat
            return null;
        }
    }
}
