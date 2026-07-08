namespace ProDiabHis.Application.Tenants;

/// <summary>DTO tra ve thong tin phong kham</summary>
public record TenantResponse(
    int Id,
    string Code,
    string Name,
    string? CskcbCode,
    string Status,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    string Subdomain,
    int StorageQuotaGb,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
