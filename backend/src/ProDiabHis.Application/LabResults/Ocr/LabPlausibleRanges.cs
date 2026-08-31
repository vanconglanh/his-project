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
    private static readonly (string Keyword, PlausibleRange Range)[] Rules =
    {
        // HbA1c (%): sinh lý 2-20%
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

        // Tuyến giáp
        ("TSH", new PlausibleRange(0m, 100m)),   // mIU/L
        ("FT3", new PlausibleRange(0m, 100m)),   // pmol/L
        ("FT4", new PlausibleRange(0m, 200m)),   // pmol/L
        ("T3",  new PlausibleRange(0m, 100m)),
        ("T4",  new PlausibleRange(0m, 500m)),
    };

    private const string DefaultNote =
        "Giá trị nằm ngoài khoảng thông thường, vui lòng kiểm tra lại (có thể do OCR đọc sai dấu thập phân)";

    /// <summary>
    /// Kiểm tra 1 giá trị số có nằm ngoài khoảng vật lý khả dĩ theo test_code không.
    /// Trả về (outOfRange, note). Không có quy tắc cho test_code -> (false, null).
    /// value null (không đọc được số) -> (false, null).
    /// </summary>
    public static (bool OutOfPlausibleRange, string? Note) Check(string? testCode, decimal? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(testCode))
            return (false, null);

        var normCode = testCode.Trim().ToUpperInvariant();

        foreach (var (keyword, range) in Rules)
        {
            if (normCode.Contains(keyword))
            {
                if (value.Value < range.Min || value.Value > range.Max)
                    return (true, DefaultNote);
                return (false, null);
            }
        }

        return (false, null);
    }
}
