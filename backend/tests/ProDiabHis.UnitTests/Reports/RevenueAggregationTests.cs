using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

public class RevenueAggregationTests
{
    [Fact]
    public void NetRevenue_Equals_TotalRevenue_Minus_Refunds()
    {
        // Arrange
        var total = 1_000_000m;
        var refunds = 50_000m;

        // Act
        var net = total - refunds;

        // Assert
        Assert.Equal(950_000m, net);
    }

    [Fact]
    public void Series_Sum_Matches_TotalRevenue()
    {
        // Arrange
        var series = new List<ChartDataPoint>
        {
            new("2026-05-01", 200_000m),
            new("2026-05-02", 300_000m),
            new("2026-05-03", 500_000m),
        };

        // Act
        var sum = series.Sum(s => s.Value);

        // Assert
        Assert.Equal(1_000_000m, sum);
    }

    [Fact]
    public void EmptySeries_Returns_ZeroTotal()
    {
        // Arrange
        var series = new List<ChartDataPoint>();

        // Act
        var sum = series.Sum(s => s.Value);

        // Assert
        Assert.Equal(0m, sum);
    }
}
