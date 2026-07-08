using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

public class DiabetesTrendCalculatorTests
{
    [Fact]
    public void DetectDeterioration_HbA1cRising_ReturnsFlag()
    {
        var points = new List<DiabetesTrendCalculator.AssessmentPoint>
        {
            new(new DateTime(2026, 1, 1), 7.0m, 120, 78),
            new(new DateTime(2026, 4, 1), 7.6m, 120, 78),
        };

        var flags = DiabetesTrendCalculator.DetectDeterioration(points, 7.0m, 130, 80);

        Assert.Contains(flags, f => f.Code == "HBA1C_RISING");
    }

    [Fact]
    public void DetectDeterioration_HbA1cAboveTargetTwice_ReturnsFlag()
    {
        var points = new List<DiabetesTrendCalculator.AssessmentPoint>
        {
            new(new DateTime(2026, 1, 1), 8.5m, 120, 78),
            new(new DateTime(2026, 4, 1), 8.6m, 120, 78),
        };

        var flags = DiabetesTrendCalculator.DetectDeterioration(points, 7.0m, 130, 80);

        Assert.Contains(flags, f => f.Code == "HBA1C_ABOVE_TARGET_2X");
    }

    [Fact]
    public void DetectDeterioration_BpAboveTargetTwice_ReturnsFlag()
    {
        var points = new List<DiabetesTrendCalculator.AssessmentPoint>
        {
            new(new DateTime(2026, 1, 1), 7.0m, 145, 92),
            new(new DateTime(2026, 4, 1), 7.0m, 150, 95),
        };

        var flags = DiabetesTrendCalculator.DetectDeterioration(points, 7.0m, 130, 80);

        Assert.Contains(flags, f => f.Code == "BP_ABOVE_TARGET_2X");
    }

    [Fact]
    public void DetectDeterioration_NoData_ReturnsEmpty()
    {
        var flags = DiabetesTrendCalculator.DetectDeterioration(new List<DiabetesTrendCalculator.AssessmentPoint>(), 7.0m, 130, 80);
        Assert.Empty(flags);
    }

    [Theory]
    [InlineData(9.5, null, null, false, false, "MEDIUM")]
    [InlineData(7.5, null, null, false, false, "LOW")]
    [InlineData(8.5, null, null, false, false, "MEDIUM")]
    [InlineData(9.5, 25.0, 145, true, true, "HIGH")]
    public void ComputeRiskScore_ClassifyRisk_Works(double? hba1c, double? egfr, int? bpSys, bool rising, bool overdue, string expectedLevel)
    {
        var score = DiabetesTrendCalculator.ComputeRiskScore(
            (decimal?)hba1c, (decimal?)egfr, bpSys, rising, overdue);
        var level = DiabetesTrendCalculator.ClassifyRisk(score);

        Assert.Equal(expectedLevel, level);
    }
}
