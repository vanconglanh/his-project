using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.InBody;

/// <summary>
/// 1 chi so trich duoc (hoac khong trich duoc) tu ket qua may InBody.
/// </summary>
public sealed record InBodyFieldResult(string IndicatorType, decimal? Value, string? Unit, bool Extracted);

/// <summary>
/// Ket qua trich xuat tu 1 file/nguon du lieu InBody (PDF hien tai, sau nay co the la API truc tiep).
/// </summary>
public sealed record InBodyReportData(string RawText, IReadOnlyList<InBodyFieldResult> Fields)
{
    public bool IsFullyExtracted => Fields.Count > 0 && Fields.All(f => f.Extracted);
    public bool HasAnyExtracted => Fields.Any(f => f.Extracted);
    public IReadOnlyList<string> MissingIndicatorTypes => Fields.Where(f => !f.Extracted).Select(f => f.IndicatorType).ToList();
}

/// <summary>
/// Nguon du lieu InBody. MVP: <c>InBodyPdfTextProvider</c> (doc text layer PDF, KHONG OCR anh).
/// Dinh huong tuong lai: them <c>InBodyApiProvider</c> goi thang API may InBody, dang ky DI khac,
/// khong doi contract nay.
/// </summary>
public interface IInBodyDataProvider
{
    Task<Result<InBodyReportData>> ExtractAsync(Stream fileStream, string fileName, CancellationToken ct);
}

/// <summary>Danh sach ma indicator_type chuan cho ket qua InBody.</summary>
public static class InBodyIndicatorTypes
{
    public const string Weight = "WEIGHT_KG";
    public const string Bmi = "BMI";
    public const string Smm = "SMM";
    public const string BodyFatMass = "BODY_FAT_MASS";
    public const string Pbf = "PBF";
    public const string VisceralFat = "VISCERAL_FAT";
    public const string Tbw = "TBW";
    public const string Bmr = "BMR";
    public const string InBodyScore = "INBODY_SCORE";

    /// <summary>
    /// Cac indicator ghi vao bang generic diab_his_cli_indicator_reading.
    /// Bug B fix: THEM Bmi vao day de BMI (doc duoc tu parser) khong bi roi mat khi confirm.
    /// KHONG bao gom Weight — Weight ghi rieng vao diab_his_enc_vital_signs (weight_kg).
    /// </summary>
    public static readonly IReadOnlyList<string> IndicatorTableTypes = new[]
    {
        Bmi, Smm, BodyFatMass, Pbf, VisceralFat, Tbw, Bmr, InBodyScore
    };
}
