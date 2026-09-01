namespace ProDiabHis.Application.LabResults.Ocr;

// ═══════════════════════════════════════════════════════════════════════════
// GAP-3: Ngưỡng VẬT LÝ KHẢ DĨ (plausible range) cho các XN thường quy Nội tiết.
//
// KHÁC reference range điều trị: đây KHÔNG phải khoảng bình thường bệnh lý, mà là
// khoảng giá trị con người có thể có về mặt sinh học. Mục đích duy nhất: chặn lỗi
// OCR đọc nhầm dấu chấm thập phân (vd "8.1" -> "81", "6.4" -> "64"). Nằm ngoài
// khoảng này gần như chắc chắn là OCR sai, KHÔNG phải kết quả thật.
//
// Không chặn cứng — chỉ set cờ cảnh báo trong extract response để FE hiển thị đỏ +
// checkbox cho người dùng kiểm tra lại. test_code không có trong bảng -> không cảnh báo.
// ═══════════════════════════════════════════════════════════════════════════
public static class LabPlausibleRanges
{
    /// <summary>1 ngưỡng vật lý khả dĩ (min/max) cho 1 nhóm XN.</summary>
    public readonly record struct PlausibleRange(decimal Min, decimal Max);

    // Map theo TỪ KHÓA chuẩn hóa (uppercase) xuất hiện trong test_code. Khớp linh hoạt:
    // test_code chứa 1 trong các từ khóa (substring) -> áp ngưỡng tương ứng.
    // Thứ tự quan trọng: từ khóa dài/cụ thể trước (HBA1C trước GLU, LDL/HDL trước CHOL...).
    // Ngưỡng theo don vi mmol/L (mac dinh, hanh vi hien tai)
    private static readonly (string Keyword, PlausibleRange Range)[] RulesMmol =
    {
        // HbA1c (%): sinh lý 2-20% - khong phu thuoc mmol/mg
        ("HBA1C", new PlausibleRange(2m, 20m)),
        ("A1C",   new PlausibleRange(2m, 20m)),

        // Đường huyết / glucose (mmol/L): 1-100
        ("GLUCOSE", new PlausibleRange(1m, 100m)),
        ("GLU",     new PlausibleRange(1m, 100m)),

        // Lipid (mmol/L): 0-50
        ("TRIGLYCERIDE", new PlausibleRange(0m, 50m)),
        ("TRIG", new PlausibleRange(0m, 50m)),
        ("TG",   new PlausibleRange(0m, 50m)),
        ("LDL",  new PlausibleRange(0m, 50m)),
        ("HDL",  new PlausibleRange(0m, 50m)),
        ("CHOLESTEROL", new PlausibleRange(0m, 50m)),
        ("CHOL", new PlausibleRange(0m, 50m)),
        ("LIPID", new PlausibleRange(0m, 50m)),

        // Tuyến giáp - khong phu thuoc mmol/mg
        ("TSH", new PlausibleRange(0m, 100m)),   // mIU/L
        ("FT3", new PlausibleRange(0m, 100m)),   // pmol/L
        ("FT4", new PlausibleRange(0m, 200m)),   // pmol/L
        ("T3",  new PlausibleRange(0m, 100m)),
        ("T4",  new PlausibleRange(0m, 500m)),
    };

    // Ngưỡng rieng cho don vi mg/dL (glucose/lipid quy doi ~18x cho glucose, ~38.6x cho cholesterol/TG)
    private static readonly (string Keyword, PlausibleRange Range)[] RulesMgDl =
    {
        ("GLUCOSE", new PlausibleRange(10m, 2000m)),
        ("GLU",     new PlausibleRange(10m, 2000m)),

        ("TRIGLYCERIDE", new PlausibleRange(0m, 2000m)),
        ("TRIG", new PlausibleRange(0m, 2000m)),
        ("TG",   new PlausibleRange(0m, 2000m)),
        ("LDL",  new PlausibleRange(0m, 2000m)),
        ("HDL",  new PlausibleRange(0m, 2000m)),
        ("CHOLESTEROL", new PlausibleRange(0m, 2000m)),
        ("CHOL", new PlausibleRange(0m, 2000m)),
        ("LIPID", new PlausibleRange(0m, 2000m)),
    };

    private const string DefaultNote =
        "Giá trị nằm ngoài khoảng thông thường, vui lòng kiểm tra lại (có thể do OCR đọc sai dấu thập phân)";

    /// <summary>
    /// Kiểm tra 1 giá trị số có nằm ngoài khoảng vật lý khả dĩ theo test_code không.
    /// Trả về (outOfRange, note). Không có quy tắc cho test_code -> (false, null).
    /// value null (không đọc được số) -> (false, null).
    /// </summary>
    public static (bool OutOfPlausibleRange, string? Note) Check(string? testCode, decimal? value, string? unit = null)
    {
        if (value is null || string.IsNullOrWhiteSpace(testCode))
            return (false, null);

        var normCode = testCode.Trim().ToUpperInvariant();
        // Detect don vi tu chinh du lieu OCR: co "mg" (mg/dL) -> dung nguong mg/dL,
        // khong co / null -> mac dinh mmol/L (giu hanh vi cu, an toan nguoc).
        var isMgDl = !string.IsNullOrWhiteSpace(unit) && unit.Contains("mg", StringComparison.OrdinalIgnoreCase);

        // Uu tien bang nguong theo don vi (mg/dL); XN khong co trong bang rieng
        // (HbA1c %, TSH, FT3/FT4...) fallback sang bang mmol/L mac dinh (khong
        // phu thuoc don vi mg/mmol nen gia tri giong nhau o ca 2 bang).
        if (isMgDl)
        {
            foreach (var (keyword, range) in RulesMgDl)
            {
                if (normCode.Contains(keyword))
                    return value.Value < range.Min || value.Value > range.Max ? (true, DefaultNote) : (false, null);
            }
        }

        foreach (var (keyword, range) in RulesMmol)
        {
            if (normCode.Contains(keyword))
                return value.Value < range.Min || value.Value > range.Max ? (true, DefaultNote) : (false, null);
        }

        return (false, null);
    }
}
