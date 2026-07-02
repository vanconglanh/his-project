using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>Tests cashier shift summary calculation</summary>
public class CashierShiftCalculationTests
{
    private static CashierShift MakeShift(decimal openingBalance = 0) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = 1,
        CashierUserId = Guid.NewGuid(),
        ShiftDate = DateOnly.FromDateTime(DateTime.Today),
        ShiftStart = DateTime.UtcNow.AddHours(-4),
        OpeningBalance = openingBalance,
        Status = "OPEN"
    };

    private record PaymentRow(string Method, string Status, decimal Amount);

    private static void ApplyPayments(CashierShift shift, List<PaymentRow> payments)
    {
        foreach (var p in payments)
        {
            if (p.Status == "VOID") { shift.TotalVoid += Math.Abs(p.Amount); continue; }
            if (p.Status == "REFUNDED" || p.Amount < 0) { shift.TotalRefund += Math.Abs(p.Amount); continue; }

            shift.CountTransactions++;
            switch (p.Method)
            {
                case "CASH": shift.TotalCash += p.Amount; break;
                case "VISA": case "MASTER": shift.TotalCard += p.Amount; break;
                case "BANK_TRANSFER": shift.TotalTransfer += p.Amount; break;
                case "QR_VIETQR": case "QR_MOMO": case "QR_VNPAY": shift.TotalQr += p.Amount; break;
                default: shift.TotalOther += p.Amount; break;
            }
        }
    }

    [Fact]
    public void SingleCashPayment_CalculatesCorrectly()
    {
        var shift = MakeShift(1_000_000);
        ApplyPayments(shift, [new("CASH", "COMPLETED", 500_000)]);

        shift.TotalCash.Should().Be(500_000);
        shift.CountTransactions.Should().Be(1);

        // Expected cash = opening + cash - refund
        var expected = shift.OpeningBalance + shift.TotalCash - shift.TotalRefund;
        expected.Should().Be(1_500_000);
    }

    [Fact]
    public void MultiPaymentMethods_AggregateCorrectly()
    {
        var shift = MakeShift();
        ApplyPayments(shift, [
            new("CASH", "COMPLETED", 300_000),
            new("VISA", "COMPLETED", 500_000),
            new("QR_MOMO", "COMPLETED", 200_000),
            new("BANK_TRANSFER", "COMPLETED", 100_000),
        ]);

        shift.TotalCash.Should().Be(300_000);
        shift.TotalCard.Should().Be(500_000);
        shift.TotalQr.Should().Be(200_000);
        shift.TotalTransfer.Should().Be(100_000);
        shift.CountTransactions.Should().Be(4);
    }

    [Fact]
    public void RefundPayment_NotCountedAsTransaction()
    {
        var shift = MakeShift();
        ApplyPayments(shift, [
            new("CASH", "COMPLETED", 500_000),
            new("CASH", "REFUNDED", 200_000),
        ]);

        shift.TotalCash.Should().Be(500_000);
        shift.TotalRefund.Should().Be(200_000);
        shift.CountTransactions.Should().Be(1); // refund not counted
    }

    [Fact]
    public void CashDifference_PositiveMeansExcess()
    {
        var shift = MakeShift(500_000);
        ApplyPayments(shift, [new("CASH", "COMPLETED", 300_000)]);

        var expectedCash = shift.OpeningBalance + shift.TotalCash - shift.TotalRefund;
        var actualCash = 810_000m; // user counted 10k more
        var difference = actualCash - expectedCash;

        difference.Should().Be(10_000); // excess
    }

    [Fact]
    public void CashDifference_NegativeMeansShortage()
    {
        var shift = MakeShift(500_000);
        ApplyPayments(shift, [new("CASH", "COMPLETED", 300_000)]);

        var expectedCash = shift.OpeningBalance + shift.TotalCash - shift.TotalRefund;
        var actualCash = 790_000m; // 10k short
        var difference = actualCash - expectedCash;

        difference.Should().Be(-10_000);
    }

    [Fact]
    public void NetCollected_IsGrossMinusRefundAndVoid()
    {
        var shift = MakeShift();
        ApplyPayments(shift, [
            new("CASH", "COMPLETED", 1_000_000),
            new("CASH", "REFUNDED", 100_000),
            new("VISA", "VOID", 200_000),
        ]);

        var gross = shift.TotalCash + shift.TotalCard + shift.TotalQr + shift.TotalTransfer + shift.TotalOther;
        var net = gross - shift.TotalRefund - shift.TotalVoid;

        gross.Should().Be(1_000_000);
        net.Should().Be(700_000); // 1M - 100k refund - 200k void
    }
}
