using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

public record BhytCoPayInput(
    string BhytCardNo,
    int CopayRate,           // 80 | 95 | 100
    string RightRoute,       // DUNG_TUYEN | TRAI_TUYEN | CAP_CUU
    IReadOnlyList<BillingItem> Items);

public record BhytCoPayResult(
    decimal BhytAmount,
    decimal PatientPayable,
    IReadOnlyList<BillingItemBhyt> ItemResults);

public record BillingItemBhyt(Guid ItemId, decimal BhytAmount, decimal PatientAmount);

/// <summary>Tinh dong chi BHYT. Sprint 9 se detail hon theo dung/trai tuyen</summary>
public interface IBhytCoPayCalculator
{
    BhytCoPayResult Calculate(BhytCoPayInput input);
}
