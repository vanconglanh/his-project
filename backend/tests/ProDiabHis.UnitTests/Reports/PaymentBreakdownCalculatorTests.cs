using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

public class PaymentBreakdownCalculatorTests
{
    [Fact]
    public void CalculateBreakdown_HappyPath_GroupsByMethodAndComputesPercentage()
    {
        // Arrange — 2 giao dich CASH, 1 BANK_TRANSFER, khong co VOID/REFUND
        var payments = new List<(string Method, decimal Amount, string Status)>
        {
            ("CASH", 300_000m, "COMPLETED"),
            ("CASH", 200_000m, "COMPLETED"),
            ("BANK_TRANSFER", 500_000m, "COMPLETED"),
        };

        // Act
        var result = PaymentBreakdownCalculator.CalculateBreakdown(payments);

        // Assert
        Assert.Equal(2, result.Count);

        var cash = result.Single(x => x.Label == "Tiền mặt");
        Assert.Equal(500_000m, cash.Value);
        Assert.Equal(2, cash.Count);
        Assert.Equal(50m, cash.Percentage);

        var transfer = result.Single(x => x.Label == "Chuyển khoản");
        Assert.Equal(500_000m, transfer.Value);
        Assert.Equal(1, transfer.Count);
        Assert.Equal(50m, transfer.Percentage);
    }

    [Fact]
    public void CalculateBreakdown_ExcludesVoidAndRefundedAndNegativeAmount()
    {
        // Arrange — giao dich VOID/REFUNDED/am tien khong duoc tinh vao breakdown
        var payments = new List<(string Method, decimal Amount, string Status)>
        {
            ("CASH", 100_000m, "COMPLETED"),
            ("CASH", 100_000m, "VOID"),
            ("QR_MOMO", 50_000m, "REFUNDED"),
            ("QR_MOMO", -20_000m, "COMPLETED"),
        };

        // Act
        var result = PaymentBreakdownCalculator.CalculateBreakdown(payments);

        // Assert — chi con 1 giao dich CASH hop le
        var item = Assert.Single(result);
        Assert.Equal("Tiền mặt", item.Label);
        Assert.Equal(100_000m, item.Value);
        Assert.Equal(1, item.Count);
        Assert.Equal(100m, item.Percentage);
    }

    [Fact]
    public void CalculateBreakdown_EmptyInput_ReturnsEmptyList()
    {
        var result = PaymentBreakdownCalculator.CalculateBreakdown(
            Array.Empty<(string Method, decimal Amount, string Status)>());

        Assert.Empty(result);
    }

    [Fact]
    public void Summarize_HappyPath_ComputesRevenueTransactionsAndRefunds()
    {
        var payments = new List<(string Method, decimal Amount, string Status)>
        {
            ("CASH", 300_000m, "COMPLETED"),
            ("BANK_TRANSFER", 200_000m, "COMPLETED"),
            ("CASH", 50_000m, "REFUNDED"),
            ("CASH", 999_999m, "VOID"),
        };

        var (totalRevenue, totalTransactions, totalRefunds) = PaymentBreakdownCalculator.Summarize(payments);

        Assert.Equal(500_000m, totalRevenue);
        Assert.Equal(2, totalTransactions);
        Assert.Equal(50_000m, totalRefunds);
    }

    [Fact]
    public void LabelFor_UnknownMethod_ReturnsRawValue()
    {
        Assert.Equal("CUSTOM_METHOD", PaymentBreakdownCalculator.LabelFor("CUSTOM_METHOD"));
        Assert.Equal("Tiền mặt", PaymentBreakdownCalculator.LabelFor("CASH"));
    }
}
