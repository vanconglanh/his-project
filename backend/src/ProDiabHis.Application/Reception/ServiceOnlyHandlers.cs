using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Reception;

// BM-92: Chi dinh dich vu (khong kham) - tao hoa don DRAFT khong gan encounter, khong tao ticket phong kham.

public record ServiceOnlyOrderRequest(
    Guid PatientId,
    List<Guid>? ServiceIds,
    List<Guid>? ServicePackageIds,
    string? Note,
    List<Guid>? ExternalFileIds);

public record ServiceOnlyOrderResponse(Guid BillingId, string? BillNo, int ItemCount, decimal PatientPayable);

public record CreateServiceOnlyOrderCommand(ServiceOnlyOrderRequest Request)
    : IRequest<Result<ServiceOnlyOrderResponse>>;

public class CreateServiceOnlyOrderHandler
    : IRequestHandler<CreateServiceOnlyOrderCommand, Result<ServiceOnlyOrderResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IServicePriceResolver _priceResolver;

    public CreateServiceOnlyOrderHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IServicePriceResolver priceResolver)
    { _db = db; _tenant = tenant; _user = user; _priceResolver = priceResolver; }

    public async Task<Result<ServiceOnlyOrderResponse>> Handle(CreateServiceOnlyOrderCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var tenantId = _tenant.TenantId;
        var serviceIds = (req.ServiceIds ?? new()).Distinct().ToList();
        var packageIds = (req.ServicePackageIds ?? new()).Distinct().ToList();

        if (serviceIds.Count == 0 && packageIds.Count == 0)
            return Result<ServiceOnlyOrderResponse>.Failure("SERVICE_ONLY_EMPTY", "Vui lòng chọn ít nhất một dịch vụ hoặc gói dịch vụ");

        var patientOk = await _db.Patients.AnyAsync(p => p.Id == req.PatientId && p.TenantId == tenantId, ct);
        if (!patientOk)
            return Result<ServiceOnlyOrderResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        // Gom (serviceId, qty, discount%) tu dich vu le + goi
        var lines = serviceIds.Select(id => (ServiceId: id, Qty: 1, Disc: 0m)).ToList();
        if (packageIds.Count > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var packages = await _db.ServicePackages.Include(p => p.Items)
                .Where(p => packageIds.Contains(p.Id) && p.TenantId == tenantId && p.IsActive).ToListAsync(ct);
            if (packages.Count != packageIds.Count)
                return Result<ServiceOnlyOrderResponse>.Failure("SERVICE_PACKAGE_NOT_FOUND", "Không tìm thấy gói dịch vụ hoặc gói đã ngừng áp dụng");
            if (packages.Any(p => (p.ValidFrom.HasValue && p.ValidFrom > today) || (p.ValidTo.HasValue && p.ValidTo < today)))
                return Result<ServiceOnlyOrderResponse>.Failure("SERVICE_PACKAGE_EXPIRED", "Gói dịch vụ đã hết hiệu lực");
            foreach (var pk in packages)
                lines.AddRange(pk.Items.Select(i => (i.ServiceId, i.Quantity, pk.DiscountPercent)));
        }

        var ids = lines.Select(l => l.ServiceId).Distinct().ToList();
        var services = await _db.BillingServices
            .Where(s => ids.Contains(s.Id) && s.TenantId == tenantId && s.IsActive).ToListAsync(ct);
        if (services.Count != ids.Count)
            return Result<ServiceOnlyOrderResponse>.Failure("SERVICE_NOT_FOUND", "Có dịch vụ không tồn tại hoặc đã ngừng sử dụng");
        var map = services.ToDictionary(s => s.Id);

        var billing = new Domain.Entities.Billing
        {
            TenantId = tenantId,
            EncounterId = null,
            PatientId = req.PatientId,
            BillNo = $"HD-{DateTime.Now:yyyyMM}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}",
            Payer = "SELF",
            Status = BillingStatus.Draft,
            Note = BuildNote(req),
            CreatedBy = _user.UserId
        };

        var today2 = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var l in lines)
        {
            var s = map[l.ServiceId];
            var resolved = await _priceResolver.ResolveAsync(tenantId, s.Id, billing.BranchId, today2, ct);
            var price = resolved?.Price ?? s.Price;
            var item = new BillingItem
            {
                BillingId = billing.Id,
                TenantId = tenantId,
                ItemType = "SERVICE",
                RefId = s.Id,
                Code = s.Code,
                Name = s.Name,
                Quantity = l.Qty,
                UnitPrice = price,
                VatRate = s.VatRate,
                DiscountPercent = l.Disc,
                LineTotal = l.Qty * price * (1 - l.Disc / 100),
                BhytApplicable = false,
                BaseUnitPrice = resolved?.Price,
                PriceSource = resolved?.PriceSource,
                PriceOverrideId = resolved?.PriceOverrideId
            };
            billing.Items.Add(item);
        }

        BillingMapper.Recalculate(billing);
        _db.Billings.Add(billing);
        foreach (var it in billing.Items) _db.BillingItems.Add(it);
        await _db.SaveChangesAsync(ct);

        return Result<ServiceOnlyOrderResponse>.Success(new ServiceOnlyOrderResponse(
            billing.Id, billing.BillNo, billing.Items.Count, billing.PatientPayable));
    }

    private static string BuildNote(ServiceOnlyOrderRequest req)
    {
        var note = "[Chỉ định dịch vụ không khám]";
        if (!string.IsNullOrWhiteSpace(req.Note)) note += " " + req.Note.Trim();
        if (req.ExternalFileIds is { Count: > 0 })
            note += " | Chỉ định ngoài (file): " + string.Join(",", req.ExternalFileIds);
        return note;
    }
}
