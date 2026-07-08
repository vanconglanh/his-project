using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Tenants;

public record UpdateTenantCommand(
    int Id,
    string? Name,
    string? CskcbCode,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    int? StorageQuotaGb,
    DateTime? ExpiresAt
) : IRequest<Result<TenantResponse>>;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, Result<TenantResponse>>
{
    private readonly IApplicationDbContext _db;

    public UpdateTenantCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TenantResponse>> Handle(UpdateTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.DeletedAt == null, ct);

        if (tenant is null)
            return Result<TenantResponse>.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        if (req.Name != null) tenant.Name = req.Name;
        if (req.CskcbCode != null) tenant.CskcbCode = req.CskcbCode;
        if (req.TaxCode != null) tenant.TaxCode = req.TaxCode;
        if (req.Address != null) tenant.Address = req.Address;
        if (req.Phone != null) tenant.Phone = req.Phone;
        if (req.Email != null) tenant.Email = req.Email;
        if (req.StorageQuotaGb.HasValue) tenant.StorageQuotaGb = req.StorageQuotaGb.Value;
        if (req.ExpiresAt.HasValue) tenant.ExpiresAt = req.ExpiresAt.Value;

        await _db.SaveChangesAsync(ct);
        return Result<TenantResponse>.Success(tenant.ToResponse());
    }
}
