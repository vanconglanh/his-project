using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProDiabHis.Api.Converters;

/// <summary>
/// Serialize DateTime as UTC ISO 8601 với Z suffix để FE hiểu đúng múi giờ.
/// Dapper đọc từ MySQL trả về DateTimeKind.Unspecified dù giá trị là UTC —
/// converter này treat Unspecified như UTC để tránh FE hiển thị sai giờ (BM-49).
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        // BM-50: FE da gui UTC (sau fix BM-49), KHONG convert them lan nua.
        // Chi dam bao Kind=Utc de EF Core / Dapper luu dung, khong goi ToUniversalTime()
        // (goi ToUniversalTime() tren gia tri Kind=Local se tru them offset 7h).
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}

public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        _inner.Write(writer, value.Value, options);
    }
}
