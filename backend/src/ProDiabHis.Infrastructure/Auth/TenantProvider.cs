using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Auth;

/// <summary>Scoped service luu tenant_id cho request hien tai</summary>
public class TenantProvider : ITenantProvider
{
    private int _tenantId;

    public int TenantId => _tenantId;

    public void SetTenantId(int tenantId) => _tenantId = tenantId;
}
