using ProDiabHis.Application.Billing;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// BHYT co-pay basic: Sprint 8 version.
/// - DUNG_TUYEN: copay_rate% (80/95/100)
/// - TRAI_TUYEN: max 40%
/// - CAP_CUU: 100%
/// Sprint 9 se detail hon theo dieu kien cu the.
/// </summary>
public class BhytCoPayCalculatorImpl : IBhytCoPayCalculator
{
    public BhytCoPayResult Calculate(BhytCoPayInput input)
    {
        // Effective rate based on right_route
        var effectiveRate = input.RightRoute switch
        {
            "CAP_CUU" => 100,
            "TRAI_TUYEN" => Math.Min(input.CopayRate, 40),
            _ => input.CopayRate  // DUNG_TUYEN
        };

        var itemResults = new List<BillingItemBhyt>();
        decimal totalBhyt = 0;

        foreach (var item in input.Items)
        {
            if (!item.BhytApplicable)
            {
                itemResults.Add(new BillingItemBhyt(item.Id, 0, item.LineTotal));
                continue;
            }

            var bhyt = Math.Round(item.LineTotal * effectiveRate / 100, 2);
            var patient = item.LineTotal - bhyt;
            totalBhyt += bhyt;
            itemResults.Add(new BillingItemBhyt(item.Id, bhyt, patient));
        }

        var totalLineTotal = input.Items.Sum(i => i.LineTotal);
        var patientPayable = totalLineTotal - totalBhyt;

        return new BhytCoPayResult(totalBhyt, patientPayable, itemResults);
    }
}
