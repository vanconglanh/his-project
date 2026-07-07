using System.Globalization;

namespace ProDiabHis.Application.Reports.Engine;

/// <summary>Helper convert/format gia tri dong Dapper (dynamic/object?) dung chung cho data-service + PDF + Excel.</summary>
public static class ReportValueConverter
{
    /// <summary>Lay gia tri theo key tu 1 dong bao cao (IDictionary), tra null neu khong co — tranh CS0411
    /// khi goi GetValueOrDefault truc tiep tren IDictionary&lt;TKey,TValue&gt; (chi co extension cho IReadOnlyDictionary).</summary>
    public static object? Get(IDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var v) ? v : null;

    public static decimal ToDecimal(object? v) => v switch
    {
        null => 0m,
        decimal d => d,
        double db => (decimal)db,
        float f => (decimal)f,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        string str when decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var ds) => ds,
        _ => 0m
    };

    public static string FormatValue(object? raw, ReportColumnType type)
    {
        if (raw is null || raw is DBNull) return "-";

        switch (type)
        {
            case ReportColumnType.Money:
                return $"{ToDecimal(raw):#,##0}";
            case ReportColumnType.Number:
                return $"{ToDecimal(raw):#,##0.####}";
            case ReportColumnType.Date:
                return raw switch
                {
                    DateTime dt => dt.ToString("dd/MM/yyyy"),
                    DateOnly d0 => d0.ToString("dd/MM/yyyy"),
                    _ => raw.ToString() ?? "-"
                };
            case ReportColumnType.DateTime:
                return raw switch
                {
                    DateTime dt2 => dt2.ToString("dd/MM/yyyy HH:mm"),
                    _ => raw.ToString() ?? "-"
                };
            default:
                return raw.ToString() ?? "-";
        }
    }
}
