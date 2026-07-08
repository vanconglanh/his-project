namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Chuyen doi ReportAggregation <-> ma chuoi dung trong JSON (definition_json + request body).
/// Dung chung giua Api (parse body) va Infrastructure (luu/doc DB) de tranh lech ma.</summary>
public static class ReportAggregationCodes
{
    public static string ToCode(ReportAggregation agg) => agg switch
    {
        ReportAggregation.Sum => "SUM",
        ReportAggregation.Count => "COUNT",
        ReportAggregation.Avg => "AVG",
        ReportAggregation.Min => "MIN",
        ReportAggregation.Max => "MAX",
        ReportAggregation.CountDistinct => "COUNT_DISTINCT",
        _ => throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Phép gộp '{agg}' không hợp lệ")
    };

    public static ReportAggregation FromCode(string code) => code?.Trim().ToUpperInvariant() switch
    {
        "SUM" => ReportAggregation.Sum,
        "COUNT" => ReportAggregation.Count,
        "AVG" => ReportAggregation.Avg,
        "MIN" => ReportAggregation.Min,
        "MAX" => ReportAggregation.Max,
        "COUNT_DISTINCT" => ReportAggregation.CountDistinct,
        _ => throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Phép gộp '{code}' không hợp lệ")
    };

    public static bool TryFromCode(string? code, out ReportAggregation agg)
    {
        agg = default;
        if (string.IsNullOrWhiteSpace(code)) return false;
        try { agg = FromCode(code); return true; }
        catch (ReportValidationException) { return false; }
    }
}
