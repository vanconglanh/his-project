namespace ProDiabHis.Domain.Common;

/// <summary>Interface danh dau entity thuoc ve mot tenant</summary>
public interface ITenantScoped
{
    int TenantId { get; set; }
}
