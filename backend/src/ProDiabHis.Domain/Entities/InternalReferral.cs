namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Chuyen co so noi bo (chuyen benh nhan giua 2 chi nhanh cung tenant) — BR-29.
/// Entity 2-branch (SourceBranchId/TargetBranchId) — KHONG implement IBranchScoped vi khong the
/// gan 1 BranchId duy nhat; scope xu ly rieng o handler (nguon HOAC dich nam trong branch scope).
/// Map bang diab_his_clinic_internal_referrals (migration 9176).
/// </summary>
public class InternalReferral
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public int SourceBranchId { get; set; }
    public int TargetBranchId { get; set; }
    public string? EncounterId { get; set; }
    public Guid? ReferringDoctorId { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = InternalReferralStatus.Sent;
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public static class InternalReferralStatus
{
    public const string Sent = "SENT";
    public const string Accepted = "ACCEPTED";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Sent, Accepted, Completed, Cancelled };
}
