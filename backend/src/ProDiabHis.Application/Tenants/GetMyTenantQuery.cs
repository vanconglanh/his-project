using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Tenants;

public record GetMyTenantQuery : IRequest<Result<TenantResponse>>;

public class GetMyTenantQueryHandler : IRequestHandler<GetMyTenantQuery, Result<TenantResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenantProvider;

    public GetMyTenantQueryHandler(IDapperConnectionFactory db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TenantResponse>> Handle(GetMyTenantQuery req, CancellationToken ct)
    {
        var tenantId = _tenantProvider.TenantId;

        using var conn = _db.CreateConnection();

        // Dung Dapper de tranh EF Core mapping loi khi co cot chua ton tai trong DB
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, code, name, cskcb_code, status, subdomain, storage_quota_gb, created_at, updated_at
              FROM diab_his_sys_tenants
              WHERE id = @tenantId AND deleted_at IS NULL
              LIMIT 1",
            new { tenantId });

        if (row is null)
            return Result<TenantResponse>.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        // Map sang TenantResponse — cac cot optional chua co trong DB tra ve null
        var response = new TenantResponse(
            Id: (int)row.id,
            Code: (string)row.code,
            Name: (string)row.name,
            CskcbCode: (string?)row.cskcb_code,
            Status: (string)(row.status ?? "ACTIVE"),
            TaxCode: null,
            Address: null,
            Phone: null,
            Email: null,
            Subdomain: (string)(row.subdomain ?? ""),
            StorageQuotaGb: (int)(row.storage_quota_gb ?? 20),
            ExpiresAt: null,
            CreatedAt: (DateTime)(row.created_at ?? DateTime.UtcNow),
            UpdatedAt: (DateTime)(row.updated_at ?? DateTime.UtcNow)
        );

        return Result<TenantResponse>.Success(response);
    }
}
