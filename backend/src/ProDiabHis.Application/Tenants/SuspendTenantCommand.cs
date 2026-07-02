using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Tenants;

public record SuspendTenantCommand(Guid Id, string? Reason) : IRequest<Result<TenantResponse>>;

public class SuspendTenantCommandHandler : IRequestHandler<SuspendTenantCommand, Result<TenantResponse>>
{
    private readonly IApplicationDbContext _db;

    public SuspendTenantCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TenantResponse>> Handle(SuspendTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.DeletedAt == null, ct);

        if (tenant is null)
            return Result<TenantResponse>.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        tenant.Status = TenantStatus.Suspended;
        await _db.SaveChangesAsync(ct);
        return Result<TenantResponse>.Success(tenant.ToResponse());
    }
}
