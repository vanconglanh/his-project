using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>ICD-10 diagnosis linked to an encounter. Maps diab_his_cli_encounter_diagnoses</summary>
public class EncounterDiagnosis : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string Icd10Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = DiagnosisType.Primary;
    public string? Note { get; set; }
}

public static class DiagnosisType
{
    public const string Primary   = "PRIMARY";
    public const string Secondary = "SECONDARY";
}
