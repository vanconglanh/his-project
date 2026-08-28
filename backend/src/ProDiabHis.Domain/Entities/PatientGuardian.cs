using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Thong tin nguoi giam ho cua benh nhan. Bat buoc co it nhat 1 ban ghi hop le
/// (FullName + Phone) khi benh nhan duoi 72 thang tuoi tai thoi diem tao ho so (FR-101).
/// Map bang diab_his_pat_guardians.
/// </summary>
public class PatientGuardian : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>CMND/CCCD nguoi giam ho, da ma hoa AES-256-GCM</summary>
    public string? IdNumberEnc { get; set; }
    public string? IdNumberMasked { get; set; }
}

public static class GuardianRelationship
{
    public const string Father = "CHA";
    public const string Mother = "ME";
    public const string Grandfather = "ONG";
    public const string Grandmother = "BA";
    public const string Other = "NGUOI_GIAM_HO_KHAC";
}
