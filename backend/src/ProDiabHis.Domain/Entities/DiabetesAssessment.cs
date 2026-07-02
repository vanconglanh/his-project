using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Diabetes clinical assessment per encounter. Maps diab_his_cli_diabetes_assessments</summary>
public class DiabetesAssessment : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public decimal? Hba1c { get; set; }
    public decimal? FastingGlucose { get; set; }
    public decimal? PostprandialGlucose { get; set; }
    public decimal? RandomGlucose { get; set; }
    public decimal? Egfr { get; set; }
    public decimal? SerumCreatinine { get; set; }
    public decimal? UrineAcr { get; set; }
    public int? BpSystolic { get; set; }
    public int? BpDiastolic { get; set; }
    public decimal? Bmi { get; set; }
    public decimal? WaistCircumference { get; set; }
    public string? DiabetesType { get; set; }
    public string? ComplicationsJson { get; set; }
    public string? TreatmentTargetJson { get; set; }
    public string? Note { get; set; }
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string? AssessedBy { get; set; }
}

/// <summary>Diabetes assessment template. Maps diab_his_cli_diabetes_templates</summary>
public class DiabetesTemplate : BaseEntity
{
    public int? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DefaultValuesJson { get; set; }
    public string? ChecklistJson { get; set; }
    public bool IsSystem { get; set; }
}
