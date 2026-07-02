using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Tenants;

public static class TenantMappingExtensions
{
    public static TenantResponse ToResponse(this Tenant t) => new(
        Id: t.Id,
        Code: t.Code,
        Name: t.Name,
        CskcbCode: t.CskcbCode,
        Status: t.Status,
        TaxCode: t.TaxCode,
        Address: t.Address,
        Phone: t.Phone,
        Email: t.Email,
        Subdomain: t.Subdomain,
        StorageQuotaGb: t.StorageQuotaGb,
        ExpiresAt: t.ExpiresAt,
        CreatedAt: t.CreatedAt,
        UpdatedAt: t.UpdatedAt
    );
}
