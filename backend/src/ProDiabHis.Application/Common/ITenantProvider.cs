namespace ProDiabHis.Application.Common;

/// <summary>Cung cap tenant context cho request hien tai</summary>
public interface ITenantProvider
{
    int TenantId { get; }
    void SetTenantId(int tenantId);
}
