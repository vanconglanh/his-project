using FluentAssertions;
using ProDiabHis.Application.VitalSigns;
using Xunit;

namespace ProDiabHis.UnitTests.VitalSigns;

/// <summary>Unit test range validation cho sinh hiệu (US-N01)</summary>
public class VitalSignsValidationTests
{
    private static VitalSignsRequest ValidRequest() => new VitalSignsRequest(
        null, 37.0m, 80, 16, 120, 80, 98, 65m, 165m, 0, 100m, null);

    // ── Temperature ──
    [Theory]
    [InlineData(29.9)]   // Below min
    [InlineData(45.1)]   // Above max
    public void Temperature_OutOfRange_ReturnsError(double temp)
    {
        var req = ValidRequest() with { TemperatureC = (decimal)temp };
        VitalSignsValidator.ValidateRanges(req).Should().NotBeNull();
    }

    [Theory]
    [InlineData(30.0)]
    [InlineData(37.5)]
    [InlineData(45.0)]
    public void Temperature_InRange_ReturnsNull(double temp)
    {
        var req = ValidRequest() with { TemperatureC = (decimal)temp };
        VitalSignsValidator.ValidateRanges(req).Should().BeNull();
    }

    // ── Heart rate ──
    [Theory]
    [InlineData(19)]
    [InlineData(301)]
    public void HeartRate_OutOfRange_ReturnsError(int hr)
    {
        var req = ValidRequest() with { HeartRateBpm = hr };
        VitalSignsValidator.ValidateRanges(req).Should().NotBeNull();
    }

    [Theory]
    [InlineData(20)]
    [InlineData(150)]
    [InlineData(300)]
    public void HeartRate_InRange_ReturnsNull(int hr)
    {
        var req = ValidRequest() with { HeartRateBpm = hr };
        VitalSignsValidator.ValidateRanges(req).Should().BeNull();
    }

    // ── Blood pressure ──
    [Fact]
    public void BpSystolic_TooLow_ReturnsError()
    {
        var req = ValidRequest() with { BpSystolic = 49 };
        VitalSignsValidator.ValidateRanges(req).Should().Contain("tâm thu");
    }

    [Fact]
    public void BpDiastolic_TooHigh_ReturnsError()
    {
        var req = ValidRequest() with { BpDiastolic = 201 };
        VitalSignsValidator.ValidateRanges(req).Should().Contain("tâm trương");
    }

    // ── SpO2 ──
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SpO2_OutOfRange_ReturnsError(int spo2)
    {
        var req = ValidRequest() with { Spo2Percent = spo2 };
        VitalSignsValidator.ValidateRanges(req).Should().NotBeNull();
    }

    // ── Glucose ──
    [Theory]
    [InlineData(19.9)]
    [InlineData(1001)]
    public void Glucose_OutOfRange_ReturnsError(double glucose)
    {
        var req = ValidRequest() with { GlucoseMgDl = (decimal)glucose };
        VitalSignsValidator.ValidateRanges(req).Should().NotBeNull();
    }

    // ── BMI computation ──
    [Theory]
    [InlineData(70, 175, 22.9)]
    [InlineData(50, 160, 19.5)]
    public void ComputeBmi_CorrectFormula(double weight, double height, double expectedBmi)
    {
        var bmi = VitalSignsValidator.ComputeBmi((decimal)weight, (decimal)height);
        bmi.Should().BeApproximately((decimal)expectedBmi, 0.2m);
    }

    [Fact]
    public void ComputeBmi_NullWeight_ReturnsNull()
    {
        VitalSignsValidator.ComputeBmi(null, 170m).Should().BeNull();
    }

    [Fact]
    public void ComputeBmi_ZeroHeight_ReturnsNull()
    {
        VitalSignsValidator.ComputeBmi(65m, 0m).Should().BeNull();
    }

    // ── All null (optional fields) ──
    [Fact]
    public void AllNullFields_NoError()
    {
        var req = new VitalSignsRequest(null, null, null, null, null, null, null, null, null, null, null, null);
        VitalSignsValidator.ValidateRanges(req).Should().BeNull();
    }
}
