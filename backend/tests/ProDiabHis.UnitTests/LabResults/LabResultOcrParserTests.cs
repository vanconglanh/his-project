using ProDiabHis.Application.LabResults.Ocr;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

public class LabResultOcrParserTests
{
    private static LabOcrPendingTest Pending(string code, string name) =>
        new(Guid.NewGuid(), code, name);

    // Mo phong text OCR tu 1 phieu KQ bo lipid mau + HbA1c + Glucose (co dau tieng Viet, layout tu do)
    private const string LipidSheetText = @"
PHONG XET NGHIEM ABC
Ho ten: Nguyen Van A    Ma BN: BN00123    Ngay: 30/08/2026

Ten xet nghiem            Ket qua      Don vi      Tham chieu
Glucose (duong huyet)     7.2          mmol/L      3.9 - 6.4
HbA1c                     8.10         %           4.0 - 6.0
Cholesterol toan phan     6.1          mmol/L      < 5.2
Triglyceride              2.30         mmol/L      < 1.7
HDL-C                     1.05         mmol/L      > 1.03
LDL-C                     3.90         mmol/L      < 3.4
";

    [Fact]
    public void Parse_LipidSheet_ExtractsAllOrderedTests()
    {
        var pending = new[]
        {
            Pending("GLU",   "Glucose"),
            Pending("HBA1C", "HbA1c (Định lượng)"),
            Pending("CHOL",  "Cholesterol toàn phần"),
            Pending("TG",    "Triglyceride"),
            Pending("HDL",   "HDL-Cholesterol"),
            Pending("LDL",   "LDL-Cholesterol"),
        };

        var result = LabResultOcrParser.Parse(LipidSheetText, pending);

        Assert.Equal(6, result.Fields.Count);
        Assert.Equal(6, result.ExtractedCount);
        Assert.Equal(7.2m,  Val(result, "GLU"));
        Assert.Equal(8.10m, Val(result, "HBA1C"));
        Assert.Equal(6.1m,  Val(result, "CHOL"));
        Assert.Equal(2.30m, Val(result, "TG"));
        Assert.Equal(1.05m, Val(result, "HDL"));
        Assert.Equal(3.90m, Val(result, "LDL"));
        Assert.Equal("mmol/l", Unit(result, "GLU"));
        Assert.Equal("%",      Unit(result, "HBA1C"));
    }

    [Fact]
    public void Parse_OnlyOrderedTests_IgnoresExtraLinesInSheet()
    {
        // Chi chi dinh HbA1c — du phieu co nhieu XN khac, parser chi tra ve dung XN dang cho
        var pending = new[] { Pending("HBA1C", "HbA1c") };

        var result = LabResultOcrParser.Parse(LipidSheetText, pending);

        Assert.Single(result.Fields);
        Assert.Equal(8.10m, Val(result, "HBA1C"));
    }

    [Fact]
    public void Parse_MissingTestInSheet_MarksNotExtracted_DoesNotBlockOthers()
    {
        // TSH khong co tren phieu -> Extracted=false; Glucose van doc duoc binh thuong
        var pending = new[]
        {
            Pending("GLU", "Glucose"),
            Pending("TSH", "TSH"),
        };

        var result = LabResultOcrParser.Parse(LipidSheetText, pending);

        Assert.Equal(7.2m, Val(result, "GLU"));
        var tsh = result.Fields.First(f => f.TestCode == "TSH");
        Assert.False(tsh.Extracted);
        Assert.Null(tsh.ValueNumeric);
    }

    [Fact]
    public void Parse_AccentInsensitive_MatchesVietnameseLabels()
    {
        const string text = "Đường huyết: 5.6 mmol/L\nAcid uric: 420 umol/L\n";
        var pending = new[]
        {
            Pending("GLU", "Đường huyết"),
            Pending("UA",  "Acid uric"),
        };

        var result = LabResultOcrParser.Parse(text, pending);

        Assert.Equal(5.6m, Val(result, "GLU"));
        Assert.Equal(420m, Val(result, "UA"));
    }

    [Fact]
    public void Parse_ShortCode_DoesNotMatchInsideLongerWord()
    {
        // "GLU" khong duoc dinh vao "Glucose" cua dong khac de lay nham so;
        // o day chi co dong LDL, dam bao "LDL" khong bat nham tu "HDL" hay chuoi khac.
        const string text = "LDL-C 3.90 mmol/L\n";
        var pending = new[] { Pending("LDL", "LDL-Cholesterol") };

        var result = LabResultOcrParser.Parse(text, pending);
        Assert.Equal(3.90m, Val(result, "LDL"));
    }

    [Fact]
    public void Parse_EmptyOrScannedNoText_ReturnsAllNotExtracted_NoException()
    {
        var pending = new[] { Pending("GLU", "Glucose"), Pending("HBA1C", "HbA1c") };

        var result = LabResultOcrParser.Parse(string.Empty, pending);

        Assert.Equal(2, result.Fields.Count);
        Assert.False(result.HasAnyExtracted);
        Assert.All(result.Fields, f => Assert.False(f.Extracted));
    }

    [Fact]
    public void Parse_NullText_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            LabResultOcrParser.Parse(null, new[] { Pending("GLU", "Glucose") }));
        Assert.Null(ex);
    }

    [Fact]
    public void Parse_CommaDecimal_ParsedCorrectly()
    {
        const string text = "Glucose 7,2 mmol/L\n";
        var result = LabResultOcrParser.Parse(text, new[] { Pending("GLU", "Glucose") });
        Assert.Equal(7.2m, Val(result, "GLU"));
    }

    private static decimal? Val(LabOcrParseResult r, string code) =>
        r.Fields.First(f => f.TestCode == code).ValueNumeric;

    private static string? Unit(LabOcrParseResult r, string code) =>
        r.Fields.First(f => f.TestCode == code).Unit;
}
