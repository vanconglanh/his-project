using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Tenants;

public record GetTenantQuery(int Id) : IRequest<Result<TenantResponse>>;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<TenantResponse>> Handle(GetTenantQuery req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .Where(t => t.Id == req.Id && t.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (tenant is null)
            return Result<TenantResponse>.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        return Result<TenantResponse>.Success(tenant.ToResponse());
    }
}
