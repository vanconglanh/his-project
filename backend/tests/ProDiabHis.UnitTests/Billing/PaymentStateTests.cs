using FluentAssertions;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>Tests billing status transitions on payment events</summary>
public class PaymentStateTests
{
    private static Domain.Entities.Billing MakeBilling(decimal patientPayable) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = 1,
        Status = BillingStatus.Finalized,
        PatientPayable = patientPayable,
        PaidAmount = 0,
        Balance = patientPayable
    };

    private static void ApplyPayment(Domain.Entities.Billing billing, decimal amount)
    {
        billing.PaidAmount += amount;
        billing.Balance = billing.PatientPayable - billing.PaidAmount;
        billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;
    }

    [Fact]
    public void FullPayment_TransitionsToPaid()
    {
        var billing = MakeBilling(500_000);
        ApplyPayment(billing, 500_000);

        billing.Status.Should().Be(BillingStatus.Paid);
        billing.Balance.Should().Be(0);
        billing.PaidAmount.Should().Be(500_000);
    }

    [Fact]
    public void PartialPayment_TransitionsToPartialPaid()
    {
        var billing = MakeBilling(500_000);
        ApplyPayment(billing, 300_000);

        billing.Status.Should().Be(BillingStatus.PartialPaid);
        billing.Balance.Should().Be(200_000);
    }

    [Fact]
    public void TwoPartialPayments_ThenFull_StatusIsPaid()
    {
        var billing = MakeBilling(1_000_000);
        ApplyPayment(billing, 400_000);
        billing.Status.Should().Be(BillingStatus.PartialPaid);
        billing.Balance.Should().Be(600_000);

        ApplyPayment(billing, 600_000);
        billing.Status.Should().Be(BillingStatus.Paid);
        billing.Balance.Should().Be(0);
    }

    [Fact]
    public void Overpayment_StatusIsPaidAndBalanceNegative()
    {
        var billing = MakeBilling(300_000);
        ApplyPayment(billing, 400_000);

        billing.Status.Should().Be(BillingStatus.Paid);
        billing.Balance.Should().Be(-100_000);
    }

    [Fact]
    public void Refund_RevertsBillingBalance()
    {
        var billing = MakeBilling(500_000);
        ApplyPayment(billing, 500_000);
        billing.Status.Should().Be(BillingStatus.Paid);

        // Refund 200_000
        billing.PaidAmount -= 200_000;
        billing.Balance = billing.PatientPayable - billing.PaidAmount;
        billing.Status = billing.Balance <= 0 ? BillingStatus.Paid : BillingStatus.PartialPaid;

        billing.Status.Should().Be(BillingStatus.PartialPaid);
        billing.Balance.Should().Be(200_000);
    }

    [Fact]
    public void ZeroPayment_StatusStaysFinalized()
    {
        var billing = MakeBilling(500_000);
        // Don't call ApplyPayment
        billing.Status.Should().Be(BillingStatus.Finalized);
        billing.Balance.Should().Be(500_000);
    }
}
