using ProDiabHis.Application.Billing;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Adapter giu nguyen contract IServicePriceResolver (BillingHandlers dang dung) nhung delegate
/// sang tang resolve DUNG CHUNG <see cref="IBranchPriceResolver"/> — 1 tang logic cho ca dich vu
/// va thuoc, khong con code resolve rieng cho dich vu.
/// </summary>
public class ServicePriceResolverImpl : IServicePriceResolver
{
    private readonly IBranchPriceResolver _resolver;

    public ServicePriceResolverImpl(IBranchPriceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ResolvedServicePrice?> ResolveAsync(
        int tenantId, Guid serviceId, int? branchId, DateOnly asOfDate, CancellationToken ct = default)
    {
        var r = await _resolver.ResolveAsync(
            tenantId, PriceItemType.Service, serviceId.ToString(), branchId, asOfDate, ct);
        return r == null ? null : new ResolvedServicePrice(r.Price, r.PriceSource, r.PriceOverrideId);
    }
}
