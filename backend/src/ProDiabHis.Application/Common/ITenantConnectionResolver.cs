namespace ProDiabHis.Application.Common;

/// <summary>Thong tin route tenant resolve tu Host header (subdomain/custom domain).</summary>
public record TenantRouteInfo(int TenantId, string Code, string Status);

/// <summary>
/// Resolve connection string cho tung tenant theo mo hinh DB-per-tenant (qua catalog control plane).
/// Fallback ve DefaultConnection khi tenant chua co DB rieng — che do shared-DB hien tai van chay
/// binh thuong (tenant_id + global query filter van loc du lieu, defense-in-depth).
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>
    /// Connection string cho tenant. tenantId &lt;= 0 (khong co context / super-admin) hoac tenant
    /// chua co DB rieng trong catalog -> tra DefaultConnection.
    /// </summary>
    string ResolveConnectionString(int tenantId);

    /// <summary>Resolve tenant tu Host header. Null neu khong khop domain nao (da verified).</summary>
    TenantRouteInfo? ResolveByHost(string host);

    /// <summary>Xoa cache connection cua 1 tenant (goi khi catalog thay doi).</summary>
    void InvalidateTenant(int tenantId);
}
