using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

public class DiabetesCohortTests
{
    [Theory]
    [InlineData(5.5,  "TOT")]
    [InlineData(6.9,  "TOT")]
    [InlineData(7.0,  "KHA")]
    [InlineData(7.9,  "KHA")]
    [InlineData(8.0,  "KEM")]
    [InlineData(12.0, "KEM")]
    public void ClassifyControl_ReturnsCorrectBucket(double hba1c, string expected)
    {
        var result = DiabetesCohortCalculator.ClassifyControl((decimal)hba1c);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildBuckets_ZeroPatients_AllZero()
    {
        var buckets = DiabetesCohortCalculator.BuildBuckets(7.0m, 0);

        Assert.All(buckets, b =>
        {
            Assert.Equal(0, b.PatientCount);
            Assert.Equal(0m, b.Percentage);
        });
    }

    [Fact]
    public void BuildBuckets_PositivePatients_SumApprox100Percent()
    {
        var buckets = DiabetesCohortCalculator.BuildBuckets(7.5m, 100);
        var total = buckets.Sum(b => b.Percentage);

        // Allow rounding drift
        Assert.InRange(total, 99m, 101m);
    }
}
