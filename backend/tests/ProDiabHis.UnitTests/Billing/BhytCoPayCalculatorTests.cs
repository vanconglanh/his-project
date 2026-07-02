using FluentAssertions;
using ProDiabHis.Application.Billing;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Billing;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

public class BhytCoPayCalculatorTests
{
    private readonly IBhytCoPayCalculator _calculator = new BhytCoPayCalculatorImpl();

    private static List<BillingItem> MakeItems(params (decimal lineTotal, bool bhytApplicable)[] specs)
    {
        return specs.Select(s => new BillingItem
        {
            Id = Guid.NewGuid(),
            LineTotal = s.lineTotal,
            BhytApplicable = s.bhytApplicable
        }).ToList();
    }

    [Theory]
    [InlineData(80)]
    [InlineData(95)]
    [InlineData(100)]
    public void DungTuyen_AppliesCorrectRate(int copayRate)
    {
        var items = MakeItems((1_000_000, true), (200_000, false));
        var input = new BhytCoPayInput("GD4050123456", copayRate, "DUNG_TUYEN", items);

        var result = _calculator.Calculate(input);

        var expectedBhyt = 1_000_000 * copayRate / 100m;
        result.BhytAmount.Should().Be(expectedBhyt);
        result.PatientPayable.Should().Be(1_200_000 - expectedBhyt);
    }

    [Fact]
    public void TraiTuyen_CappedAt40Percent()
    {
        var items = MakeItems((1_000_000, true));
        var input = new BhytCoPayInput("GD4050123456", 80, "TRAI_TUYEN", items);

        var result = _calculator.Calculate(input);

        // TRAI_TUYEN: min(80, 40) = 40%
        result.BhytAmount.Should().Be(400_000);
        result.PatientPayable.Should().Be(600_000);
    }

    [Fact]
    public void CapCuu_Always100Percent()
    {
        var items = MakeItems((1_000_000, true));
        var input = new BhytCoPayInput("GD4050123456", 80, "CAP_CUU", items);

        var result = _calculator.Calculate(input);

        // CAP_CUU: 100%
        result.BhytAmount.Should().Be(1_000_000);
        result.PatientPayable.Should().Be(0);
    }

    [Fact]
    public void NonBhytItems_NotIncludedInBhytAmount()
    {
        var items = MakeItems((500_000, true), (300_000, false));
        var input = new BhytCoPayInput("GD4050123456", 80, "DUNG_TUYEN", items);

        var result = _calculator.Calculate(input);

        result.BhytAmount.Should().Be(400_000); // 80% of 500_000 only
        result.PatientPayable.Should().Be(400_000); // 100_000 (20%) + 300_000 (non-bhyt)
    }

    [Fact]
    public void EmptyItems_ReturnsZero()
    {
        var input = new BhytCoPayInput("GD4050123456", 80, "DUNG_TUYEN", []);
        var result = _calculator.Calculate(input);
        result.BhytAmount.Should().Be(0);
        result.PatientPayable.Should().Be(0);
        result.ItemResults.Should().BeEmpty();
    }

    [Fact]
    public void ItemResults_ContainsCorrectPerItemBhyt()
    {
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();
        var items = new List<BillingItem>
        {
            new() { Id = item1Id, LineTotal = 500_000, BhytApplicable = true },
            new() { Id = item2Id, LineTotal = 200_000, BhytApplicable = false }
        };

        var input = new BhytCoPayInput("GD4050123456", 80, "DUNG_TUYEN", items);
        var result = _calculator.Calculate(input);

        var r1 = result.ItemResults.First(r => r.ItemId == item1Id);
        var r2 = result.ItemResults.First(r => r.ItemId == item2Id);

        r1.BhytAmount.Should().Be(400_000);
        r2.BhytAmount.Should().Be(0);
    }
}
