using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Tenants;

public record DeleteTenantCommand(Guid Id) : IRequest<Result>;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public DeleteTenantCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.DeletedAt == null, ct);

        if (tenant is null)
            return Result.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        tenant.Status = TenantStatus.Terminated;
        tenant.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
