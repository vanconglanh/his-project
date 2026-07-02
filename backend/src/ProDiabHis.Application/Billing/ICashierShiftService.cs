using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

/// <summary>Quan ly ca thu ngan: tim ca dang mo, tinh toan cuoi ca</summary>
public interface ICashierShiftService
{
    Task<CashierShift?> GetOpenShiftAsync(Guid userId, int tenantId, CancellationToken ct = default);
    Task<CashierShift> CalculateShiftSummaryAsync(CashierShift shift, CancellationToken ct = default);
}
