using ProDiabHis.Application.LabResults;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>
/// HL7 v2.5 ORU^R01 parser stub.
/// Parse OBR (order) va OBX (result) segments.
/// Format: segment|field^component...
/// </summary>
public class Hl7v25ParserStub : IHl7v25Parser
{
    public List<Hl7ParsedRow> Parse(string hl7Message)
    {
        var rows    = new List<Hl7ParsedRow>();
        var lines   = hl7Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string? currentOrderId = null;

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length == 0) continue;

            var segName = parts[0];

            if (segName == "OBR" && parts.Length > 3)
            {
                // OBR-3: filler order number (co the la lab_order_id)
                currentOrderId = parts.Length > 3 ? parts[3].Split('^')[0].Trim() : null;
            }
            else if (segName == "OBX" && parts.Length >= 6)
            {
                // OBX-3: observation identifier (test code)
                var testCode  = parts.Length > 3 ? parts[3].Split('^')[0].Trim() : "";
                var value     = parts.Length > 5 ? parts[5].Trim() : "";
                var unit      = parts.Length > 6 ? parts[6].Split('^')[0].Trim() : null;

                decimal? valNum = null;
                if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    valNum = parsed;

                // OBX-14: observation date/time
                var dateStr     = parts.Length > 14 ? parts[14].Trim() : "";
                var performedAt = ParseHl7DateTime(dateStr);

                if (!string.IsNullOrEmpty(testCode))
                    rows.Add(new(currentOrderId, testCode, value, valNum,
                        string.IsNullOrEmpty(unit) ? null : unit, performedAt));
            }
        }

        return rows;
    }

    private static DateTime ParseHl7DateTime(string s)
    {
        // HL7 format: YYYYMMDDHHMMSS
        if (s.Length >= 8 &&
            int.TryParse(s[..4], out var y) &&
            int.TryParse(s[4..6], out var mo) &&
            int.TryParse(s[6..8], out var d))
        {
            var h  = s.Length >= 10 ? int.Parse(s[8..10]) : 0;
            var mi = s.Length >= 12 ? int.Parse(s[10..12]) : 0;
            return new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);
        }
        return DateTime.UtcNow;
    }
}
