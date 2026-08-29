namespace ProDiabHis.Application.Billing;

/// <summary>Ket qua resolve gia dich vu 3 tang (BR-70..BR-76): BRANCH override -> GROUP override -> TENANT (gia goc).</summary>
public record ResolvedServicePrice(decimal Price, string PriceSource, Guid? PriceOverrideId);

/// <summary>
/// Resolve gia ap dung cho 1 dich vu tai 1 chi nhanh vao 1 ngay cu the.
/// Thu tu uu tien (BR-70): override BRANCH con hieu luc > override GROUP (theo nhom cua branch)
/// con hieu luc > gia goc TENANT trong diab_his_bil_services.
/// Luon resolve o server (khong tin gia tu client) - dung khi tao billing item (snapshot BR-73).
/// </summary>
public interface IServicePriceResolver
{
    Task<ResolvedServicePrice?> ResolveAsync(
        int tenantId, Guid serviceId, int? branchId, DateOnly asOfDate, CancellationToken ct = default);
}
