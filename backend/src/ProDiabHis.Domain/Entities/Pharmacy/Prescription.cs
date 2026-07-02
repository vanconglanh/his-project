using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>Don thuoc ke cho benh nhan. Map bang diab_his_pha_prescriptions</summary>
public class Prescription : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? PrescriptionNo { get; set; }
    public string Status { get; set; } = PrescriptionStatus.Draft;
    public string? DtqgCode { get; set; }
    public string? DtqgQr { get; set; }
    public DateTime? DtqgPushedAt { get; set; }
    public string? DiagnosisIcd10 { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? DispensedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public static class PrescriptionStatus
{
    public const string Draft = "DRAFT";
    public const string Signed = "SIGNED";
    public const string Dispensed = "DISPENSED";
    public const string Cancelled = "CANCELLED";
}
