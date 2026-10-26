using ProDiabHis.Application.InBody;
using Xunit;

namespace ProDiabHis.UnitTests.InBody;

public class InBodyReportParserTests
{
    private const string FullSampleText = @"
InBody 770 Result Sheet
Name: Nguyen Van A     ID: 000123     Date: 2026.08.30

Body Composition Analysis
Weight        70.5 kg
Skeletal Muscle Mass   32.1 kg
Body Fat Mass          15.4 kg

Obesity Analysis
BMI           23.1 kg/m2
Percent Body Fat  21.9 %

Segmental / Additional
Visceral Fat Level   8
Total Body Water     41.3 L
Basal Metabolic Rate 1520 kcal

InBody Score 82 points
";

    [Fact]
    public void Parse_FullText_ExtractsAllNineFields()
    {
        var result = InBodyReportParser.Parse(FullSampleText);

        Assert.Equal(9, result.Fields.Count);
        Assert.True(result.IsFullyExtracted);
        Assert.Empty(result.MissingIndicatorTypes);

        Assert.Equal(70.5m, GetValue(result, InBodyIndicatorTypes.Weight));
        Assert.Equal(32.1m, GetValue(result, InBodyIndicatorTypes.Smm));
        Assert.Equal(15.4m, GetValue(result, InBodyIndicatorTypes.BodyFatMass));
        Assert.Equal(23.1m, GetValue(result, InBodyIndicatorTypes.Bmi));
        Assert.Equal(21.9m, GetValue(result, InBodyIndicatorTypes.Pbf));
        Assert.Equal(8m, GetValue(result, InBodyIndicatorTypes.VisceralFat));
        Assert.Equal(41.3m, GetValue(result, InBodyIndicatorTypes.Tbw));
        Assert.Equal(1520m, GetValue(result, InBodyIndicatorTypes.Bmr));
        Assert.Equal(82m, GetValue(result, InBodyIndicatorTypes.InBodyScore));
    }

    [Fact]
    public void Parse_UsingAbbreviations_ExtractsCorrectly()
    {
        // Mo phong layout may doi cu hon dung viet tat thay vi ten day du, xuong dong khac nhau
        const string text = "Weight: 65.2kg\nSMM:28.4kg\nPBF:19.5%\nBMI:22.0\nTBW:38.1L\nBMR:1400kcal\n";

        var result = InBodyReportParser.Parse(text);

        Assert.Equal(65.2m, GetValue(result, InBodyIndicatorTypes.Weight));
        Assert.Equal(28.4m, GetValue(result, InBodyIndicatorTypes.Smm));
        Assert.Equal(19.5m, GetValue(result, InBodyIndicatorTypes.Pbf));
        Assert.Equal(22.0m, GetValue(result, InBodyIndicatorTypes.Bmi));
        Assert.Equal(38.1m, GetValue(result, InBodyIndicatorTypes.Tbw));
        Assert.Equal(1400m, GetValue(result, InBodyIndicatorTypes.Bmr));
    }

    [Fact]
    public void Parse_MissingSomeFields_DoesNotThrow_MarksMissing()
    {
        // Mo phong layout thieu Visceral Fat Level va InBody Score
        const string text = @"
            Weight 80.0 kg
            Skeletal Muscle Mass 35.0 kg
            Body Fat Mass 20.0 kg
            BMI 25.0
            Percent Body Fat 25.0 %
            Total Body Water 45.0 L
            Basal Metabolic Rate 1600 kcal
        ";

        var result = InBodyReportParser.Parse(text);

        Assert.False(result.IsFullyExtracted);
        Assert.True(result.HasAnyExtracted);
        Assert.Contains(InBodyIndicatorTypes.VisceralFat, result.MissingIndicatorTypes);
        Assert.Contains(InBodyIndicatorTypes.InBodyScore, result.MissingIndicatorTypes);
        Assert.Equal(80.0m, GetValue(result, InBodyIndicatorTypes.Weight));
    }

    [Fact]
    public void Parse_EmptyOrScannedImageText_ReturnsAllMissing_NoException()
    {
        var result = InBodyReportParser.Parse(string.Empty);

        Assert.Equal(9, result.Fields.Count);
        Assert.False(result.HasAnyExtracted);
        Assert.False(result.IsFullyExtracted);
        Assert.Equal(9, result.MissingIndicatorTypes.Count);
    }

    [Fact]
    public void Parse_NullText_DoesNotThrow()
    {
        var exception = Record.Exception(() => InBodyReportParser.Parse(null));
        Assert.Null(exception);
    }

    [Fact]
    public void Parse_ScaleGridlineAfterLabel_NotCaptured_LeftEmpty()
    {
        // BM-InBody OCR: o cac bar Muscle-Fat/Obesity, nhan di kem thang do "70 80 90 100..."
        // Parser KHONG duoc lay nham so dau thang do lam gia tri (vd SMM = 70). De TRONG thay vi sai.
        const string text = "Skeletal Muscle Mass (kg) 70 80 90 100 110 120 25.0\n";
        var result = InBodyReportParser.Parse(text);
        Assert.False(result.Fields.First(f => f.IndicatorType == InBodyIndicatorTypes.Smm).Extracted);
    }

    [Fact]
    public void Parse_UnitGluedNumber_NotCaptured()
    {
        // Don vi "(9" / "(%)" bi OCR doc dinh so -> KHONG lay "9" lam PBF.
        const string text = "PBF (9 | 80 13.0 18.0 23.0\n";
        var result = InBodyReportParser.Parse(text);
        Assert.False(result.Fields.First(f => f.IndicatorType == InBodyIndicatorTypes.Pbf).Extracted);
    }

    [Fact]
    public void Parse_HistoryLastColumn_FillsSmmPbf_WhenWeightMatches()
    {
        // Fallback co kiem cheo: cot moi nhat (so cuoi) cua history dung cho SMM/PBF khi dong du
        // tot (>=3 so) VA Weight moi nhat history khop Weight hien tai.
        const string text = @"
            Sum of the above Weight (kg) 76.6 ( 49.2~66.6 )
            Body Composition History
            Weight (kg) 79.0 78.0 77.7 77.3 77.1 77.4 76.8 76.6
            SMM (kg) 25.3 25.6 25.0 25.8 24.6 25.5 24.8 25.0
            PBF (%) 41.9 40.5 41.5 39.9 42.1 40.2 41.3 40.6
        ";
        var r = InBodyReportParser.Parse(text);
        Assert.Equal(25.0m, GetValue(r, InBodyIndicatorTypes.Smm));
        Assert.Equal(40.6m, GetValue(r, InBodyIndicatorTypes.Pbf));
    }

    [Fact]
    public void Parse_HistoryLastColumn_Ignored_WhenWeightMismatch()
    {
        // Weight history khong khop Weight hien tai -> KHONG dung history (co the la benh nhan khac/loi).
        const string text = @"
            Sum of the above Weight (kg) 60.0 ( 49.2~66.6 )
            Body Composition History
            Weight (kg) 79.0 78.0 77.7 77.3 77.1 77.4 76.8 76.6
            SMM (kg) 25.3 25.6 25.0 25.8 24.6 25.5 24.8 25.0
        ";
        var r = InBodyReportParser.Parse(text);
        Assert.False(r.Fields.First(f => f.IndicatorType == InBodyIndicatorTypes.Smm).Extracted);
    }

    private static decimal? GetValue(InBodyReportData data, string indicatorType) =>
        data.Fields.First(f => f.IndicatorType == indicatorType).Value;
}
