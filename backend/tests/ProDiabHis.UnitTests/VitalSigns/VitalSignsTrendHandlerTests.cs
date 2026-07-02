using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ProDiabHis.Application.VitalSigns;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.VitalSigns;

/// <summary>
/// Unit test cho <see cref="GetVitalSignsTrendQueryHandler"/> — thống kê xu hướng 1 chỉ số sinh hiệu.
///
/// Dùng harness InMemory (<see cref="TestDbContextFactory"/> + FakeTenantProvider) nên KHÔNG cần MySQL/Docker.
/// Bám CẤU TRÚC Result&lt;T&gt;/DTO + GIÁ TRỊ SỐ (seed cố định -> assert min/max/avg/first/latest), không bám
/// câu chữ. Các giá trị seed chọn sao cho trung bình chia hết -> làm tròn không mập mờ.
/// </summary>
public class VitalSignsTrendHandlerTests
{
    /// <summary>
    /// Seed 1 bản ghi VitalSigns tối giản (chỉ set trường cần cho trend) cho một bệnh nhân/tenant.
    /// Fully-qualify Domain.Entities.VitalSigns vì namespace file trùng tên 'VitalSigns'.
    /// </summary>
    private static Domain.Entities.VitalSigns Row(
        string patientId, DateTime recordedAt, int seq, int tenantId = 1,
        decimal? glucose = null, int? bpSystolic = null, decimal? weight = null,
        decimal? temperature = null, int? heartRate = null, int? respiratoryRate = null,
        int? bpDiastolic = null, int? spo2 = null)
        => new()
        {
            TenantId = tenantId,
            EncounterId = Guid.NewGuid().ToString(),
            PatientId = patientId,
            RecordedAt = recordedAt,
            RecordSequence = seq,
            GlucoseMgDl = glucose,
            BpSystolic = bpSystolic,
            WeightKg = weight,
            TemperatureC = temperature,
            HeartRateBpm = heartRate,
            RespiratoryRate = respiratoryRate,
            BpDiastolic = bpDiastolic,
            Spo2Percent = spo2,
        };

    // ─── (1) Happy path: tính đúng thống kê + thứ tự chuỗi theo RecordedAt ───
    [Fact]
    public async Task Trend_Glucose_HappyPath_ComputesStatsAndOrderedSeries()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, glucose: 100m),
            Row(patId, t0.AddHours(1), 2, glucose: 200m),
            Row(patId, t0.AddHours(2), 3, glucose: 150m));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Metric.Should().Be("glucose_mg_dl");
        d.Count.Should().Be(3);
        d.Min.Should().Be(100m);
        d.Max.Should().Be(200m);
        d.Average.Should().Be(150m);       // (100+200+150)/3 = 150
        d.First.Should().Be(100m);         // sớm nhất theo RecordedAt
        d.Latest.Should().Be(150m);        // muộn nhất theo RecordedAt
        d.Series.Select(p => p.Value).Should().ContainInOrder(100m, 200m, 150m);
    }

    // ─── (2) Lọc khoảng ngày: bản ghi ngoài [DateFrom, DateTo] bị loại ───
    [Fact]
    public async Task Trend_DateRange_ExcludesRowsOutsideWindow()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        db.VitalSigns.AddRange(
            Row(patId, new DateTime(2026, 1, 1,  9, 0, 0, DateTimeKind.Utc), 1, glucose: 500m), // ngoài (trước)
            Row(patId, new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), 2, glucose: 100m), // trong
            Row(patId, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc), 3, glucose: 140m), // trong
            Row(patId, new DateTime(2026, 1, 31, 9, 0, 0, DateTimeKind.Utc), 4, glucose: 900m));// ngoài (sau)
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(
                Guid.Parse(patId), "glucose_mg_dl",
                new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 20)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(2);
        d.Min.Should().Be(100m);
        d.Max.Should().Be(140m);
        d.Series.Select(p => p.Value).Should().ContainInOrder(100m, 140m);
    }

    // ─── (3) Mẫu null bị loại khỏi thống kê (count chỉ tính mẫu không null) ───
    [Fact]
    public async Task Trend_NullSamples_AreSkipped()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, glucose: 120m),
            Row(patId, t0.AddHours(1), 2, glucose: null, bpSystolic: 130), // đo huyết áp, KHÔNG đo đường huyết
            Row(patId, t0.AddHours(2), 3, glucose: 160m));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(2);                 // bản ghi glucose null không được đếm
        d.Series.Should().HaveCount(2);
        d.Min.Should().Be(120m);
        d.Max.Should().Be(160m);
    }

    // ─── (4) Metric ngoài whitelist -> Failure VITAL_TREND_INVALID_METRIC ───
    [Fact]
    public async Task Trend_UnknownMetric_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new GetVitalSignsTrendQueryHandler(db);

        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.NewGuid(), "not_a_metric", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VITAL_TREND_INVALID_METRIC");
    }

    // ─── (5) Tập rỗng -> Success, count 0, mọi thống kê null ───
    [Fact]
    public async Task Trend_EmptyResult_ReturnsZeroCountNullStats()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new GetVitalSignsTrendQueryHandler(db);

        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.NewGuid(), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(0);
        d.Min.Should().BeNull();
        d.Max.Should().BeNull();
        d.Average.Should().BeNull();
        d.Latest.Should().BeNull();
        d.First.Should().BeNull();
        d.Series.Should().BeEmpty();
    }

    // ─── (6) Cô lập đa-tenant: bản ghi tenant khác bị global query filter loại ───
    [Fact]
    public async Task Trend_TenantFilter_ExcludesOtherTenantRows()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, tenantId: 1, glucose: 100m),
            Row(patId, t0.AddHours(1), 2, tenantId: 2, glucose: 999m)); // tenant khác -> phải bị loại
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(1);
        d.Max.Should().Be(100m);   // 999 của tenant 2 KHÔNG lọt vào
    }

    // ─── (7) Selector map đọc đúng cột cho CẢ 8 metric (phủ cả cột int? lẫn decimal?) ───
    [Theory]
    [InlineData("glucose_mg_dl", 100, 200)]
    [InlineData("bp_systolic", 110, 130)]
    [InlineData("bp_diastolic", 70, 90)]
    [InlineData("heart_rate_bpm", 60, 80)]
    [InlineData("respiratory_rate", 12, 20)]
    [InlineData("spo2_percent", 94, 99)]
    [InlineData("weight_kg", 60, 70)]
    [InlineData("temperature_c", 36.5, 38.0)]
    public async Task Trend_SelectorMap_ReadsCorrectColumnPerMetric(string metric, double expMin, double expMax)
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        // Mỗi bản ghi set GIÁ TRỊ KHÁC NHAU ở TỪNG cột -> chứng minh selector chọn đúng cột (bắt mismapping cùng kiểu).
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, glucose: 100m, bpSystolic: 110, weight: 60m, temperature: 36.5m, heartRate: 60, respiratoryRate: 12, bpDiastolic: 70, spo2: 94),
            Row(patId, t0.AddHours(1), 2, glucose: 200m, bpSystolic: 130, weight: 70m, temperature: 38.0m, heartRate: 80, respiratoryRate: 20, bpDiastolic: 90, spo2: 99),
            Row(patId, t0.AddHours(2), 3, glucose: 150m, bpSystolic: 120, weight: 65m, temperature: 37.2m, heartRate: 70, respiratoryRate: 16, bpDiastolic: 80, spo2: 97));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), metric, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(3);
        d.Min.Should().Be((decimal)expMin);
        d.Max.Should().Be((decimal)expMax);
    }

    // ─── (8) Series/First/Latest bám OrderBy(RecordedAt) — seed LỆCH thứ tự chèn (giết mutation xoá OrderBy) ───
    [Fact]
    public async Task Trend_Series_OrderedByRecordedAt_RegardlessOfInsertionOrder()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        // Cố ý chèn KHÔNG theo thứ tự thời gian: muộn nhất chèn ĐẦU, sớm nhất chèn GIỮA.
        // InMemory trả theo thứ tự chèn -> nếu handler bỏ OrderBy(RecordedAt), test này sẽ ĐỎ.
        db.VitalSigns.AddRange(
            Row(patId, t0.AddHours(2), 1, glucose: 300m),
            Row(patId, t0,             2, glucose: 100m),
            Row(patId, t0.AddHours(1), 3, glucose: 200m));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Series.Select(p => p.RecordedAt).Should().BeInAscendingOrder();
        d.Series.Select(p => p.Value).Should().ContainInOrder(100m, 200m, 300m);
        d.First.Should().Be(100m);   // sớm nhất theo RecordedAt (dù chèn ở giữa)
        d.Latest.Should().Be(300m);  // muộn nhất theo RecordedAt (dù chèn đầu)
    }

    // ─── (9) Trùng RecordedAt -> tie-break XÁC ĐỊNH theo CreatedAt (First/Latest ổn định) ───
    [Fact]
    public async Task Trend_Ties_SameRecordedAt_TieBreakByCreatedAt()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var sameTime = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var rowA = Row(patId, sameTime, 1, glucose: 100m);
        var rowB = Row(patId, sameTime, 2, glucose: 200m);
        db.VitalSigns.AddRange(rowB, rowA); // chèn B trước A để loại trừ "may nhờ thứ tự chèn"
        await db.SaveChangesAsync();

        // Ép CreatedAt: A tạo TRƯỚC B -> tie-break theo CreatedAt phải cho First=A(100), Latest=B(200).
        db.Entry(rowA).Property(e => e.CreatedAt).CurrentValue = sameTime.AddMinutes(-2);
        db.Entry(rowB).Property(e => e.CreatedAt).CurrentValue = sameTime.AddMinutes(-1);
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(2);
        d.First.Should().Be(100m);   // A: CreatedAt sớm hơn
        d.Latest.Should().Be(200m);  // B: CreatedAt muộn hơn
        d.Series.Select(p => p.Value).Should().ContainInOrder(100m, 200m);
    }

    // ─── (10) Average làm tròn 2 chữ số trên giá trị PHÂN SỐ (giết mutation bỏ Math.Round) ───
    [Fact]
    public async Task Trend_Average_RoundsToTwoDecimals()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, glucose: 100m),
            Row(patId, t0.AddHours(1), 2, glucose: 101m),
            Row(patId, t0.AddHours(2), 3, glucose: 103m)); // (100+101+103)/3 = 101.333...
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, null),
            CancellationToken.None);

        result.Value!.Average.Should().Be(101.33m); // làm tròn 2 chữ số
    }

    // ─── (11) Biên khoảng ngày INCLUSIVE hai đầu (00:00 đầu ngày / cuối ngày) ───
    [Fact]
    public async Task Trend_DateRange_BoundariesAreInclusive()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var from = new DateOnly(2026, 1, 10);
        var to   = new DateOnly(2026, 1, 20);
        db.VitalSigns.AddRange(
            Row(patId, new DateTime(2026, 1,  9, 23, 0, 0, DateTimeKind.Utc), 1, glucose: 500m), // trước from -> LOẠI
            Row(patId, new DateTime(2026, 1, 10,  0, 30, 0, DateTimeKind.Utc), 2, glucose: 100m), // đầu from -> LỌT
            Row(patId, new DateTime(2026, 1, 20, 23, 0, 0, DateTimeKind.Utc), 3, glucose: 140m), // cuối to -> LỌT
            Row(patId, new DateTime(2026, 1, 21,  0,  0, 0, DateTimeKind.Utc), 4, glucose: 900m));// to+1 -> LOẠI
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", from, to),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(2);
        d.Min.Should().Be(100m);
        d.Max.Should().Be(140m);
        d.Series.Select(p => p.Value).Should().ContainInOrder(100m, 140m);
    }

    // ─── (12) Chỉ truyền DateFrom (DateTo null) -> chỉ chặn dưới ───
    [Fact]
    public async Task Trend_DateRange_OnlyDateFrom_FiltersLowerBoundOnly()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        db.VitalSigns.AddRange(
            Row(patId, new DateTime(2026, 1,  1, 9, 0, 0, DateTimeKind.Utc), 1, glucose: 500m), // trước from -> LOẠI
            Row(patId, new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), 2, glucose: 100m),
            Row(patId, new DateTime(2026, 1, 20, 9, 0, 0, DateTimeKind.Utc), 3, glucose: 120m));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", new DateOnly(2026, 1, 5), null),
            CancellationToken.None);

        result.Value!.Count.Should().Be(2);
        result.Value!.Min.Should().Be(100m);
    }

    // ─── (13) Chỉ truyền DateTo (DateFrom null) -> chỉ chặn trên ───
    [Fact]
    public async Task Trend_DateRange_OnlyDateTo_FiltersUpperBoundOnly()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        db.VitalSigns.AddRange(
            Row(patId, new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), 1, glucose: 100m),
            Row(patId, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc), 2, glucose: 120m),
            Row(patId, new DateTime(2026, 1, 31, 9, 0, 0, DateTimeKind.Utc), 3, glucose: 900m)); // sau to -> LOẠI
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "glucose_mg_dl", null, new DateOnly(2026, 1, 20)),
            CancellationToken.None);

        result.Value!.Count.Should().Be(2);
        result.Value!.Max.Should().Be(120m);
    }

    // ─── (14) Chuẩn hoá metric: Trim + ToLowerInvariant, trả về tên đã chuẩn hoá ───
    [Fact]
    public async Task Trend_MetricNormalization_TrimAndLowercase()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patId = Guid.NewGuid().ToString();
        var t0 = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
        db.VitalSigns.AddRange(
            Row(patId, t0,             1, glucose: 100m),
            Row(patId, t0.AddHours(1), 2, glucose: 200m),
            Row(patId, t0.AddHours(2), 3, glucose: 150m));
        await db.SaveChangesAsync();

        var handler = new GetVitalSignsTrendQueryHandler(db);
        var result = await handler.Handle(
            new GetVitalSignsTrendQuery(Guid.Parse(patId), "  Glucose_Mg_Dl  ", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Count.Should().Be(3);
        d.Metric.Should().Be("glucose_mg_dl"); // đã chuẩn hoá
    }
}
