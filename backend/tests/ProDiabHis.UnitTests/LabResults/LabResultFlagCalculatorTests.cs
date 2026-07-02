using FluentAssertions;
using ProDiabHis.Application.LabResults;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

public class LabResultFlagCalculatorTests
{
    private readonly LabResultFlagCalculator _sut = new();

    // ─── NORMAL ───
    [Theory]
    [InlineData(5.0, 4.0, 6.0)]
    [InlineData(4.0, 4.0, 6.0)]  // bang low
    [InlineData(6.0, 4.0, 6.0)]  // bang high
    public void Calculate_WithinRange_ReturnsNormal(double val, double low, double high)
    {
        var flag = _sut.Calculate((decimal)val, (decimal)low, (decimal)high);
        flag.Should().Be(LabResultFlag.Normal);
    }

    // ─── H (cao hon khoang, chua den HH) ───
    [Fact]
    public void Calculate_SlightlyAboveHigh_ReturnsH()
    {
        // range 4-6, span=2; HH threshold = 6 + 2*0.5 = 7
        // 6.5 > 6 nhung < 7 -> H
        var flag = _sut.Calculate(6.5m, 4m, 6m);
        flag.Should().Be(LabResultFlag.H);
    }

    // ─── L (thap hon khoang) ───
    [Fact]
    public void Calculate_SlightlyBelowLow_ReturnsL()
    {
        // 3.5 < 4 nhung con > 4 - 2*0.5 = 3 -> L
        var flag = _sut.Calculate(3.5m, 4m, 6m);
        flag.Should().Be(LabResultFlag.L);
    }

    // ─── HH ───
    [Fact]
    public void Calculate_WellAboveHigh_ReturnsHH()
    {
        // range 4-6, span=2; HH = > 6+1 = 7; Critical = > 6+2 = 8
        // 7.5 > 7 nhung < 8 -> HH
        var flag = _sut.Calculate(7.5m, 4m, 6m);
        flag.Should().Be(LabResultFlag.HH);
    }

    // ─── LL ───
    [Fact]
    public void Calculate_WellBelowLow_ReturnsLL()
    {
        // LL = < 4 - 2*0.5 = 3; Critical = < 4 - 2*1 = 2
        // 2.5 < 3 nhung > 2 -> LL
        var flag = _sut.Calculate(2.5m, 4m, 6m);
        flag.Should().Be(LabResultFlag.LL);
    }

    // ─── CRITICAL ───
    [Fact]
    public void Calculate_ExtremlyHigh_ReturnsCritical()
    {
        // Critical = > 6 + 2*1 = 8
        var flag = _sut.Calculate(9m, 4m, 6m);
        flag.Should().Be(LabResultFlag.Critical);
    }

    [Fact]
    public void Calculate_ExtremlyLow_ReturnsCritical()
    {
        // Critical = < 4 - 2*1 = 2
        var flag = _sut.Calculate(1m, 4m, 6m);
        flag.Should().Be(LabResultFlag.Critical);
    }

    // ─── Null cases ───
    [Fact]
    public void Calculate_NullValue_ReturnsNormal()
    {
        var flag = _sut.Calculate(null, 4m, 6m);
        flag.Should().Be(LabResultFlag.Normal);
    }

    [Fact]
    public void Calculate_BothRangesNull_ReturnsNormal()
    {
        var flag = _sut.Calculate(100m, null, null);
        flag.Should().Be(LabResultFlag.Normal);
    }

    // ─── Chi co High ───
    [Fact]
    public void Calculate_OnlyHighDefined_BelowHigh_ReturnsNormal()
    {
        var flag = _sut.Calculate(5m, null, 6m);
        flag.Should().Be(LabResultFlag.Normal);
    }

    [Fact]
    public void Calculate_OnlyHighDefined_AboveHigh_ReturnsH()
    {
        var flag = _sut.Calculate(6.5m, null, 6m);
        flag.Should().Be(LabResultFlag.H);
    }

    // ─── Edge case: range = 0 ───
    [Fact]
    public void Calculate_ZeroRange_ValueEqual_ReturnsNormal()
    {
        var flag = _sut.Calculate(5m, 5m, 5m);
        flag.Should().Be(LabResultFlag.Normal);
    }
}
