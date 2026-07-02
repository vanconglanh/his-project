using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

/// <summary>Gom items tu encounter va tinh toan hoa don</summary>
public interface IBillingCalculator
{
    Task<List<BillingItem>> BuildItemsFromEncounterAsync(
        Guid encounterId,
        int tenantId,
        bool includeDispensing,
        CancellationToken ct = default);
}
