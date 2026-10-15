using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ProDiabHis.Api.Converters;
using Xunit;

namespace ProDiabHis.UnitTests.Api;

/// <summary>
/// FIX 2026-10-16 (A3): finished_at null truoc day bi serialize thanh "0001-01-01T00:00:00Z"
/// (DateTime.MinValue) khi field dung DateTime non-nullable. Khoa hanh vi dung: DateTime? null -> JSON null;
/// co gia tri -> hau to Z (UTC, BM-49).
/// </summary>
public class UtcNullableDateTimeConverterTests
{
    private record Dto(
        [property: JsonPropertyName("finished_at")] DateTime? FinishedAt);

    private static JsonSerializerOptions Options()
    {
        var o = new JsonSerializerOptions();
        o.Converters.Add(new UtcDateTimeConverter());
        o.Converters.Add(new UtcNullableDateTimeConverter());
        return o;
    }

    [Fact]
    public void FinishedAtNull_SerializeThanh_JsonNull_KhongPhai_MinValue()
    {
        var json = JsonSerializer.Serialize(new Dto(null), Options());

        json.Should().Contain("\"finished_at\":null");
        json.Should().NotContain("0001-01-01");
    }

    [Fact]
    public void FinishedAtCoGiaTri_SerializeVoi_HauToZ()
    {
        var dt = new DateTime(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new Dto(dt), Options());

        json.Should().Contain("\"finished_at\":\"2026-10-16T08:30:00Z\"");
    }

    [Fact]
    public void DeserializeNull_TraVe_Null()
    {
        var dto = JsonSerializer.Deserialize<Dto>("{\"finished_at\":null}", Options());
        dto!.FinishedAt.Should().BeNull();
    }
}
