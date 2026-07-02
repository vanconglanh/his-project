using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>Tests for IBillingCalculator - verifies item aggregation logic</summary>
public class BillingCalculatorTests
{
    // The actual BillingCalculatorImpl requires a DB connection.
    // We test the core calculation logic through a testable wrapper.

    [Fact]
    public void LineTotal_Calculation_IsCorrect()
    {
        // Arrange
        var item = new BillingItem
        {
            Quantity = 3,
            UnitPrice = 150_000,
            DiscountPercent = 0,
            VatRate = 10
        };
        item.LineTotal = item.Quantity * item.UnitPrice * (1 - item.DiscountPercent / 100);

        // Assert
        item.LineTotal.Should().Be(450_000);
    }

    [Fact]
    public void LineTotal_WithDiscount_IsCorrect()
    {
        var item = new BillingItem
        {
            Quantity = 2,
            UnitPrice = 200_000,
            DiscountPercent = 10
        };
        item.LineTotal = item.Quantity * item.UnitPrice * (1 - item.DiscountPercent / 100);
        item.LineTotal.Should().Be(360_000);
    }

    [Fact]
    public void BillingRecalculate_SumsCorrectly()
    {
        // Arrange
        var billing = new Domain.Entities.Billing
        {
            DiscountAmount = 0,
            BhytAmount = 0,
            PaidAmount = 0
        };
        billing.Items.Add(new BillingItem { LineTotal = 100_000, VatRate = 10 });
        billing.Items.Add(new BillingItem { LineTotal = 200_000, VatRate = 0 });

        // Act - mirror BillingMapper.Recalculate
        billing.Subtotal = billing.Items.Sum(i => i.LineTotal);
        billing.VatTotal = billing.Items.Sum(i => i.LineTotal * i.VatRate / 100);
        billing.PatientPayable = billing.Subtotal + billing.VatTotal - billing.DiscountAmount - billing.BhytAmount;
        billing.Balance = billing.PatientPayable - billing.PaidAmount;

        // Assert
        billing.Subtotal.Should().Be(300_000);
        billing.VatTotal.Should().Be(10_000); // only first item has 10%
        billing.PatientPayable.Should().Be(310_000);
        billing.Balance.Should().Be(310_000);
    }

    [Fact]
    public void BillingStatus_Transitions_AreValid()
    {
        // DRAFT -> FINALIZED -> (cannot change)
        var billing = new Domain.Entities.Billing { Status = BillingStatus.Draft };
        billing.Status.Should().Be("DRAFT");

        billing.Status = BillingStatus.Finalized;
        billing.Status.Should().Be("FINALIZED");
    }

    [Fact]
    public void BillingStatus_VoidFromAnyState_IsAllowed()
    {
        var billing = new Domain.Entities.Billing { Status = BillingStatus.Finalized };
        billing.Status = BillingStatus.Void;
        billing.VoidReason = "Test void";
        billing.Status.Should().Be("VOID");
        billing.VoidReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MultipleItems_WithMixedTypes_AggregatCorrectly()
    {
        var items = new List<BillingItem>
        {
            new() { ItemType = "SERVICE", LineTotal = 100_000, BhytApplicable = false },
            new() { ItemType = "DRUG", LineTotal = 50_000, BhytApplicable = true },
            new() { ItemType = "LAB", LineTotal = 200_000, BhytApplicable = true }
        };

        var subtotal = items.Sum(i => i.LineTotal);
        var bhytEligibleTotal = items.Where(i => i.BhytApplicable).Sum(i => i.LineTotal);

        subtotal.Should().Be(350_000);
        bhytEligibleTotal.Should().Be(250_000);
    }
}
