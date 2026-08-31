namespace ProDiabHis.Application.InBody;

/// <summary>
/// Khoang VAT LY KHA DI cho tung chi so InBody (KHONG phai range dieu tri/binh thuong).
/// Muc tieu: bat loi doc nham OCR (vd 8.0 doc thanh 80, thieu/thua dau phay) — CANH BAO
/// chu KHONG chan cung. Gia tri ngoai khoang van cho luu (nguoi dung tu quyet dinh).
/// </summary>
public static class InBodyPlausibleRanges
{
    public sealed record Range(decimal Min, decimal Max, string? Unit);

    // Cac nguong dat rong (bao trum moi truong hop nguoi that) de chi bat loi doc nham ro rang.
    private static readonly IReadOnlyDictionary<string, Range> Ranges = new Dictionary<string, Range>
    {
        [InBodyIndicatorTypes.Weight]      = new Range(2m, 400m, "kg"),      // can nang co the
        [InBodyIndicatorTypes.Bmi]         = new Range(5m, 100m, "kg/m2"),   // chi so khoi co the
        [InBodyIndicatorTypes.Smm]         = new Range(1m, 80m, "kg"),       // khoi co xuong
        [InBodyIndicatorTypes.BodyFatMass] = new Range(0m, 200m, "kg"),      // khoi mo
        [InBodyIndicatorTypes.Pbf]         = new Range(1m, 70m, "%"),        // ty le mo (%)
        [InBodyIndicatorTypes.VisceralFat] = new Range(1m, 60m, null),       // muc mo noi tang
        [InBodyIndicatorTypes.Tbw]         = new Range(1m, 200m, "L"),       // tong nuoc co the
        [InBodyIndicatorTypes.Bmr]         = new Range(300m, 5000m, "kcal"), // chuyen hoa co ban
        [InBodyIndicatorTypes.InBodyScore] = new Range(0m, 100m, null),      // diem InBody
    };

    /// <summary>
    /// Kiem tra 1 gia tri co nam trong khoang vat ly kha di khong.
    /// Tra ve (outOfRange, note): outOfRange=false khi khong co gia tri, khong co dinh nghia
    /// khoang, hoac gia tri nam trong khoang. note chi co khi outOfRange=true.
    /// </summary>
    public static (bool OutOfRange, string? Note) Evaluate(string indicatorType, decimal? value)
    {
        if (!value.HasValue) return (false, null);
        if (!Ranges.TryGetValue(indicatorType, out var range)) return (false, null);
        if (value.Value >= range.Min && value.Value <= range.Max) return (false, null);

        var unit = string.IsNullOrEmpty(range.Unit) ? string.Empty : $" {range.Unit}";
        var note = $"Giá trị {value.Value}{unit} nằm ngoài khoảng khả dĩ ({range.Min}–{range.Max}{unit}), vui lòng kiểm tra lại (có thể đọc nhầm khi trích xuất).";
        return (true, note);
    }
}
