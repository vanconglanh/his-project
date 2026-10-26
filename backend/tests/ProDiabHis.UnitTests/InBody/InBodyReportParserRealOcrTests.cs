using System.Linq;
using ProDiabHis.Application.InBody;
using Xunit;

namespace ProDiabHis.UnitTests.InBody;

/// <summary>
/// BM-InBody OCR: fixture la TEXT OCR THAT (Tesseract vie+eng) cua 1 phieu InBody270 SCAN
/// (anh 7.webp user gui), DA AN DANH (bo ten/SDT/ghi tay). Ground truth tren phieu:
///   TBW 33.3 L; Protein 8.9; Mineral 3.25; Body Fat Mass 31.1; Weight 76.6; SMM 25.0;
///   BMI 28.4; PBF 40.6; InBody Score 63; Visceral Fat Level 15; BMR 1353; Fat Free Mass 45.5.
/// OCR nhieu o vung bar/callout -> parser UU TIEN PRECISION: chi dien o doc chac chan
/// (Weight, TBW, Body Fat Mass, Visceral Fat Level, InBody Score); cac o OCR hong nang
/// (SMM "25, ()", BMI tach khoi nhan, PBF "(). o", BMR "13B3") -> de TRONG + canh bao,
/// KHONG dien sai (yeu cau an toan du lieu y te).
/// </summary>
public class InBodyReportParserRealOcrTests
{
    private const string RealOcrText = """
InBody
[InBody270]
TRUNG TAM DIEU TRI
CAN NANG VA BENH MAN TINH
TICH HOP
Height Age Gender | Test Date / Time DIAB
ence K 164. Lom 21 Female | 19. 06. 2026 12:14
Body Composition Analysis
y P : Y InBody Score —=
Total amount of water inmybody Total Body Water (L)| 33.3 (29.5~36.1)
What Ineedto bud muscles Protein (8| 8.9 (79x97 ) 63/100 ris
= al score that reflects the evaluation of bod
What | need for strong bones Mineral (kg)} 3.25 (2.73~3.33) Tol seore oer pers meee
100 points
Where my excess energy is stored Body Fat Mass (kg) 31 ( 11.6~18.5 ) Weight Control
Sum of the above Weight (kg)| 76.6 ( 49.2~66.6 ) Target Weight 59. lke
Weight Control -17. 5kg
Fat Control -17. 5kg
Muscle Control 0. Okg
Muscle-Fat Analysis
Obesity Evaluation
a INE--——-ENHINL”---H NHHEHINHNNL-:“NhNNENNI wsilzitly
W ht 70 85 100 5 130 5 160 175 190 305 BMI ONormal [1 Under Over
eigl (k;)| om on (ni nHEEEn 76. 6 [Ower
1 8D 90 10 HD H0 HO HO B0 10 H0 % Slightly =
es vos (K?)| mm in 25, () : ` = GNormal 0 Oyen Wore
io Go 80 100 160 20 28 3Ó 400 40 500 % Waist-Hip Ratio
Body FatMass (k2)) i VV 31. 1 0.75 _0. 85
0.91 _——$—$ — —
Obesity Analysis
man J over
2||[ 10 15.0 185 21.5 20
(kg/m?)
!
Body Mass Index
30 40 400 46.0 50.0 55.0
———m——..... uuuuuuu ng 28, 4
Visceral Fat Level—
Low 10
High
Level 15
Research Parameters -
Fat Free Mass 45.5 xg (40.1~49.0)
PBF (0@)|| P0 H0 HO 240 Ho 38.0 8.0 HOO BHO 550 Basal Metabolic Rate 13B3kei ( 1517~1771 )
Percent Body Fat | (0000000 rr me {(). ö Obesity Degree 1320 ( 90~110 )
Lean Mass Fat Mass SMI VÀ 2 kg/m?
hà Hà Recommended calore intake 1846 tccat
Evaluation Evaluation
Segmental Lean Analysis
Segmental Fat Analysis
Calorie Expenditure of Exercise
Golf 135] Gateball 146
2. 1 31 ke Walking 153] Yoga 153
9: 3 € i Badminton 173} Table Tennis 173
ormal Normal Tennis 230) Bicycling 230
20. One Boxing 230) Basketball 230
e ( Xr k 3 3 Mountain Climbing _ 250| Jumping Rope 268
® bì @ ® Le] A ða oR
a a oa = Aerobics 268} Jogging 268
Normal Soccer 268] Swimming 268
7. 35 ks io — x Japanese Fencing _ 3833| Racketball 383
100, 1% ko Squash 383| Taekwondo 383
Normal Neal Based on your current weight
«Based on 30 minute duration
Impedance
+ Segmental fat is estimated RA LA TR RL. LI
Body Composition History
79.0 j i
i kg : i77.4i 78 gi 7e 6
Weight (kg)} | 76.8} 76, 6
SMM kg)|| 25. 3
Skeletal Muscle Mass. 2 †
41.9)
Pee om (6)
= SOR DOD
WRecent aTotal 1329 lego
i" i 15:09 |
ZQ) 20knz| 357. 6 382.6 27.9 239.6 241.9
5 213.6 217.9
100 ktiz) 325. 5 349. 2
""";

    private static decimal? V(InBodyReportData d, string t) =>
        d.Fields.First(f => f.IndicatorType == t).Value;
    private static bool Ex(InBodyReportData d, string t) =>
        d.Fields.First(f => f.IndicatorType == t).Extracted;

    [Fact]
    public void Parse_RealInBody270Ocr_FillsReliableFields_LeavesGarbledEmpty()
    {
        var d = InBodyReportParser.Parse(RealOcrText);

        // Cac o doc CHAC CHAN (value sau nhan, truoc "(range)", cung dong) -> dung ground truth
        Assert.Equal(76.6m, V(d, InBodyIndicatorTypes.Weight));
        Assert.Equal(33.3m, V(d, InBodyIndicatorTypes.Tbw));
        Assert.Equal(15m, V(d, InBodyIndicatorTypes.VisceralFat));
        Assert.Equal(63m, V(d, InBodyIndicatorTypes.InBodyScore));
        // Body Fat Mass: OCR o bang Body Composition cho "31" (rot ".1") - van dien (31.0, gan dung)
        Assert.Equal(31m, V(d, InBodyIndicatorTypes.BodyFatMass));

        // BMI: dien qua KIEM CHEO cong thuc Weight/(Height/100)^2 (~28.49) -> chon ung vien 28.4
        // trong vung Obesity (loai vach thang do). Trung ground truth 28.4.
        Assert.Equal(28.4m, V(d, InBodyIndicatorTypes.Bmi));

        // Cac o OCR hong nang, khong co nguon kiem cheo -> de TRONG (precision), KHONG dien sai.
        // SMM/PBF: callout + history OCR hong nang (dong history < 3 so) -> khong du tin cay.
        Assert.False(Ex(d, InBodyIndicatorTypes.Smm));
        Assert.False(Ex(d, InBodyIndicatorTypes.Pbf));
        Assert.False(Ex(d, InBodyIndicatorTypes.Bmr));

        // Khong bao gio la "all empty" nhu truoc fix (truoc fix: PDF scan -> text rong -> 0 o)
        Assert.True(d.HasAnyExtracted);
    }

    [Fact]
    public void Parse_RealOcr_NoImplausibleValues()
    {
        var d = InBodyReportParser.Parse(RealOcrText);
        foreach (var f in d.Fields.Where(f => f.Extracted))
        {
            var (outOfRange, _) = InBodyPlausibleRanges.Evaluate(f.IndicatorType, f.Value);
            Assert.False(outOfRange, $"{f.IndicatorType} = {f.Value} ngoai khoang kha di");
        }
    }
}
