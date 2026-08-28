using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Phan cong nhan su vao chi nhanh (N-N User &lt;-&gt; Branch). Map bang
/// diab_his_sec_user_branches. Bac si co the truc luan phien nhieu co so.
/// </summary>
public class UserBranch : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public int BranchId { get; set; }

    /// <summary>Chi nhanh chinh cua user — dung 1 per user (enforce o application layer)</summary>
    public bool IsPrimary { get; set; }
}
