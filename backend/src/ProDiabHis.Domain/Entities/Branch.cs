using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Chi nhanh / co so kham chua benh cua mot tenant. Map bang diab_his_sys_branches.
/// PK la INT AUTO_INCREMENT (giong Tenant) de khop cot branch_id INT o toan bo bang
/// nghiep vu + claim branch_id/branch_ids trong JWT. Khong ke thua BaseEntity (Guid Id)
/// theo mau Tenant.cs.
/// </summary>
public class Branch : IAuditTimestamps
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>DEPRECATED — bang diab_his_sys_clinics khong con duoc dung, giu de tuong thich nguoc</summary>
    public int? ClinicId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Ma CSKCB Bo Y te cap rieng cho chi nhanh — dung de lien thong DTQG/BHYT</summary>
    public string? CskcbCode { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WorkingHours { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public bool IsActive { get; set; } = true;

    /// <summary>Chi nhanh mac dinh cua tenant — dung 1 per tenant (enforce o application layer)</summary>
    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Trang thai vong doi chi nhanh — DRAFT|CONFIGURING|READY_CHECK|ACTIVE|SUSPENDED|CLOSED (BR-08/BR-110)</summary>
    public string Status { get; set; } = BranchStatus.Active;

    public int? GroupId { get; set; }

    // --- BHYT/DTQG compliance theo chi nhanh (BR-100..108, migration 9175) ---
    public string? HospitalRank { get; set; }
    public string? KcbTuyen { get; set; }
    public string? BhytContractCode { get; set; }
    public DateTime? BhytContractValidFrom { get; set; }
    public DateTime? BhytContractValidTo { get; set; }
    public bool BhytEnabled { get; set; }
    public bool DtqgEnabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public static class BranchStatus
{
    public const string Draft = "DRAFT";
    public const string Configuring = "CONFIGURING";
    public const string ReadyCheck = "READY_CHECK";
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Closed = "CLOSED";
}
