using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Vital signs record. Maps diab_his_cli_vital_signs (existing from sprint 1-2)</summary>
public class VitalSigns : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? RecordedBy { get; set; }
    public int RecordSequence { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? HeartRateBpm { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? BpSystolic { get; set; }
    public int? BpDiastolic { get; set; }
    public int? Spo2Percent { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? PainScale { get; set; }
    public decimal? GlucoseMgDl { get; set; }
    public string? Note { get; set; }
}
