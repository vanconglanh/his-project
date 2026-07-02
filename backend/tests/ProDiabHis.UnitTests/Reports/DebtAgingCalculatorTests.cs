using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

public class DebtAgingCalculatorTests
{
    [Fact]
    public void Calculate_HappyPath_GroupsIntoCorrectBuckets()
    {
        // Arrange — 1 hoa don moi (10 ngay), 1 hoa don 45 ngay, 1 hoa don 75 ngay, 1 hoa don 120 ngay
        var details = new List<DebtDetailItem>
        {
            new("HD-0001", "Nguyễn Văn A", 100_000m, 10, null),
            new("HD-0002", "Trần Thị B", 200_000m, 45, null),
            new("HD-0003", "Lê Văn C", 300_000m, 75, null),
            new("HD-0004", "Phạm Thị D", 400_000m, 120, null),
        };

        // Act
        var result = DebtAgingCalculator.Calculate(details);

        // Assert
        Assert.Equal(100_000m, result.Bucket0To30);
        Assert.Equal(200_000m, result.Bucket30To60);
        Assert.Equal(300_000m, result.Bucket60To90);
        Assert.Equal(400_000m, result.BucketOver90);
        Assert.Equal(1_000_000m, result.Total);
        Assert.Equal(4, result.Details.Count);
    }

    [Fact]
    public void Calculate_BoundaryDays_AreInclusiveOnLowerBucket()
    {
        // Arrange — dung moc bien: 30, 60, 90 ngay phai roi vao bucket thap hon (<=)
        var details = new List<DebtDetailItem>
        {
            new("HD-0001", "BN 30 ngay", 111_000m, 30, null),
            new("HD-0002", "BN 60 ngay", 222_000m, 60, null),
            new("HD-0003", "BN 90 ngay", 333_000m, 90, null),
            new("HD-0004", "BN 91 ngay", 444_000m, 91, null),
        };

        // Act
        var result = DebtAgingCalculator.Calculate(details);

        // Assert
        Assert.Equal(111_000m, result.Bucket0To30);
        Assert.Equal(222_000m, result.Bucket30To60);
        Assert.Equal(333_000m, result.Bucket60To90);
        Assert.Equal(444_000m, result.BucketOver90);
    }

    [Fact]
    public void Calculate_EmptyInput_ReturnsAllZeroBuckets()
    {
        var result = DebtAgingCalculator.Calculate(Array.Empty<DebtDetailItem>());

        Assert.Equal(0m, result.Bucket0To30);
        Assert.Equal(0m, result.Bucket30To60);
        Assert.Equal(0m, result.Bucket60To90);
        Assert.Equal(0m, result.BucketOver90);
        Assert.Equal(0m, result.Total);
        Assert.Empty(result.Details);
    }

    [Fact]
    public void Calculate_IgnoresNonPositiveBalance()
    {
        // Arrange — balance <= 0 khong duoc cong vao bucket nao (du lieu bat thuong / da tat toan)
        var details = new List<DebtDetailItem>
        {
            new("HD-0001", "BN so du am", 0m, 10, null),
            new("HD-0002", "BN hop le", 500_000m, 5, null),
        };

        // Act
        var result = DebtAgingCalculator.Calculate(details);

        // Assert
        Assert.Equal(500_000m, result.Bucket0To30);
        Assert.Equal(500_000m, result.Total);
    }
}
