using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>Kiem tra validate + tinh khoang ngay tuong doi cho Lich gui bao cao qua email (P3.3).</summary>
public class ReportScheduleTests
{
    private static SaveReportScheduleRequest ValidDaily(List<string>? recipients = null) => new(
        ReportCode: "revenue", Title: "Doanh thu hang ngay", Frequency: "DAILY", Hour: 7,
        DayOfWeek: null, DayOfMonth: null, Period: "YESTERDAY", Format: "PDF",
        Recipients: recipients ?? new List<string> { "a@x.vn" });

    [Fact]
    public void ToInput_ValidDaily_MapsCorrectly()
    {
        var input = ReportScheduleRequestMapper.ToInput(ValidDaily());

        Assert.Equal(ReportScheduleFrequency.Daily, input.Frequency);
        Assert.Equal(7, input.Hour);
        Assert.Null(input.DayOfWeek);
        Assert.Equal(ReportSchedulePeriod.Yesterday, input.Period);
        Assert.Equal(ReportScheduleFormat.Pdf, input.Format);
        Assert.Single(input.Recipients);
    }

    [Fact]
    public void ToInput_WeeklyMissingDayOfWeek_Throws()
    {
        var req = ValidDaily() with { Frequency = "WEEKLY", DayOfWeek = null };
        var ex = Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
        Assert.Equal("REPORT_SCHEDULE_INVALID", ex.ErrorCode);
    }

    [Fact]
    public void ToInput_MonthlyDayOfMonthOutOfRange_Throws()
    {
        var req = ValidDaily() with { Frequency = "MONTHLY", DayOfMonth = 30 };
        Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
    }

    [Fact]
    public void ToInput_InvalidHour_Throws()
    {
        var req = ValidDaily() with { Hour = 24 };
        Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
    }

    [Fact]
    public void ToInput_NoRecipients_Throws()
    {
        var req = ValidDaily(new List<string>());
        Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
    }

    [Fact]
    public void ToInput_InvalidEmail_Throws()
    {
        var req = ValidDaily(new List<string> { "not-an-email" });
        Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
    }

    [Fact]
    public void ToInput_InvalidFrequencyCode_Throws()
    {
        var req = ValidDaily() with { Frequency = "HOURLY" };
        Assert.Throws<ReportValidationException>(() => ReportScheduleRequestMapper.ToInput(req));
    }

    [Theory]
    [InlineData("TODAY", "2026-07-08", "2026-07-08")]
    [InlineData("YESTERDAY", "2026-07-07", "2026-07-07")]
    [InlineData("THIS_MONTH", "2026-07-01", "2026-07-08")]
    [InlineData("LAST_MONTH", "2026-06-01", "2026-06-30")]
    public void ResolveDateRange_ComputesExpectedWindow(string periodCode, string expectedFrom, string expectedTo)
    {
        var today = new DateOnly(2026, 7, 8); // Thu 4
        var period = ReportScheduleCodes.PeriodFromCode(periodCode);
        var (from, to) = ReportScheduleCodes.ResolveDateRange(period, today);

        Assert.Equal(DateOnly.Parse(expectedFrom), from);
        Assert.Equal(DateOnly.Parse(expectedTo), to);
    }

    [Fact]
    public void ResolveDateRange_ThisWeek_StartsAtSunday()
    {
        var today = new DateOnly(2026, 7, 8); // Wed
        var (from, to) = ReportScheduleCodes.ResolveDateRange(ReportSchedulePeriod.ThisWeek, today);

        Assert.Equal(DayOfWeek.Sunday, from.DayOfWeek);
        Assert.Equal(today, to);
        Assert.True(from <= today);
    }
}
